/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jorn.baayen@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
 */

using System;
using System.Collections;
using System.IO;

using Gdk;

namespace Muine
{
	// TODO: Inherit from Database
	public class CoverDatabase 
	{
		// Constants
		// Constants :: CoverSize
		/// <remarks>
		///	We don't bother to get this one back from GtkIconSize as we'd
		///	have to resize all our covers.
		/// </remarks>
		public const int CoverSize = 66;

		// Events
		public delegate void DoneLoadingHandler ();
		public event         DoneLoadingHandler DoneLoading;

		// Objects
		private Hashtable covers;
		private Pixbuf downloading_pixbuf;
		private CoverGetter getter;
		private Database db;	

		// Variables
		private bool loading = true;

		// Constructor
		/// <summary>
		///	Create a <see cref="CoverDatabase"/ > object.
		/// </summary>
		/// <param name="version">
		///	Version of the database to use.
		/// </param>
		public CoverDatabase (int version)
		{
			db = new Database (FileUtils.CoversDBFile, version);

			covers = new Hashtable ();

			// Hack to get the GtkStyle
			Gtk.Label label = new Gtk.Label (String.Empty);
			label.EnsureStyle ();

			downloading_pixbuf =
			  label.RenderIcon
			    ("muine-cover-downloading", StockIcons.CoverSize, null);

			label.Destroy ();

			getter = new CoverGetter (this);
		}

		// Properties
		// Properties :: Covers (get;)
		/// <summary>
		///	The covers in the database.
		/// </summary>
		/// <returns>
		///	A <see cref="Hashtable" /> of the covers in the 
		///	database in the format of:
		///	album key => <see cref="Gdk.Pixbuf" />.
		/// </returns>
		public Hashtable Covers {
			get { return covers; }
		}

		// Properties :: DownloadingPixbuf (get;)
		/// <summary>
		///	The <see cref="Gdk.Pixbuf" /> which is used as a
		///	placeholder while the cover is being downloaded.
		/// </summary>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" />.
		/// </returns>
		public Pixbuf DownloadingPixbuf {
			get { return downloading_pixbuf; }
		}

		// Properties :: Getter (get;)
		/// <summary>
		///	The <see cref="CoverGetter"> used to find covers.
		/// </summary>
		/// <returns>
		///	A <see cref="CoverGetter" />.
		/// </returns>
		public CoverGetter Getter {
			get { return getter; }
		}

		// Properties :: Loading (get;)
		/// <summary>
		///	Whether the database is currently being loaded.
		/// </summary>
		/// <returns>
		///	True if the database is currently being loaded,
		///	False otherwise.
		/// </returns>
		public bool Loading {
			get { return loading; }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Load
		/// <summary>
		///	Load the database.
		/// </summary>
		/// <remarks>
		///	The database loading is actually carried out by a 
		///	<see cref="LoadThread" />.
		/// </remarks>
		public void Load ()
		{
			new LoadThread (db);
		}

		// Methods :: Public :: SetCover
		/// <summary>
		///	Store a cover in the database.
		/// </summary>
		/// <param name="key">
		///	The album key.
		/// </param>
		/// <param name="pix">
		///	The <see cref="Gdk.Pixbuf" /> to be used for the cover.
		/// </param>
		public void SetCover (string key, Pixbuf pix)
		{
			lock (this) {
				bool replace = Covers.ContainsKey (key);

				if (replace)
					Covers.Remove (key);

				Covers.Add (key, pix);

				int data_size;
				IntPtr data = PackCover (pix, out data_size);
				db.Store (key, data, data_size, replace);
			}
		}

		// Methods :: Public :: RemoveCover
		/// <summary>
		///	Remove a cover from the database.
		/// </summary>
		/// <param name="key">
		///	The album key.
		/// </param>
		public void RemoveCover (string key)
		{
			lock (this) {
				if (!Covers.ContainsKey (key))
					return;

				db.Delete (key);
				Covers.Remove (key);
			}
		}

		// Methods :: Public :: MarkAsBeingChecked
		/// <summary>
		/// 	Marks the cover as being checked.
		/// </summary>
		/// <remarks>
		///	Sets the cover to be null. Unmark with 
		///	<see cref="UnmarkAsBeingChecked" /> to maintain
		///	orthogonality.
		/// </remarks>
		/// <param name="key">
		///	The album key.
		/// </param>
		public void MarkAsBeingChecked (string key)
		{
			SetCover (key, null);
		}

		// Methods ::: Public :: UnmarkAsBeingChecked
		/// <summary>
		///	Unmarks a cover that was being checked.
		/// </summary>
		/// <remarks>
		///	This simply removes the cover. Use with
		///	<see cref="MarkAsBeingChecked" />.
		/// </remarks>
		/// <param name="key">
		///	The album key.
		/// </param>
		public void UnmarkAsBeingChecked (string key)
		{
			RemoveCover (key);
		}
				
		// Methods :: Private
		// Methods :: Private :: PackCover
		/// <summary>
		///	Pack cover into a format which can be stored in the database.
		/// </summary>
		/// <param name="pixbuf">
		///	The cover to be added.
		/// </param>
		/// <param name="length">
		///	Return location to store data length.
		/// </param>
		/// <returns>
		///	An <see cref="IntPtr" /> to the packed cover.
		/// </returns>
		private IntPtr PackCover (Pixbuf pixbuf, out int length)
		{
			IntPtr p = Database.PackStart ();

			bool being_checked = (pixbuf == null);
			
			Database.PackBool (p, being_checked);

			if (!being_checked)
				Database.PackPixbuf (p, pixbuf.Handle);

			return Database.PackEnd (p, out length);
		}

		// Methods :: Private :: EmitDoneLoading
		/// <summary>
		///	Calls the handler for the <see cref="DoneLoading" />
		///	event.
		/// </summary>
		private void EmitDoneLoading ()
		{
			loading = false;

			if (DoneLoading != null)
				DoneLoading ();
		}

		// Internal Classes
		// Internal Classes :: LoadThread
		// 	FIXME: Split off? This is big.
		private class LoadThread : ThreadBase
		{
			// Structs
			/// <summary>
			///	A structure representing a loaded cover.
			/// </summary>
			private struct LoadedCover {
				public Pixbuf Pixbuf;
				public Item Item;

				public LoadedCover (Item item, Pixbuf pixbuf)
				{
					Item   = item;
					Pixbuf = pixbuf;
				}
			}

			// Variables
			private Database db;			

			// Constructor
			/// <summary>
			///	Create a new <see cref="LoadThread"/ > object.
			/// </summary>
			/// <remarks>
			///	This thread is used to load the cover database.
			/// </remarks>
			/// <param name="db">
			///	The <see cref="Database" /> to load.
			/// </param>
			public LoadThread (Database db)
			{
				this.db = db;
				thread.Start ();
			}

			// Delegate Functions 
			// Delegate Functions :: ThreadFunc (ThreadStart) (ThreadBase)
			/// <summary>
			/// 	Load the database.
			/// </summary>
			/// <remarks>
			///	This is the main thread function.
			/// </remarks>
			protected override void ThreadFunc ()
			{
				Database.DecodeFunctionDelegate func =
				  new Database.DecodeFunctionDelegate (DecodeFunction);

				lock (Global.CoverDB)
					db.Load (func);

				thread_done = true;
			}

			// Delegate Functions :: MainLoopIdle (ThreadBase)
			/// <summary>
			///	Adds the covers from the queue to their 
			///	associated albums.
			/// </summary>
			/// <remarks>
			///	This runs from GLib's main loop.
			/// </remarks>
			/// <returns>
			///	True if the loop should continue,
			///	False if it should stop.
			/// </returns>
			protected override bool MainLoopIdle ()
			{
				if (queue.Count == 0) {
					if (thread_done) {
						Global.CoverDB.EmitDoneLoading ();						
						return false;
					}

					return true;
				}

				LoadedCover lc = (LoadedCover) queue.Dequeue ();
			
				lc.Item.CoverImage = lc.Pixbuf;

				return true;
			}

			// Delegate Functions :: DecodeFunction
			//   (Database.DecodeFunctionDelegate)
			/// <summary>
			///	Delegate to decode the data in the database.
			/// </summary>
			/// <param name="key">
			///	The album key.
			/// </param>
			/// <param name="data">
			///	An <see cref="IntPtr" /> to the data.
			/// </param>
			private void DecodeFunction (string key, IntPtr data)
			{
				IntPtr p = data;

				bool being_checked;
				p = Database.UnpackBool (p, out being_checked);
		
				Pixbuf pixbuf = null;
				if (!being_checked) {
					IntPtr pix_handle;
					p = Database.UnpackPixbuf (p, out pix_handle);
					pixbuf = new Pixbuf (pix_handle);
				}

				if (Global.CoverDB.Covers.Contains (key)) {
					if (being_checked)
						return;
					
					// stored covers take priority
					Global.CoverDB.Covers.Remove (key);
				}
				
				// Add independent of whether item is null or not,
				// this way manually set covers will stay for
				// removable devices.
				if (!being_checked) 
					Global.CoverDB.Covers.Add (key, pixbuf);

				Item item = Global.DB.GetAlbum (key);
				if (item == null)
					item = Global.DB.GetSong (key);

				if (item != null) {
					if (being_checked) {
						// false, as we don't want to write to the db
						// while we're loading
						Album album = (Album) item;

						pixbuf =
						  Global.CoverDB.Getter.GetAmazon (album, false);

						Global.CoverDB.Covers.Add (key, null);
					}
					
					LoadedCover lc = new LoadedCover (item, pixbuf);
					queue.Enqueue (lc);
				}
			}
		}
	}
}
