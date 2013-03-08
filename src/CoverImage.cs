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
using System.Text.RegularExpressions;

using Gtk;
using Gdk;

namespace Muine
{
	public class CoverImage : EventBox
	{
		// Static
		// Static :: Variables
		// Static :: Variables :: Drag-and-Drop
		private static TargetEntry [] drag_entries = {
			DndUtils.TargetUriList,
			DndUtils.TargetGnomeIconList,
			DndUtils.TargetNetscapeUrl
		};

		// Static :: Properties
		// Static :: Properties :: DragEntries
		public static TargetEntry [] DragEntries {
			get { return drag_entries; }
		}

		// Static :: Methods
		// Static :: Methods :: HandleDrop
		//	TODO: Refactor
		/// <summary>
		///	Handle a Drag-and-Drop event.
		/// </summary>
		/// <remarks>
		///	This is called from <see cref="AddAlbumWindow" /> when
		///	a cover is dropped there, and also from 
		///	<see cref="OnDragDataReceived" />
		///	when one is dropped somewhere else (e.g. in the
		///	<see cref="PlaylistWindow" />).
		/// </remarks>
		/// <param name="song">
		///	The <see cref="Song" /> which the cover is associated with.
		/// </param>
		/// <param name="args">
		///	The <see cref="DragDataReceivedArgs" />.
		/// </param>
		public static void HandleDrop (Song song, DragDataReceivedArgs args)
		{
			string data = DndUtils.SelectionDataToString (args.SelectionData);

			bool success = false;

			string [] uri_list;
			string fn;
			
			switch (args.Info) {
			case (uint) DndUtils.TargetType.Uri:
				uri_list = Regex.Split (data, "\n");
				fn = uri_list [0];
				
				Uri uri = new Uri (fn);

				if (uri.Scheme != "http")
					break;

				if (song.HasAlbum) {
					Album a = Global.DB.GetAlbum (song);
					a.SetCoverWeb (uri.AbsoluteUri);

				} else {
					song.SetCoverWeb (uri.AbsoluteUri);
				}

				success = true;

				break;
				
			case (uint) DndUtils.TargetType.UriList:
				uri_list = DndUtils.SplitSelectionData (data);
				fn = Gnome.Vfs.Uri.GetLocalPathFromUri (uri_list [0]);

				if (fn == null)
					break;

				try {
					if (song.HasAlbum) {
						Album a = Global.DB.GetAlbum (song);
						a.SetCoverLocal (fn);

					} else {
						song.SetCoverLocal (fn);
					}
						
					success = true;

				} catch {
					success = false;
				}
				
				break;

			default:
				break;
			}

			Gtk.Drag.Finish (args.Context, success, false, args.Time);
		}

		// Objects
		private Gtk.Image image;
		private Song song;
		
		// Constructor
		/// <summary>
		///	Create a new <see cref="CoverImage" />.
		/// </summary>
		public CoverImage () : base ()
		{
			image = new Gtk.Image ();	
			image.SetSizeRequest (CoverDatabase.CoverSize, 
					      CoverDatabase.CoverSize);
			
			Add (image);

			DragDataReceived += OnDragDataReceived;

			Global.CoverDB.DoneLoading += OnCoversDoneLoading;
		}

		// Destructor
		~CoverImage ()
		{
			Dispose ();
		}

		// Properties
		// Properties :: Song (set;)
		//	TODO: Add get?
		/// <summary>
		///	The <see cref="Song" /> associated with this cover.
		/// </summary>
		/// <param name="value">
		///	A <see cref="Song" />, or null to use a default cover.
		/// </param>
		public Song Song {
			set {
				song = value;
				Sync ();
			}
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: Sync
		/// <summary>
		///	Synchronizes the image with the desired cover.
		/// </summary>
		/// <remarks>
		///	If a cover is located in the database that is associated
		///	with the same song that we are associated with, use that.
		///	If we are associated with a song whose cover is currently
		///	being downloaded, show a temporary cover. If we have no
		///	associated song, use a default cover.
		/// </remarks>
		private void Sync ()
		{
			// Image
			if (song != null && song.CoverImage != null) {
				image.Pixbuf = song.CoverImage;

			} else if (song != null && Global.CoverDB.Loading) {
				image.Pixbuf = Global.CoverDB.DownloadingPixbuf;

			} else {
				image.SetFromStock
				  ("muine-default-cover", StockIcons.CoverSize);
			}

			// DnD Entries
			TargetEntry [] entries;
			
			if (song != null && !Global.CoverDB.Loading)
				entries = drag_entries;
			else
				entries = null;

			// DnD Destination
			Gtk.Drag.DestSet
			  (this, DestDefaults.All, entries, Gdk.DragAction.Copy);
		}

		// Handlers
		// Handlers :: OnDragDataReceived
		/// <summary>
		///	Handler called when Drag-and-Drop data is received.
		/// </summary>
		/// <remarks>
		///	This just calls <see cref="HandleDrop" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="DragDataReceivedArgs" />.
		/// </param>
		private void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			HandleDrop (song, args);
		}

		// Handlers :: OnCoversDoneLoading
		/// <summary>
		///	Handler called when covers are done loading.
		/// </summary>
		/// <remarks>
		///	This just calls <see cref="Sync" />.
		/// </remarks>
		private void OnCoversDoneLoading ()
		{
			Sync ();
		}
	}
}
