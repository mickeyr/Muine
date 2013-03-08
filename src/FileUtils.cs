/*
 * Copyright (C) 2004 Jorn Baayen <jorn.baayen@gmail.com>
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
using System.IO;
using System.Runtime.InteropServices;

using Gnome.Vfs;

using Mono.Unix;

namespace Muine
{
	public static class FileUtils
	{
		// Constants
		private const string playlist_filename = "playlist.m3u";
		private const string songsdb_filename  = "songs.db"    ;
		private const string coversdb_filename = "covers.db"   ;
		private const string plugin_dirname    = "plugins"     ;

		private readonly static DateTime date_time_1970 = 
			new DateTime (1970, 1, 1, 0, 0, 0, 0);

		// Strings
		//	TODO: Rename to string_error_config and string_error_temp.
		private static readonly string string_init_config_failed = 
			Catalog.GetString ("Failed to initialize the configuration folder: {0}");

		private static readonly string string_init_temp_failed =
			Catalog.GetString ("Failed to initialize the temporary files folder: {0}");
		
		private static readonly string string_exiting =
			Catalog.GetString ("Exiting...");


		// Variables
		private static string home_directory;
		private static string config_directory;
		private static string playlist_file;
		private static string songsdb_file;
		private static string coversdb_file;
		private static string user_plugin_directory;
		private static string temp_directory;

		// Methods
		// Methods :: Public
		// Methods :: Public :: Init
		//	TODO: 
		//	* Replace directly looking up the environment variable
		//	  with System.Environment.GetFolderPath (
		//		System.Environment.SpecialFolder.Personal).
		//	* Perhaps we just use ~/.muine rather than ~/.gnome2/muine?
		//	  Gnome.User.DirGet is the only reason we have this method
		//	  (otherwise it could just be a static constructor).
		//	* Do we have to initialize all paths here? Why not
		//	  just generate them as-we-go in the properties?
		/// <summary>
		///	Initializes all variables for use.
		/// </summary>
		/// <remarks>
		///	Gnome must be initialized before this is executed.
		/// </remarks>
		/// <exception cref="Exception">
		///	Thrown if configuration directory or temp directory 
		///	cannot be found.
		/// </exception>
		public static void Init ()
		{
			home_directory = Environment.GetEnvironmentVariable ("HOME");
			
			try {
				config_directory =
				  Path.Combine (Gnome.User.DirGet (), "muine");

				CreateDirectory (config_directory);

			} catch (Exception e) {
				string msg = string_init_config_failed;
				msg += Environment.NewLine;
				msg += Environment.NewLine;
				msg += string_exiting;
				throw new Exception (msg, e);
			}

			playlist_file =
			  Path.Combine (config_directory, playlist_filename);

			songsdb_file =
			  Path.Combine (config_directory, songsdb_filename );

			coversdb_file =
			  Path.Combine (config_directory, coversdb_filename);

			user_plugin_directory =
			  Path.Combine (config_directory, plugin_dirname);
			
			try {
				string path = System.IO.Path.GetTempPath ();
				string name = ("muine-" + Environment.UserName);
				temp_directory = Path.Combine (path, name);
				CreateDirectory (temp_directory);

			} catch (Exception e) {
				string msg = string_init_temp_failed;
				msg += Environment.NewLine;
				msg += Environment.NewLine;
				msg += string_exiting;
				throw new Exception (msg, e);
			}
		}

		// Properties
		// Properties :: HomeDirectory (get;)
		/// <summary>
		///	The user's home directory
		/// </summary>
		/// <returns>
		///	The absolute path to the user's home directory.
		/// </returns>
		public static string HomeDirectory {
			get { return home_directory; }
		}

		// Properties :: ConfigDirectory (get;)
		/// <summary>
		///	Muine's configuration directory
		/// </summary>
		/// <remarks>
		///	This should be ~/.gnome2/muine or similar.
		/// </remarks>
		/// <returns>
		///	The absolute path to Muine's configuration directory.
		/// </returns>
		public static string ConfigDirectory {
			get { return config_directory; }
		}
		
		// Properties :: PlaylistFile (get;)
		/// <summary>
		///	The path to the current playlist.
		/// </summary>
		/// <remarks>
		///	This should be ~/.gnome2/muine/playlist.m3u or similar.
		/// </remarks>
		/// <returns>
		///	The absolute path to the current playlist.
		/// </returns>
		public static string PlaylistFile {
			get { return playlist_file; }
		}

		// Properties :: SongsDBFile (get;)
		/// <summary>
		///	The path to the song database.
		/// </summary>
		/// <remarks>
		///	This should be ~/.gnome2/muine/songs.db or similar.
		/// </remarks>
		/// <returns>
		///	The absolute path to the song database.
		/// </returns>
		public static string SongsDBFile {
			get { return songsdb_file; }
		}

		// Properties :: CoversDBFile (get;)
		/// <summary>
		/// 	The path to the covers database.
		/// </summary>
		/// <remarks>
		///	This should be ~/.gnome2/muine/covers.db or similar.
		/// </remarks>
		/// <returns>
		///	The absolute path to the covers database.
		/// </returns>
		public static string CoversDBFile {
			get { return coversdb_file; }
		}

		// Properties :: SystemPluginDirectory (get;)
		/// <summary>
		///	Path to the system-wide plugins directory.
		/// </summary>
		/// <remarks>
		///	This should be /usr/lib/muine/plugins or similar.
		/// </remarks>
		/// <returns>
		///	The absolute path to the system-wide plugins directory.
		/// </returns>
		public static string SystemPluginDirectory {
			get { return Defines.PLUGIN_DIR; }
		}

		// Properties :: UserPluginDirectory (get;)
		/// <summary>
		///	Path to the user's personal plugins directory.
		/// </summary>
		/// <remarks>
		///	This should be ~/.gnome2/muine/plugins or similar.
		/// </remarks>
		/// <returns>
		///	The absolute path to the user's personal plugins directory.
		/// </returns>
		public static string UserPluginDirectory {
			get { return user_plugin_directory; }
		}

		// Properties :: TempDirectory (get;)
		/// <summary>
		///	Path to Muine's temporary directory.
		/// </summary>
		/// <remarks>
		///	This should be /tmp/muine-tamara or similar.
		/// </remarks>
		/// <returns>
		///	The absolute path to Muine's temporary directory.
		/// </returns>
		public static string TempDirectory {
			get { return temp_directory; }
		}
		
		// Methods
		// Methods :: Public
		// Methods :: Public :: IsFromRemovableMedia
		//	TODO: There's gotta be a better way...
		//	perhaps we can do something with HAL.
		/// <summary>
		///	Checks to see whether a file is on removable media.
		/// </summary>
		/// <remarks>
		///	This works by checking to see if the path starts with
		///	/mnt or /media (either proceeded by file:// or not).
		/// </remarks>
		/// <param name="fn">
		///	The absolute path to check.
		/// </param>
		/// <returns>
		///	True if <paramref name="fn" /> is on removable media,
		///	otherwise False.
		/// </returns>
		public static bool IsFromRemovableMedia (string fn)
		{
			return (fn.StartsWith ("/mnt/") ||
				fn.StartsWith ("file:///mnt/") ||
				fn.StartsWith ("/media/") ||
				fn.StartsWith ("file:///media/"));
		}

		// Methods :: Public :: IsPlaylist
		/// <summary>
		///	Checks to see if a file is a playlist.
		/// </summary>
		/// <remarks>
		///	This checks to see if the filename ends in
		///	.m3u (common for MP3 playlists).
		/// </remarks>
		/// <param name="fn">
		///	The filename to check.
		/// </param>
		/// <returns>
		///	True if the file is a playlist, False otherwise.
		/// </returns>
		public static bool IsPlaylist (string fn)
		{
			string ext = Path.GetExtension (fn).ToLower ();
			return (ext == ".m3u");
		}

		// Methods :: Public :: Exists
		/// <summary>
		///	Checks to see if the URI exists.
		/// </summary>
		/// <param name="fn">
		///	The URI to check.
		/// </param>
		/// <returns>
		///	True if the URI exists, False otherwise.
		/// </returns>
		public static bool Exists (string fn)
		{
			Gnome.Vfs.Uri u = new Gnome.Vfs.Uri (fn);
			return u.Exists;
		}

		// Methods :: Public :: MakeHumanReadable
		/// <summary>
		///	Simplify a filename for presentation.
		/// </summary>
		/// <remarks>
		///	The filename is parsed by <see cref="System.Uri.ToString" />.
		///	If it is a local file, then only the filename is returned,
		///	Otherwise, the whole URI is returned.
		/// </remarks>
		/// <param name="fn">
		///	The URI to parse.
		/// </param>
		/// <returns>
		///	The filename simplified for presentation.
		/// </returns>
		public static string MakeHumanReadable (string fn)
		{
			System.Uri u = new System.Uri (fn);
			string ret = u.ToString ();

			if (ret.StartsWith ("file://"))
				ret = ret.Substring ("file://".Length);

			return Path.GetFileName (ret);
		}

		// Methods :: Public :: MTimeToTicks
		//	TODO:
		//	* Change the literal 10,000,000 to 
		//	  System.TimeSpan.TicksPerSecond
		//	* Store the MTimes internally as System.DateTime 
		//	  structures rather than as ticks (this requires moving
		//	  some code over from libmuine). That would make this 
		//	  method obsolete.
		/// <summary>
		///	Convert POSIX-style MTime to ticks.
		/// </summary>
		/// <remarks>
		///	A tick is one ten millionth (1/10^7) of a second.
		/// </remarks>
		/// <param name="mtime">
		///	Seconds since the epoch (1970-01-01).
		/// </param>
		/// <returns>
		///	<paramref name="mtime" /> as ticks since 01-01-01.
		/// </returns>
		public static long MTimeToTicks (int mtime)
		{
			return (long) (mtime * 10000000L) + date_time_1970.Ticks;
		}

		// Methods :: Public :: CreateDirectory
		/// <summary>
		///	Creates a directory.
		/// </summary>
		/// <remarks>
		///	If the directory already exists, nothing happens.
		/// </remarks>
		private static void CreateDirectory (string dir)
		{
			DirectoryInfo dinfo = new DirectoryInfo (dir);
			if (dinfo.Exists)
				return;
					
			dinfo.Create ();
		}

		// Methods :: Public ::: IsRemote
		// 	TODO: 
		//	* Make portable
		//	* Can this be replaced or simplified with
		//	  !System.Uri.IsFile?
		/// <summary>
		///	Checks to see if a URI is remote or not.
		/// </summary>
		/// <remarks>
		///	A URI is considered remote if it does not begin
		///	with '/' or "file://".
		/// </remarks>
		/// <returns>
		///	True if the URI is remote, False otherwise.
		/// </returns>
		public static bool IsRemote (string uri)
		{
			bool is_rooted = (uri [0] == '/');
			bool is_file_uri = uri.StartsWith ("file://");
			
			return (!is_rooted && !is_file_uri);
		}
	}
}
