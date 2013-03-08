/*
 * Copyright (C) 2003, 2004, 2005 Jorn Baayen <jorn.baayen@gmail.com>
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
using System.Globalization;

using Gdk;

using Muine.PluginLib;

namespace Muine
{
	public class Song : Item, ISong
	{
		// Static
		// Static :: Variables
		private static Hashtable pointers =
		  Hashtable.Synchronized (new Hashtable ());

		private static IntPtr cur_ptr = IntPtr.Zero;

		// Static :: Methods
		// Static :: Methods :: Public
		// Static :: Methods :: Public :: FromHandle
		public static Song FromHandle (IntPtr handle)
		{
			return (Song) pointers [handle];
		}

		// Objects
		private Gdk.Pixbuf cover_image;
		private ArrayList handles;

		// Variables
		private string    filename;
		private string    title;
		private string [] artists;
		private string [] performers;
		private string    album;
		private int       track_number;
		private int       n_album_tracks;
		private int       disc_number;
		private string    year;
		private int       duration;
		private double    gain;
		private double    peak;

		private int mtime;

		private bool dead = false;
	
		// Constructor
		public Song (string fn)
		{
			filename = fn;

			Metadata metadata = new Metadata (filename);

			Sync (metadata);

			handles = new ArrayList ();

			RegisterHandle ();
		}

		public Song (string fn, IntPtr data)
		{
			IntPtr p = data;

			filename = fn;

			// Tags
			p = Database.UnpackString      (p, out title         );
			p = Database.UnpackStringArray (p, out artists       );
			p = Database.UnpackStringArray (p, out performers    );
			p = Database.UnpackString      (p, out album         );
			p = Database.UnpackInt         (p, out track_number  );
			p = Database.UnpackInt         (p, out n_album_tracks);
			p = Database.UnpackInt         (p, out disc_number   );
			p = Database.UnpackString      (p, out year          );
			p = Database.UnpackInt         (p, out duration      );
			p = Database.UnpackInt         (p, out mtime         );
			p = Database.UnpackDouble      (p, out gain          );
			p = Database.UnpackDouble      (p, out peak          );

			// cover image is loaded later

			handles = new ArrayList ();

			RegisterHandle ();
		}

		// Properties
		// Properties :: Filename (get;)
		public string Filename {
			get { return filename; }
		}

		// Properties :: Folder (get;)
		public string Folder {
			get { return Path.GetDirectoryName (filename); }
		}

		// Properties :: Public (get;)
		public override bool Public {
			get { return true; }
		}
		
		// Properties :: Title (get;)
		public string Title {
			get { return title; }
		}

		// Properties :: Artists (get;)
		public string [] Artists {
			get { return artists; }
		}

		// Properties :: Performers (get;)
		public string [] Performers {
			get { return performers; }
		}

		// Properties :: Album (set; get;)
		//	The setter is only for simple memory usage optimization,
		//	therefore we don't emit a changed signal
		public string Album {
			set { album = value; }
			get { return album;  }
		}

		// Properties :: HasAlbum (get;)
		public bool HasAlbum {
			get { return (album != null && album.Length > 0); }
		}

		// Properties :: TrackNumber (get;)
		public int TrackNumber {
			get { return track_number; }
		}

		// Properties :: NAlbumTracks (get;)
		public int NAlbumTracks {
			get { return n_album_tracks; }
		}

		// Properties :: DiscNumber (get;)
		public int DiscNumber {
			get { return disc_number; }
		}

		// Properties :: Year (get;)
		//	The setter is only for simple memory usage optimization,
		//	therefore we don't emit a changed signal
		public string Year {
			set { year = value; }
			get { return year;  }
		}

		// Properties :: Duration (set; get;)
		// 	We have a setter too, because sometimes we want to 
		//	correct the duration.
		public int Duration {
			set {
				duration = value;
				Global.DB.EmitSongChanged (this);
			}
		
			get { return duration; }
		}

		// Properties :: CoverImage (set; get;)
		public override Gdk.Pixbuf CoverImage {
			set {
				cover_image = value;
				Global.DB.EmitSongChanged (this);
			}
		
			get { return cover_image; }
		}

		// Properties :: MTime (get;)
		public int MTime {
			get { return mtime; }
		}

		// Properties :: Gain (get;)
		public double Gain {
			get { return gain; }
		}

		// Properties :: Peak (get;)
		public double Peak {
			get { return peak; }
		}

		// Properties :: AlbumKey (get;)
		public string AlbumKey {
			get { return Global.DB.MakeAlbumKey (Folder, album); }
		}

		// Properties :: Dead (get;)
		public bool Dead {
			get { return dead; }
		}

		// Properties :: Handle (get;)
		public override IntPtr Handle {
			get { return (IntPtr) handles [0]; }
		}

		// Properties :: Handles (get;)
		public ArrayList Handles {
			get { return handles; }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: SetCoverImageQuiet
		public void SetCoverImageQuiet (Pixbuf cover_image)
		{
			this.cover_image = cover_image;
		}

		// Methods :: Public :: Deregister
		public override void Deregister ()
		{
			dead = true;

			pointers.Remove (this.Handle);

			foreach (IntPtr extra_handle in handles)
				pointers.Remove (extra_handle);
				
			if (!HasAlbum && !FileUtils.IsFromRemovableMedia (filename))
				Global.CoverDB.RemoveCover (filename);
		}

		// Methods :: Public :: RegisterHandle
		// 	Support for having multiple handles to the same song,
		// 	used for, for example, having the same song in the 
		//	playlist more than once.
		public IntPtr RegisterHandle ()
		{
			cur_ptr = new IntPtr (((int) cur_ptr) + 1);
			pointers [cur_ptr] = this;

			handles.Add (cur_ptr);

			return cur_ptr;
		}
	
		// Methods :: Public :: RegisterExtraHandle
		public IntPtr RegisterExtraHandle ()
		{
			return RegisterHandle ();
		}

		// Methods :: Public :: UnregisterExtraHandle
		public void UnregisterExtraHandle (IntPtr handle)
		{
			handles.Remove (cur_ptr);
			pointers.Remove (handle);
		}

		// Methods :: Public :: IsExtraHandle
		public bool IsExtraHandle (IntPtr h)
		{
			return ((pointers [h] == this) && (Handle != h));
		}

		// Methods :: Public :: Sync
		public void Sync (Metadata metadata)
		{
			bool had_album = HasAlbum;

			if (metadata.Title.Length > 0)
				title = metadata.Title;
			else
				title = Path.GetFileNameWithoutExtension (filename);
			
			artists        = metadata.Artists;
			performers     = metadata.Performers;
			album          = metadata.Album;
			track_number   = metadata.TrackNumber;
			n_album_tracks = metadata.TotalTracks;
			disc_number    = metadata.DiscNumber;
			year           = metadata.Year;
			duration       = metadata.Duration;
			mtime          = metadata.MTime;
			gain           = metadata.Gain;
			peak           = metadata.Peak;

			// We really need to do this here. It is ugly, we would
			// like to keep all album cover stuff to the album class,
			// but we can't, as cover image metadata just is stored
			// in the song file itself.
			GetCover (metadata, had_album);

			sort_key   = null;
			search_key = null;
		}

		// Methods :: Private :: GetCover
		private void GetCover (Metadata metadata, bool had_album)
		{
			// We need to do cover stuff here too, as we support 
			// setting covers to songs that are not associated with
			// any album. and, we also need this to support ID3 
			// embedded cover images.
			//
			// Note that CoverDB has the required thread safety for
			// us to be able to do this from a thread.
			//
			// Also, this is safe here as Sync () is only called for new or 
			// actually changed songs. Never from db.Load.
			if (!had_album && HasAlbum && cover_image != null) {

				// This used to be a single song, but not anymore, and it does
				// have a cover - migrate the cover to the album, if there is
				// none there yet
				Global.CoverDB.RemoveCover (filename);

				string akey = AlbumKey;
				if (Global.CoverDB.Covers [akey] == null)
					Global.CoverDB.SetCover (akey, cover_image);

			// See if there is a cover for this single song
			} else if (!HasAlbum) { 
				cover_image = (Pixbuf) Global.CoverDB.Covers [filename];
			}
			
			if (cover_image == null && metadata.AlbumArt != null) {
				// Look for an ID3 embedded cover image, if it 
				// is there, and no cover image is set yet, set
				// it as cover image if it is a single song, or
				// as album cover image if it belongs to an album 
				string key = HasAlbum ? AlbumKey : filename;

				if (Global.CoverDB.Covers [key] == null) {
					cover_image =
					  Global.CoverDB.Getter.GetEmbedded
					    (key, metadata.AlbumArt);
				}

				// Album itself will pick up change when this 
				// song is added to it
			}
		}

		// Methods :: Public :: Pack
		public IntPtr Pack (out int length)
		{
			IntPtr p;
			
			p = Database.PackStart ();

			Database.PackString      (p, title         );
			Database.PackStringArray (p, artists       );
			Database.PackStringArray (p, performers    );
			Database.PackString      (p, album         );
			Database.PackInt         (p, track_number  );
			Database.PackInt         (p, n_album_tracks);
			Database.PackInt         (p, disc_number   );
			Database.PackString      (p, year          );
			Database.PackInt         (p, duration      );
			Database.PackInt         (p, mtime         );
			Database.PackDouble      (p, gain          );
			Database.PackDouble      (p, peak          );

			return Database.PackEnd (p, out length);
		}

		// Methods :: Public :: SetCoverLocal
		// 	Only call if it is a single song
		public void SetCoverLocal (string file)
		{
			CoverImage = Global.CoverDB.Getter.GetLocal (filename, file);
		}

		// Methods :: Public :: SetCoverWeb
		// 	Only call if it is a single song
		public void SetCoverWeb (string url)
		{
			CoverImage = Global.CoverDB.Getter.GetWeb (filename, url,
				new CoverGetter.GotCoverDelegate (OnGotCover));
		}

		// Methods :: Protected
		// Methods :: Protected :: GenerateSortKey (Item)
		protected override SortKey GenerateSortKey ()
		{
			string a = String.Join (" ", artists);
			string p = String.Join (" ", performers);

			string key = String.Format ("{0} {1} {2}", title, a, p);
				
			return CultureInfo.CurrentUICulture.CompareInfo.GetSortKey
			  (key, CompareOptions.IgnoreCase);
		}

		// Methods :: Protected :: GenerateSearchKey (Item)
		protected override string GenerateSearchKey ()
		{
			string a = String.Join (" ", artists);
			string p = String.Join (" ", performers);
				
			string key =
			  String.Format ("{0} {1} {2} {3}", title, a, p, album);

			return StringUtils.SearchKey (key);
		}
		
		// Handlers
		// Handlers :: OnGotCover
		private void OnGotCover (Pixbuf pixbuf)
		{
			CoverImage = pixbuf;
		}
	}
}
