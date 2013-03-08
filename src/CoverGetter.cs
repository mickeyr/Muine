/*
 * Copyright (C) 2005 Jorn Baayen <jorn.baayen@gmail.com>
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
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Collections;

using Gdk;

using musicbrainz;

namespace Muine
{
	public class CoverGetter
	{
		// GConf
		// GConf :: AmazonLocale
		private const string GConfKeyAmazonLocale     = "/apps/muine/amazon_locale";
		private const string GConfDefaultAmazonLocale = "us";
		
		// GConf :: AmazonDevTag
		private const string GConfKeyAmazonDevTag     = "/apps/muine/amazon_dev_tag";
		private	const string GConfDefaultAmazonDevTag = "amazondevtag";

		// Delegates
		public delegate void GotCoverDelegate (Pixbuf pixbuf);

		// Objects
		private CoverDatabase db;
		private GnomeProxy proxy;
		private GetAmazonThread amazon_thread;

		// Variables
		private string amazon_locale;
		private string amazon_dev_tag;
		private bool musicbrainz_lib_missing = false;
		private bool amazon_dev_tag_missing = false;

		// Variables :: Cover Filenames
		//	TODO: Maybe make checking these case-insensitve instead
		//	and split possible extensions from possible filenames.
		private string [] cover_filenames = {
			"cover.jpg" , "Cover.jpg" ,
			"cover.jpeg", "Cover.jpeg",
			"cover.png" , "Cover.png" ,
			"cover.gif" , "Cover.gif" ,
			"folder.jpg", "Folder.jpg"
		};

		// Constructor
		/// <summary>
		///	Creates a new <see cref="CoverGetter" />.
		/// </summary>
		/// <remarks>
		///	This object is used to retrieve album covers in a 
		///	variety of ways.
		/// </remarks>
		/// <param name="db">
		///	The <see cref="CoverDatabase" /> which stores the covers.
		/// </param>
		public CoverGetter (CoverDatabase db)
		{
			this.db = db;
			
			amazon_locale = (string) Config.Get (GConfKeyAmazonLocale, 
				GConfDefaultAmazonLocale);

			Config.AddNotify (GConfKeyAmazonLocale,
				new GConf.NotifyEventHandler (OnAmazonLocaleChanged));

			Config.AddNotify (GConfKeyAmazonDevTag,
				new GConf.NotifyEventHandler (OnAmazonDevTagChanged));

			proxy = new GnomeProxy ();

			amazon_thread = new GetAmazonThread (this);
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: GetLocal
		/// <summary>
		///	Set the cover to a local file.
		/// </summary>
		/// <remarks>
		///	Image is scaled and placed on a background with
		///	<see cref="AddBorder" />.
		/// </remarks>
		/// <param name="key">
		///	Album key.
		/// </param>
		/// <param name="file">
		///	Filename.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" />.
		/// </returns>
		/// <exception cref="GLib.GException">
		///	Thrown if loading file fails.
		/// </exception>
		public Pixbuf GetLocal (string key, string file)
		{
			Pixbuf pix = new Pixbuf (file);
			pix = AddBorder (pix);
			db.SetCover (key, pix);
			return pix;
		}

		// Methods :: Public :: GetEmbedded
		/// <summary>
		///	Set the cover.
		/// </summary>
		/// <remarks>
		///	Image is scaled and placed on a background with
		///	<see cref="AddBorder" />.
		/// </remarks>
		/// <param name="key">
		///	Album key.
		/// </param>
		/// <param name="pixbuf">
		///	The <see cref="Gdk.Pixbuf" /> to use as the
		///	album cover.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" />.
		/// </returns>
		public Pixbuf GetEmbedded (string key, Pixbuf pixbuf)
		{
			Pixbuf pix = AddBorder (pixbuf);
			db.SetCover (key, pix);
			return pix;
		}

		// Methods :: Public :: GetFolderImage
		/// <summary>
		///	Search for the album cover in a folder.
		/// </summary>
		/// <remarks>
		///	Image is scaled and placed on a background with
		///	<see cref="AddBorder" />.
		/// </remarks>
		/// <param name="key">
		///	Album key.
		/// </param>
		/// <param name="folder">
		///	The folder in which to search.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> if a cover is found, null otherwise.
		/// </returns>
		public Pixbuf GetFolderImage (string key, string folder)
		{
			foreach (string fn in cover_filenames) {
				FileInfo cover = new FileInfo (Path.Combine (folder, fn));
				
				if (!cover.Exists)
					continue;

				Pixbuf pix;

				try {
					pix = new Pixbuf (cover.FullName);
				} catch {
					continue;
				}

				pix = AddBorder (pix);
				db.SetCover (key, pix);
				return pix;
			}

			return null;
		}

		// Methods :: Public :: GetWeb
		/// <summary>
		///	Get the cover from a URL.
		/// </summary>
		/// <remarks>
		///	Immediately returns a temporary cover.
		///	The actual downloading occurs in
		///	<see cref="GetWebThread" />.
		/// </remarks>
		/// <param name="key">
		///	Album key.
		/// </param>
		/// <param name="url">
		///	The URL from which to get the cover.
		/// </param>
		/// <param name="done_func">
		///	A <see cref="GotCoverDelegate" />.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> of the temporary cover.
		/// </returns>
		public Pixbuf GetWeb
		  (string key, string url, GotCoverDelegate done_func)
		{
			db.RemoveCover (key);
			new GetWebThread (this, key, url, done_func);
			return db.DownloadingPixbuf;
		}


		// Methods :: Public :: GetAmazon
		/// <summary>
		///	Search for the album cover on Amazon.
		/// </summary>
		/// <remarks>
		///	Immediately returns a temporary cover.
		///	The actual downloading occurs in
		///	<see cref="GetAmazonThread" />.
		/// </remarks>
		/// <param name="album">
		///	An <see cref="Album" />.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> of the temporary cover.
		/// </returns>
		public Pixbuf GetAmazon (Album album)
		{
			return GetAmazon (album, true);
		}
		
		
		/// <summary>
		///	Search for the album cover on Amazon,
		///	optionally marking the covering as being checked.
		/// </summary>
		/// <remarks>
		///	Immediately returns a temporary cover.
		///	The actual downloading occurs in
		///	<see cref="GetAmazonThread" />.
		/// </remarks>
		/// <param name="album">
		///	An <see cref="Album" />.
		/// </param>
		/// <param name="mark">
		///	Whether or not to mark the cover in the database as
		///	currently being downloaded. Normally, this should be
		///	'true' but if you want to avoid modifying the database,
		///	it should be 'false'.
		/// </parm>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> of the temporary cover.
		/// </returns>
		public Pixbuf GetAmazon (Album album, bool mark)
		{
			if (mark)
				db.MarkAsBeingChecked (album.Key);

			amazon_thread.Queue.Enqueue (album);

			return db.DownloadingPixbuf;
		}

		// Methods :: Public :: DownloadFromAmazon
		//	TODO: Refactor this
		/// <summary>
		///	Search for the album cover on Amazon.
		/// </summary>
		/// <remarks>
		///	This should only be called from <see cref="GetAmazonThread" />.
		///	Normally, <see cref="GetAmazon" /> should be used instead.
		/// </remarks>
		/// <param name="album">
		///	An <see cref="Album" />.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> if a cover is found, null otherwise.
		/// </returns>
		public Pixbuf DownloadFromAmazon (Album album)
		{
			Pixbuf pix = DownloadFromAmazonViaMusicBrainz (album);

			return pix != null ?
				pix : DownloadFromAmazonViaAPI (album);
		}
		
		// Methods :: Private :: DownloadFromAmazonViaMusicBrainz
		/// <summary>
		///	Get the cover URL from amazon with the help of libmusicbrainz
		/// </summary>
		/// <remarks>
		///	This should only be called from <see cref="GetWebThread" />
		/// 	and <see cref="DownloadFromAmazon" />. Normally, 
		///	<see cref="GetWeb" /> should be used instead.
		/// </remarks>
		/// <param name="album">
		///	The <see cref="Album"> for which a cover needs to be downloaded.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> if a cover is found, null otherwise.
		/// </returns>
		/// <exception cref="WebException">
		///	Thrown if an error occurred while downloading.
		/// </exception>
		/// <exception cref="GLib.GException">
		///	Thrown if loading file fails.
		/// </exception>
		private Pixbuf DownloadFromAmazonViaMusicBrainz (Album album)
		{
			// Rather than do the lib search and catch the
			// DllNotFoundException every single time,
			// we check a simple bool as a performance helper.
			if (musicbrainz_lib_missing)
				return null;

			Pixbuf pix = null;
			try {
				// Sane album title
				string sane_album_title;
				if (album.Name != null)
					sane_album_title = SanitizeString (album.Name);
				else
					sane_album_title = String.Empty;

				// Sane artist name
				string sane_artist_name;
				if (album.Artists != null && album.Artists.Length > 0)
					sane_artist_name = album.Artists [0].ToLower ();
				else
					sane_artist_name = String.Empty;

				//
				string asin = null;
				
				// TODO: Move to constant
				string AmazonImageUri =
				  "http://images.amazon.com/images/P/{0}.01._SCMZZZZZZZ_.jpg";


				// remove "disc 1" and family
				//  TODO: Make the regexes translatable?
				//  (e.g. "uno|dos|tres...", "les|los..)
				string sane_album_title_regex  = @"[,:]?\s*";
				sane_album_title_regex += @"(cd|dis[ck])\s*";
				sane_album_title_regex += @"(\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s*$";
				sane_album_title =  Regex.Replace (sane_album_title, sane_album_title_regex, String.Empty);

				// Remove "The " and "the " from artist names
				string sane_artist_name_regex = @"^the\s+";
				sane_artist_name = Regex.Replace (sane_artist_name, sane_artist_name_regex, String.Empty);
				
				MusicBrainz c = new MusicBrainz ();
				
				// set the depth of the query
				//   (see http://wiki.musicbrainz.org/ClientHOWTO)
				c.SetDepth(4);

				string [] album_name = new string [] { sane_album_title };

				bool match =
				  c.Query (MusicBrainz.MBQ_FindAlbumByName, album_name);

				if (match) {
					int num_albums =
					  c.GetResultInt (MusicBrainz.MBE_GetNumAlbums);
					
					string fetched_artist_name;
					for (int i = 1; i <= num_albums; i++) {
						c.Select (MusicBrainz.MBS_SelectAlbum, i);

						// gets the artist from the first track of the album
						c.GetResultData
						  (MusicBrainz.MBE_AlbumGetArtistName, 1,
						   out fetched_artist_name);

						// Remove "The " here as well
						if (fetched_artist_name != null) {
							string tmp = fetched_artist_name.ToLower ();
							string fetched_artist_name_regex = @"^the\s+";
							fetched_artist_name = Regex.Replace (tmp, fetched_artist_name_regex, String.Empty);

						} else {
							fetched_artist_name = String.Empty;
						}

						if (fetched_artist_name == sane_artist_name) {
							c.GetResultData
							  (MusicBrainz.MBE_AlbumGetAmazonAsin, out asin);

							break;
						}

						// go back one level so we can select the next album
						c.Select(MusicBrainz.MBS_Back); 
					}
				}

				if (asin == null) {
					pix = null;
				} else {
					string uri = String.Format (AmazonImageUri, asin);
					pix = Download (uri);
				}
				
			} catch (DllNotFoundException) {
				// We catch this exception so we can always include the
				// musicbrainz support but not have a strict compile/runtime
				// requirement on it.
				musicbrainz_lib_missing = true;
			}

			return pix;
		}
		
		// Methods :: Private :: DownloadFromAmazonViaAPI
		/// <summary>
		///   Get the cover URL from amazon usig the Amazon API (required
		///   valid dev tag in GConf)
		/// </summary>
		/// <remarks>
		///   This should only be called from <see cref="GetWebThread" />
		///   and <see cref="DownloadFromAmazon" />. Normally, 
		///	<see cref="GetWeb" /> should be used instead.
		/// </remarks>
		/// <param name="album">
		///	The <see cref="Album"> for which a cover needs to be downloaded.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> if a cover is found, null otherwise.
		/// </returns>
		/// <exception cref="WebException">
		///	Thrown if an error occurred while downloading.
		/// </exception>
		/// <exception cref="GLib.GException">
		///	Thrown if loading file fails.
		/// </exception>
		private Pixbuf DownloadFromAmazonViaAPI (Album album)
		{
			// Don't bother trying if we're missing a valid dev tag
			if (amazon_dev_tag_missing)
				return null;

			Amazon.AmazonSearchService search_service =
			  new Amazon.AmazonSearchService ();

			string sane_album_title = SanitizeString (album.Name);

			// remove "disc 1" and family
			//	TODO: Make the regex translatable? (e.g. "uno|dos|tres...")
			string sane_album_title_regex = @"[,:]?\s*";
			sane_album_title_regex += @"(cd|dis[ck])\s*";
			sane_album_title_regex += @"\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s*$";
			sane_album_title =  Regex.Replace (sane_album_title, sane_album_title_regex, String.Empty); 

			string [] album_title_array = sane_album_title.Split (' ');
			Array.Sort (album_title_array);

			// This assumes the right artist is always in Artists [0]
			string sane_artist = SanitizeString (album.Artists [0]);
			
			// Prepare for handling multi-page results
			int total_pages = 1;
			int current_page = 1;
			int max_pages = 2; // check no more than 2 pages
			
			//Getting the GConf Dev Tag
			amazon_dev_tag = (string) Config.Get (GConfKeyAmazonDevTag, 
			  GConfDefaultAmazonDevTag);
			
			// If we're the default, don't bother searching
			// as Amazon will reject the requests
			if (amazon_dev_tag == GConfDefaultAmazonDevTag) {
				amazon_dev_tag_missing = true;
				return null;
			}

			// Create Encapsulated Request
			Amazon.ArtistRequest asearch = new Amazon.ArtistRequest ();
			asearch.devtag = amazon_dev_tag;
			asearch.artist = sane_artist;
			asearch.keywords = sane_album_title;
			asearch.type = "heavy";
			asearch.mode = "music";
			asearch.tag = "webservices-20";

			// Use selected Amazon service
			switch (amazon_locale) {
			case "uk":
				search_service.Url = "http://soap-eu.amazon.com/onca/soap3";
				asearch.locale = "uk";
				break;

			case "de":
				search_service.Url = "http://soap-eu.amazon.com/onca/soap3";
				asearch.locale = "de";
				break;

			case "jp":
				search_service.Url = "http://soap.amazon.com/onca/soap3";
				asearch.locale = "jp";
				break;

			default:
				search_service.Url = "http://soap.amazon.com/onca/soap3";
				break;
			}

			double best_match_percent = 0.0;
			Pixbuf best_match = null;

			while (current_page <= total_pages && current_page <= max_pages) {
				asearch.page = Convert.ToString (current_page);

				Amazon.ProductInfo pi;
				
				// Amazon API requires this
				Thread.Sleep (1000);
			
				// Web service calls timeout after 30 seconds
				search_service.Timeout = 30000;
				if (proxy.Use)
					search_service.Proxy = proxy.Proxy;
				
				// This may throw an exception, we catch it in the calling
				//   function
				pi = search_service.ArtistSearchRequest (asearch);

				int num_results = pi.Details.Length;
				total_pages = Convert.ToInt32 (pi.TotalPages);

				// Work out how many matches are on this page
				if (num_results < 1)
					return null;

				for (int i = 0; i < num_results; i++) {
					// Ignore bracketed text on the result from Amazon
					
					Amazon.Details details = pi.Details [i];

					string sane_product_name =
					  SanitizeString (details.ProductName);

					// Compare the two strings statistically
					string [] product_name_array =
					  sane_product_name.Split (' ');

					Array.Sort (product_name_array);

					int match_count = 0;
					foreach (string s in album_title_array) {
						if (Array.BinarySearch (product_name_array, s) < 0)
							continue;

						match_count++;
					}

					double match_percent;
					match_percent = match_count / (double) album_title_array.Length;

					if (match_percent < 0.6)
						continue;

					string url = pi.Details [i].ImageUrlMedium;

					if (url == null || url.Length == 0)
						continue;

					double backward_match_percent = 0.0;
					int backward_match_count = 0;

					foreach (string s in product_name_array) {
						if (Array.BinarySearch (album_title_array, s) < 0)
							continue;

						backward_match_count++;
					}

					backward_match_percent = backward_match_count / (double) product_name_array.Length;

					double total_match_percent = match_percent + backward_match_percent;
					if (total_match_percent <= best_match_percent)
						continue; // look for a better match

					Pixbuf pix;
								
					try {
						pix = Download (url);
						if (pix == null && amazon_locale != "us") {
							// Manipulate the image URL since Amazon sometimes
							// return it wrong :(
							// http://www.amazon.com/gp/browse.html/103-1953981-2427826?node=3434651#misc-image
							url = Regex.Replace (url, "[.]0[0-9][.]", ".01.");
							pix = Download (url);
						}

					} catch (WebException) {
						throw;

					} catch (Exception) {
						pix = null;
					}

					if (pix != null) {
						best_match_percent = total_match_percent;
						best_match = pix;

						if (best_match_percent == 2.0)
							return best_match;
					}
				}

				current_page++;
			}

			return best_match;
		}

		// Methods :: Public :: Download
		/// <summary>
		///	Get the cover from a URL.
		/// </summary>
		/// <remarks>
		///	This should only be called from <see cref="GetWebThread" />
		/// 	and <see cref="DownloadFromAmazon" />. Normally, 
		///	<see cref="GetWeb" /> should be used instead.
		/// </remarks>
		/// <param name="url">
		///	The URL from which to get the cover.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> if a cover is found, null otherwise.
		/// </returns>
		/// <exception cref="WebException">
		///	Thrown if an error occurred while downloading.
		/// </exception>
		/// <exception cref="GLib.GException">
		///	Thrown if loading file fails.
		/// </exception>
		public Pixbuf Download (string url)
		{
			Pixbuf cover;

			// read the cover image
			HttpWebRequest req = (HttpWebRequest) WebRequest.Create (url);
			req.UserAgent = "Muine";
			req.KeepAlive = false;
			req.Timeout = 30000; // Timeout after 30 seconds
			if (proxy.Use)
				req.Proxy = proxy.Proxy;
				
			WebResponse resp = null;

			resp = req.GetResponse ();

			Stream s = resp.GetResponseStream ();
		
			cover = new Pixbuf (s);

			resp.Close ();

			// Trap Amazon 1x1 images
			if (cover.Height == 1 && cover.Width == 1)
				return null;

			return cover;
		}

		// Methods :: Public :: AddBorder
		/// <summary>
		///	Scale the image and add a black 1 pixel border to it.
		/// </summary>
		/// <param name="cover">
		///	The <see cref="Gdk.Pixbuf" /> to modify.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> of the modified cover.
		/// </returns>
		public Pixbuf AddBorder (Pixbuf cover)
		{
			Pixbuf border;

			// 1px border, so -2
			int target_size = CoverDatabase.CoverSize - 2;

			// scale the cover image if necessary
			if (cover.Height > target_size || cover.Width > target_size) {
				int new_width, new_height;

				double target_size_d = (double) target_size;
				double width_d  = (double) cover.Width;
				double height_d = (double) cover.Height;

				if (cover.Height > cover.Width) {
					new_width = (int) Math.Round (target_size_d * (width_d / height_d));
					new_height = target_size;

				} else {
					new_height = (int) Math.Round (target_size_d * (height_d / width_d));
					new_width = target_size;
				}

				cover =
				  cover.ScaleSimple
				    (new_width, new_height, InterpType.Bilinear);
			}

			// create the background + black border pixbuf
			border =
			  new Pixbuf
			    (Colorspace.Rgb, true, 8, cover.Width + 2, cover.Height + 2);

			border.Fill (0x000000ff);

			// put the cover image on the border area
			cover.CopyArea (0, 0, cover.Width, cover.Height, border, 1, 1);

			// done
			return border;
		}

		// Methods :: Private
		// Methods :: Private :: SanitizeString
		//	TODO: We should probably also trim the string at the 
		//	end.
		/// <summary>
		/// Sanitize the string for use in <see cref="DownloadFromAmazon" />.
		/// </summary>
		/// <remarks>
		///	The string is made lower-case, all text within 
		///	parentheses or square brackets are discarded. Dashes,
		///	underscores and plus signs are removed.
		/// </remarks>
		/// <param name="s">
		///	The <see cref="String" /> which to modify.
		/// </param>
		/// <returns>
		///	A <see cref="Gdk.Pixbuf" /> of the modified cover.
		/// </returns>
		private string SanitizeString (string s)
		{
			s = s.ToLower ();
			s = Regex.Replace (s, "\\(.*\\)", String.Empty);
			s = Regex.Replace (s, "\\[.*\\]", String.Empty);
			s = s.Replace ("-", " ");
			s = s.Replace ("_", " ");
			s = Regex.Replace (s, " +", " ");

			return s;
		}

		// Handlers
		// Handlers :: OnAmazonLocaleChanged
		/// <summary>
		///	Handler called when the Amazon locale is changed in GConf.
		/// </summary>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="GConf.NotifyEventArgs" />.
		/// </param>
		private void OnAmazonLocaleChanged
		  (object o, GConf.NotifyEventArgs args)
		{
			amazon_locale = (string) args.Value;
		}
		
		// Handlers
		// Handlers :: OnAmazonDevTagChanged
		/// <summary>
		///	Handler called when the Amazon dev tag is changed in GConf.
		/// </summary>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="GConf.NotifyEventArgs" />.
		/// </param>
		private void OnAmazonDevTagChanged
		  (object o, GConf.NotifyEventArgs args)
		{
			amazon_dev_tag = (string) args.Value;

			// Tag is 'missing' if set to the default value
			amazon_dev_tag_missing =
			  (amazon_dev_tag == GConfDefaultAmazonDevTag);
		}

		// Internal Classes
		// Internal Classes :: GetWebThread
		//   FIXME: Split off? This is big.
		private class GetWebThread
		{
			// Delegates
			private GotCoverDelegate done_func;
			
			// Objects
			private CoverGetter getter;
			private Pixbuf pixbuf = null;

			// Variables
			private string key;
			private string url;

			// Constructor
			/// <summary>
			///	Create a new <see cref="GetWebThread" />.
			/// </summary>
			/// <param name="getter">
			///	A <see cref="CoverGetter" />.
			/// </param>
			/// <param name="key">
			/// 	The album key.
			/// </param>
			/// <param name="url">
			///	The URL from which to download the cover.
			/// </param>
			/// <param name="done_func">
			///	The delegate to call when the cover is done downloading.
			/// </param>
			/// <returns>
			///	A <see cref="Gdk.Pixbuf" /> of the modified cover.
			/// </returns>
			public GetWebThread (CoverGetter getter, string key, string url,
			  GotCoverDelegate done_func)
			{
				this.getter = getter;
				this.key = key;
				this.url = url;
				this.done_func = done_func;

				Thread thread = new Thread (new ThreadStart (ThreadFunc));
				thread.IsBackground = true;
				thread.Priority = ThreadPriority.BelowNormal;
				thread.Start ();
			}

			// Methods
			// Methods :: Private
			// Methods :: Private :: SignalIdle
			/// <summary>
			///	Calls the <see cref="GotCoverDelegate" />.
			/// </summary>
			/// <remarks>
			///	<para>
			///	This is the method called when idling. It calls the 
			///	<see cref="GotCoverDelegate" /> because if we're
			///	idling, we must be done downloading.
			///	</para>
			///
			///	<para>
			/// 	This is not entirely safe, as the user can in theory
			/// 	have changed the cover image between the thread and
			/// 	this idle. However, chances are very, very slim and
			/// 	things won't explode if it happens.
			///	</para>
			/// </remarks>
			/// <returns>
			///	False, we only want to do this once.
			/// </returns>
			private bool SignalIdle ()
			{
				done_func (pixbuf);

				return false;
			}			

			// Delegate Functions
			// Delegate Functions :: ThreadFunc
			/// <summary>
			///	Downloads the cover and sets it in the database.
			/// </summary>
			/// <remarks>
			///	This is the main method of the thread.
			/// </remarks>
			private void ThreadFunc ()
			{
				try {
					pixbuf = getter.Download (url);
					pixbuf = getter.AddBorder (pixbuf);
				} catch {
				}

				// Check if cover has been modified while we were downloading
				if (Global.CoverDB.Covers [key] != null)
					return;

				if (pixbuf != null)
					Global.CoverDB.SetCover (key, pixbuf);

				// Also do this if it is null, as we need to remove the
				// downloading image
				GLib.IdleHandler idle = new GLib.IdleHandler (SignalIdle);
				GLib.Idle.Add (idle);
			}
		}

		// Internal Classes :: GetAmazonThread
		// 	FIXME: Split off? This is big.
		/// <summary>
		///	This is used to download covers from Amazon in a threaded
		///	fashion.
		/// </summary>
		/// <remarks>
		///	To download a cover, simply <see cref="Queue.Enqueue" />
		/// 	it on the <see cref="Queue" />.
		/// </remarks>
		private class GetAmazonThread
		{
			// Objects
			private CoverGetter getter;
			private Queue queue;

			// Internal Classes
			// Internal Classes :: IdleData
			// 	FIXME: Internal classes inside internal classes? ick!
			private class IdleData
			{
				// Objects
				private Album album;
				private Pixbuf pixbuf;

				// Constructor
				/// <summary>
				///	Create a new <see cref="IdleData" /> object.
				/// </summary>
				public IdleData (Album album, Pixbuf pixbuf)
				{
					this.album = album;
					this.pixbuf = pixbuf;

					GLib.IdleHandler idle = new GLib.IdleHandler (IdleFunc);
					GLib.Idle.Add (idle);
				}

				// Delegate Functions
				// Delegate Functions :: IdleFunc
				/// <summary>
				///	Sets the cover to the currently held
				///	<see cref="Gdk.Pixbuf" />.
				/// </summary>
				/// <remarks>
				///	<para>
				///	This is the method called when idling. It is used
				///	to flush the covers in the queue to the database.
				///	</para>
				///
				///	<para>
				/// 	This is not entirely safe, as the user can in theory
				/// 	have changed the cover image between the thread and
				/// 	this idle. However, chances are very, very slim and
				/// 	things won't explode if it happens.
				///	</para>
				/// </remarks>
				/// <returns>
				///	False, we only want to do this once.
				/// </returns>
				private bool IdleFunc ()
				{
					album.CoverImage = pixbuf;
		
					return false;
				}
			}

			// Constructor
			/// <summary>
			///	Create a new <see cref="GetAmazonThread" />.
			/// </summary>			
			public GetAmazonThread (CoverGetter getter)
			{
				this.getter = getter;

				queue = Queue.Synchronized (new Queue ());

				Thread thread = new Thread (new ThreadStart (ThreadFunc));
				thread.IsBackground = true;
				thread.Priority = ThreadPriority.BelowNormal;
				thread.Start ();
			}

			// Properties
			// Properties :: Queue (get;)
			/// <summary>
			///	The <see cref="Queue" />, which holds albums to
			///	download.
			/// </summary>
			/// <returns>
			/// 	A <see cref="Queue" />.
			/// </returns>
			public Queue Queue {
				get { return queue; }
			}

			// Delegate Functions
			// Delegate Functions :: ThreadFunc
			//	TODO: Refactor
			/// <summary>
			///	Downloads the cover and sets it in the database.
			/// </summary>
			/// <remarks>
			///	<para>
			///	This is the main method of the thread.
			///	</para>
			///
			///	<para>
			///	If the cover has trouble downloading, this thread
			///	sleeps for a minute. In order to not overload the
			///	server, this thread sleeps for a second between
			///	downloads. The Amazon API requires that we do this.
			///	</para>
			/// </remarks>
			private void ThreadFunc ()
			{
				while (true) {
					while (queue.Count > 0) {
						Album album = (Album) queue.Dequeue ();
						Pixbuf pixbuf = null;

						// Download Cover
						try {
							pixbuf = getter.DownloadFromAmazon (album);

						} catch (WebException) {
							// Temporary web problem (Timeout etc.) - re-queue
							Thread.Sleep (60000); // wait for a minute first
							queue.Enqueue (album);
							continue;

						} catch (Exception) {
						}

						string key = album.Key;
						
						// Check if cover has been modified while we were
						//   downloading
						if (Global.CoverDB.Covers [key] != null)
							continue;

						// Add border and set cover						
						if (pixbuf == null) {
							Global.CoverDB.UnmarkAsBeingChecked (key);

						} else {
							pixbuf = getter.AddBorder (pixbuf);
							Global.CoverDB.SetCover (key, pixbuf);
						}

						new IdleData (album, pixbuf);
					}

					Thread.Sleep (1000);
				}
			}						
		}
	}
}
