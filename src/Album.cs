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
using System.Globalization;

using Gdk;

using Mono.Unix;

namespace Muine
{
	public class Album : Item
	{
		// Strings
		// Strings :: Prefixes
		/// <summary>
		/// Space-separated list of prefixes that will be taken off the front
		/// when sorting.
		/// </summary>
		/// <remarks>
		///	For example, "The Beatles" will be sorted as "Beatles",
		/// if "the" is included in this list. Also include the English "the"
		///	if English is generally spoken in your country.
		/// </summary>
		private static readonly string string_prefixes = 
			Catalog.GetString ("the dj");

		// Static
		// Static :: Variables
		private static Hashtable pointers = new Hashtable ();
		private static IntPtr cur_ptr = IntPtr.Zero;

		// Static :: Methods
		// Static :: Methods :: FromHandle
		/// <summary>
		/// 	Returns the Album associated with the given handle.
		/// </summary>
		/// <param name="handle">
		///	The <see cref="IntPtr">handle</see> for the Album.
		/// </param>
		/// <returns>
		///	The associated <see cref="Album">album</see>.
		/// </returns>
		public static Album FromHandle (IntPtr handle)
		{
			return (Album) pointers [handle];
		}

		// Objects
		private IComparer  song_comparer = new SongComparer ();
		private Gdk.Pixbuf cover_image;

		// Variables
		private string name;
		private ArrayList songs      = new ArrayList ();
		private ArrayList artists    = new ArrayList ();
		private ArrayList performers = new ArrayList ();
		private string year;
		private string folder;
		private int n_tracks;
		private int total_n_tracks;
		private bool complete = false;
		
		// Constructor
		/// <summary>
		///	Creates a new <see cref="Album" /> object.
		/// </summary>
		/// <param name="initial_song">
		///	The first <see cref="Song" /> in the album.
		/// </param>
		/// <param name="check_cover">
		///	Whether the cover should be found or not.
		/// </param>
		public Album (Song initial_song, bool check_cover)
		{
			songs.Add (initial_song);

			AddArtistsAndPerformers (initial_song);

			name = initial_song.Album;
			year = initial_song.Year;

			folder = initial_song.Folder;

			n_tracks = 1;
			total_n_tracks = initial_song.NAlbumTracks;

			CheckCompleteness ();

			cur_ptr = new IntPtr (((int) cur_ptr) + 1);
			pointers [cur_ptr] = this;
			base.handle = cur_ptr;

			if (check_cover) {
				cover_image = GetCover (initial_song);
				initial_song.SetCoverImageQuiet (cover_image);
			}
		}

		// Properties
		// Properties :: Name (get;)
		/// <summary>
		///	The album title.
		/// </summary>
		/// <returns>
		///	A <see cref="String" />.
		/// </returns>
		public string Name {
			get { return name; }
		}

		// Properties :: Songs (get;)
		/// <summary>
		///	The <see cref="Song">Songs</see> on this album.
		/// </summary>
		/// <returns>
		///	A <see cref="ArrayList" /> of <see cref="Song">Songs</see>.
		/// </returns>
		public ArrayList Songs {
			get {
				lock (this)
					return (ArrayList) songs.Clone ();
			}
		}

		// Properties :: Artists (get;)
		/// <summary>
		///	The artists who appear on this album.
		/// </summary>
		/// <returns>
		///	An <see cref="Array" /> of <see cref="String">strings</see>.
		/// </returns>
		public string [] Artists {
			get {
				lock (this)
					return (string []) artists.ToArray (typeof (string));
			}
		}

		// Properties :: Performers (get;)
		/// <summary>
		///	The performers who appear on this album.
		/// </summary>
		/// <returns>
		///	An <see cref="Array" /> of <see cref="String">strings</see>.
		/// </returns>
		public string [] Performers {
			get {
				lock (this)
					return (string []) performers.ToArray (typeof (string));
			}
		}

		// Properties :: Year (get;)
		/// <summary>
		///	The year this album was released.
		/// </summary>
		/// <returns>
		///	The year as a <see cref="String" />.
		/// </returns>
		public string Year {
			get { return year; }
		}

		// Properties :: CoverImage (set; get;) (Item)
		/// <summary>
		///	The <see cref="Gdk.Pixbuf" /> of the cover.
		/// </summary>
		/// <param name="value">
		///	A <see cref="Gdk.Pixbuf" /> for use as the cover.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> of the cover.
		/// </returns>
		public override Gdk.Pixbuf CoverImage {
			set {
				cover_image = value;

				foreach (Song s in songs)
					s.CoverImage = value;

				Global.DB.EmitAlbumChanged (this);
			}

			get { return cover_image; }
		}

		// Properties :: Public (get;) (Item)
		//	TODO: Rename to Complete.
		/// <summary>
		///	Whether the Album is complete.
		/// </summary>
		/// <returns>
		///	True if album is complete, False otherwise.
		/// </returns>
		public override bool Public {
			get {
				if (Global.DB.OnlyCompleteAlbums)
					return complete;
				else
					return true;
			}
		}

		// Properties :: Key (get;)
		/// <summary>
		///	The key used to reference this album.
		/// </summary>
		/// <returns>
		///	The Album Key generated by 
		///	<see cref="SongDatabase.MakeAlbumKey" />.
		/// </returns>
		public string Key {
			get { return Global.DB.MakeAlbumKey (folder, name); }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Add
		/// <summary>
		///	Add a <see cref="Song" /> to the album.
		/// </summary>
		/// <param name="song">
		///	The <see cref="Song" /> to add.
		/// </param>
		/// <param name="check_cover">
		///	Whether to check if a cover needs to be retrieved.
		/// </param>
		/// <param name="changed">
		///	Return location for whether anything changed.
		/// </param>
		/// <param name="songs_changed">
		///	Return location for whether all songs changed 
		///	(e.g. the cover changed)
		/// </param>
		public void Add (Song song, bool check_cover,
				 out bool changed, out bool songs_changed)
		{
			changed = false;
			songs_changed = false;

			lock (this) {
			
				// Cover
				if (check_cover) {
					if (cover_image == null && song.CoverImage != null) {

						// This is to pick up any embedded album covers
						changed = true;
						songs_changed = true;
				
						cover_image = song.CoverImage;
						foreach (Song s in Songs)
							s.SetCoverImageQuiet (cover_image);

					} else {
						song.SetCoverImageQuiet (cover_image);
					}
				}

				// Year
				if (year.Length == 0 && song.Year.Length > 0) {
					year = song.Year;
					changed = true;

				} else {
					song.Year = year;
				}

				// Name
				if (name != song.Album) {
					name = song.Album;
					changed = true;

				} else {
					song.Album = name;
				}

				// Artists / Performers
				bool artists_changed = AddArtistsAndPerformers (song);
				if (artists_changed)
					changed = true;

				songs.Add (song);
				songs.Sort (song_comparer);

				// Tracks
				if (total_n_tracks != song.NAlbumTracks &&
				    song.NAlbumTracks > 0) {
					total_n_tracks = song.NAlbumTracks;

					changed = true;
				}

				n_tracks ++;

				// Complete?
				bool complete_changed = CheckCompleteness ();
				if (complete_changed)
					changed = true;
			}
		}

		// Methods :: Public :: Remove
		/// <summary>
		/// 	Remove a <see cref="Song" /> from the album.
		/// </summary>
		/// <param name="song">
		///	The <see cref="Song" /> to remove.
		/// </param>
		/// <param name="changed">
		///	Return location for whether anything changed.
		/// </param>
		/// <param name="empty">
		///	Return location for whether the album is now empty.
		/// </param>
		public void Remove (Song song, out bool changed, out bool empty)
		{
			changed = false;

			lock (this) {
				n_tracks --;

				// Complete?
				bool complete_changed = CheckCompleteness ();
				if (complete_changed)
					changed = true;

				// Remove Song
				songs.Remove (song);

				// Artists
				bool artists_changed = RemoveArtistsAndPerformers (song);
				if (artists_changed)
					changed = true;

				// Remove if empty
				empty = (n_tracks == 0);
				if (empty && !FileUtils.IsFromRemovableMedia (folder))
					Global.CoverDB.RemoveCover (this.Key);
			}
		}

		// Methods :: Public :: SetCoverLocal
		/// <summary>
		/// 	Set the cover to a local file.
		/// </summary>
		/// <param name="file">
		///	The location of the file to be used as the cover.
		/// </param>
		public void SetCoverLocal (string file)
		{
			CoverImage = Global.CoverDB.Getter.GetLocal (this.Key, file);
		}

		// Methods :: Public :: SetCoverWeb
		/// <summary>
		///	Set the cover to be downloaded from a URL.
		/// </summary>
		/// <param name="url">
		///	The URL of the file to be used as the cover.
		/// </param>
		public void SetCoverWeb (string url)
		{
			CoverImage = Global.CoverDB.Getter.GetWeb (this.Key, url,
				new CoverGetter.GotCoverDelegate (OnGotCover));
		}

		// Methods :: Public :: Deregister (Item)
		/// <summary>
		///	Remove the current Album from the collection of
		///	all album handles.
		/// </summary>
		/// <remarks>
		///	Call this when an album is removed, so it is no longer
		///	accessible.
		/// </remarks>
		public override void Deregister ()
		{
			pointers.Remove (base.handle);
		}

		// Methods :: Protected
		// Methods :: Protected :: GenerateSortKey (Item)
		/// <summary>
		///	Generate the key used for sorting albums.
		/// </summary>
		/// <remarks>
		///	<para>
		///	If there are three or fewer artists on the album,
		///	they key is a space-separated combination of
		///	artists, performers, year, and name. Thus the album
		///	will be sorted by artist.
		///	</para>
		///
		///	<para>
		///	If there are more than three artists on the album,
		///	the key is a space-separated combination of
		///	name, year, artists, and performers. Thus the album
		///	will be sorted by name.
		///	</para>
		/// </remarks>
		/// <returns>
		///	A <see cref="System.Globalization.SortKey" />.
		/// </returns>
		protected override SortKey GenerateSortKey ()
		{
			// Prefixes
			string [] prefixes = string_prefixes.Split (' ');

			// FIXME: Refactor Artists and Performers function

			// Artists
			string [] p_artists = new string [artists.Count];

			for (int i = 0; i < artists.Count; i ++) {
				string artist_tmp = (string) artists [i];
				string artist = artist_tmp.ToLower ();

				p_artists [i] = artist;
				
				foreach (string prefix in prefixes) {
					bool has_prefix = artist.StartsWith (prefix + " ");
					if (!has_prefix)
						continue;

					p_artists [i] =
					  StringUtils.PrefixToSuffix (artist, prefix);

					break;
				}
			}

			string a = String.Join (" ", p_artists);

			// Performers
			string [] p_performers = new string [performers.Count];

			for (int i = 0; i < performers.Count; i ++) {
				string performer_tmp = (string) performers [i];
				string performer = performer_tmp.ToLower ();

				p_performers [i] = performer;				
			
				foreach (string prefix in prefixes) {
					bool has_prefix = performer.StartsWith (prefix + " ");
					if (!has_prefix)
						continue;

					p_performers [i] =
					  StringUtils.PrefixToSuffix (performer, prefix);

					break;
				}
			}

			string p = String.Join (" ", p_performers);

			// Key
			string key;

			if (artists.Count > 3)
				key = String.Format ("{0} {1} {2} {3}", name, year, a, p);
			else
				key = String.Format ("{0} {1} {2} {3}", a, p, year, name);

			return CultureInfo.CurrentUICulture.CompareInfo.GetSortKey
			  (key, CompareOptions.IgnoreCase);
		}

		// Methods :: Protected :: GenerateSearchKey (Item)
		/// <summary>
		///	Generates a key used in searching.
		/// </summary>
		/// <returns>
		///	A search key <see cref="String" />.
		/// </returns>
		/// <seealso cref="StringUtils.SearchKey" />.
		protected override string GenerateSearchKey ()
		{
			string a = String.Join (" ", this.Artists   );
			string p = String.Join (" ", this.Performers);

			string key = String.Format ("{0} {1} {2}", name, a, p);

			return StringUtils.SearchKey (key);
		}

		// Methods :: Private
		// Methods :: Private :: AddArtistsAndPerformers
		/// <summary>
		///	Add the artists and performers on a song to the list
		///	of all artists and performes on the album.
		/// </summary>
		/// <param name="song">
		///	The <see cref="Song" /> from which to get artist and
		///	performer names.
		/// </param>
		/// <returns>
		///	True if things changed, False otherwise.
		/// </returns>
		private bool AddArtistsAndPerformers (Song song)
		{
			// FIXME: Refactor Artists and Performers function

			// Artists			
			bool artists_changed = false;

			foreach (string artist in song.Artists) {
				if (artists.Contains (artist))
					continue;
				
				artists.Add (artist);
				artists_changed = true;
			}

			if (artists_changed)
				artists.Sort ();

			// Performers
			bool performers_changed = false;

			foreach (string performer in song.Performers) {
				if (performers.Contains (performer))
					continue;

				performers.Add (performer);
				performers_changed = true;
			}

			if (performers_changed)
				performers.Sort ();

			// If changed, clear keys
			bool changed = (artists_changed || performers_changed);

			if (changed) {
				search_key = null;
				sort_key   = null;
			}

			return changed;
		}

		// Methods :: Private :: RemoveArtistsAndPerformers
		/// <summary>
		///	Remove the artists and performers on a song from the list
		///	of all artists and performers on the album.
		/// </summary>
		/// <param name="song">
		///	The <see cref="Song" /> from which to get artist and
		//	performer names.
		/// </param>
		/// <returns>
		///	True if things changed, False otherwise.
		/// </returns>
		private bool RemoveArtistsAndPerformers (Song song)
		{
			// FIXME: Refactor Artists and Performers function

			// Artists
			bool artists_changed = false;
			
			foreach (string artist in song.Artists) {
				bool found = false;

				foreach (Song s in songs) {
					foreach (string s_artist in s.Artists) {
						if (artist != s_artist)
							continue;

						found = true;
						break;
					}

					if (found)
						break;
				}

				if (!found) {
					artists.Remove (artist);
					artists_changed = true;
				}
			}

			// Performers
			bool performers_changed = false;

			foreach (string performer in song.Performers) {
				bool found = false;

				foreach (Song s in songs) {
					foreach (string s_performer in s.Performers) {
						if (performer != s_performer)
							continue;

						found = true;
						break;
					}

					if (found)
						break;
				}

				if (!found) {
					performers.Remove (performer);
					performers_changed = true;
				}
			}

			// If changed, reset keys
			bool changed = (artists_changed || performers_changed);

			if (changed) {
				search_key = null;
				sort_key   = null;
			}

			return changed;
		}

		// Methods :: Private :: GetCover
		/// <summary>
		///	Try to add a cover to the album using a variety of methods.
		/// </summary>
		/// <remarks>
		///	First, the Database is checked to see if a cover is 
		///	already present. Then, the song given is checked for an
		///	embedded image. Then, the directory is searched for a 
		///	image file with a name commonly used for cover images.
		///	Finally, if those methods fail, Amazon.com is searched.
		/// </remarks>
		/// <param name="initial_song">
		///	The <see cref="Song" /> which should be searched for
		///	an embedded image.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> of the cover.
		/// </returns>
		private Pixbuf GetCover (Song initial_song)
		{
			string key = this.Key;
			Gdk.Pixbuf pixbuf;
			
			// Database
			pixbuf = (Pixbuf) Global.CoverDB.Covers [key];
			if (pixbuf != null)
				return pixbuf;

			// Embedded
			pixbuf = initial_song.CoverImage;
			if (pixbuf != null)
				return pixbuf;

			// Folder
			pixbuf = Global.CoverDB.Getter.GetFolderImage (key, folder);
			if (pixbuf != null)
				return pixbuf;

			// Amazon
			return Global.CoverDB.Getter.GetAmazon (this);
		}

		// Methods :: Private :: HaveHalfAlbum
		/// <summary>
		///	Check if at least half the songs that should be on the
		///	album are present.
		/// </summary>
		/// <returns>
		///	True if at least half of the songs on the album are
		///	present, False otherwise.
		/// </returns>
		private bool HaveHalfAlbum ()
		{
			return HaveHalfAlbum (total_n_tracks, n_tracks);
		}
		
		/// <summary>
		///	Check if at least half the songs that should be on the
		///	album are present.
		/// </summary>
		/// <param name="total">
		///	The total number of tracks that should be on the album.
		/// </param>
		/// <param name="have">
		///	The number of tracks that are present.
		/// </param>
		/// <returns>
		///	True if at least half of the songs on the album are
		///	present, False otherwise.
		/// </returns>
		private bool HaveHalfAlbum (int total, int have)
		{
			return (have >= total / 2);
		}

		// Methods :: Private :: CheckCompleteness
		/// <summary>
		///	Check if the album is now complete enough to be listed
		///	in the <see cref="AddAlbumWindow" />.
		/// </summary>
		/// <remarks>
		///	<para>
		///	If the album appears to be only one song long, it is
		///	considered a complete album if it is at least 10 
		///	minutes long.
		///	</para>
		///
		///	<para>
		///	An album is only considered complete if the track
		///	number of the last song we have is at least 8.
		///	</para>
		/// </remarks>
		/// <returns>
		///	True if completeness has changed, False otherwise.
		/// </returns>
		private bool CheckCompleteness ()
		{
			bool new_complete = false;

			if (total_n_tracks > 0) {
				int delta = total_n_tracks - n_tracks;
				new_complete = (delta <= 0) ? true : HaveHalfAlbum ();

			} else {
				// Take track number of last song
				Song last_song = (Song) songs [songs.Count - 1];
				int last_track = last_song.TrackNumber;

				if (last_track == 1) {
					// Check for one-song album (song > 10 min)
					if (n_tracks == 1 && last_song.Duration >= 600)
						new_complete = true;

				} else if (last_track > 1) {
					int delta = last_track - n_tracks;
					
					if (delta <= 0)
						new_complete = true;

					else if (last_track >= 8)
						new_complete = HaveHalfAlbum (last_track, n_tracks);
				}
			}

			// Changed?
			bool changed = (new_complete != complete);			
			complete = new_complete;
			
			return changed;
		}

		// Handlers
		// Handlers :: OnGotCover
		/// <summary>
		///	Handler called when a cover has been found.
		/// </summary>
		/// <remarks>
		///	Sets the cover to the new cover.
		/// </remarks>
		/// <param name="pixbuf">
		///	A <see cref="Gdk.Pixbuf" /> of the new cover.
		/// </param>
		private void OnGotCover (Pixbuf pixbuf)
		{
			CoverImage = pixbuf;
		}

		// Internal Classes
		// Internal Classes :: SongComparer
		private class SongComparer : IComparer {
		
			// Methods
			// Methods :: Compare (IComparer)
			/// <summary>
			///	Compares two <see cref="Song">Songs</see>.
			/// </summary>
			/// <remarks>
			///	<see cref="Song">Songs</see> are sorted by disc
			///	number and then by track number.
			/// </remarks>
			/// <param name="a">
			///	The first <see cref="Song" /> (boxed).
			/// </param>
			/// <param name="b">
			///	The second <see cref="Song" /> (boxed).
			/// </param>
			/// <returns>
			///	-1 if the first Song comes after the second Song.
			///	 0 if the Songs are the same
			///	+1 if the first Song comes before the second Song.
			/// </returns>
			int IComparer.Compare (object a, object b)
			{
				Song song_a = (Song) a;
				Song song_b = (Song) b;

				int ret = song_a.DiscNumber.CompareTo (song_b.DiscNumber);
				
				if (ret == 0)
					ret = song_a.TrackNumber.CompareTo (song_b.TrackNumber);
				
				return ret;
			}
		}
	}
}
