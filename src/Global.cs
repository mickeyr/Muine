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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Gtk;
using GLib;
using Gdk;

using Mono.Unix;

namespace Muine
{
	public sealed class Global : Gnome.Program
	{
		// Strings
		private static readonly string string_dbus_failed =
			Catalog.GetString ("Failed to export D-Bus object: {0}");		

		private static readonly string string_coverdb_failed =
			Catalog.GetString ("Failed to load the cover database: {0}");

		private static readonly string string_songdb_failed =
			Catalog.GetString ("Failed to load the song database: {0}");

		private static readonly string string_error_initializing =
			Catalog.GetString ("Error initializing Muine.");
	
		// Variables
		private static SongDatabase   db;
		private static CoverDatabase  cover_db;
		private static PlaylistWindow playlist;
		private static Actions        actions;

		private static DBusLib.IPlayer dbus_object = null;
		private static Gnome.Client   session_client;
		
		// Properties
		// Properties :: DB (get;)
		/// <summary>
		///	The <see cref="SongDatabase" />.
		/// </summary>
		/// <returns>
		///	A <see cref="SongDatabase" /> object.
		/// </returns>
		public static SongDatabase DB {
			get { return db; }
		}

		// Properties :: CoverDB (get;)
		/// <summary>
		///	The <see cref="CoverDatabase" />.
		/// </summary>
		/// <returns>
		///	A <see cref="CoverDatabase" /> object.
		/// </returns>
		public static CoverDatabase CoverDB {
			get { return cover_db; }
		}	

		// Properties :: Playlist (get;)
		/// <summary>
		///	The <see cref="PlaylistWindow" />.
		/// </summary>
		/// <returns>
		///	A <see cref="PlaylistWindow" /> object.
		/// </returns>
		public static PlaylistWindow Playlist {
			get { return playlist; }
		}

		// Properties :: Actions (get;)
		/// <summary>
		///	The <see cref="Actions" />.
		/// </summary>
		/// <returns>
		///	A <see cref="Actions" /> object.
		/// </returns>
		public static Actions Actions {
			get { return actions; }
		}

		// Main
		/// <summary>
		///	The main method.
		/// </summary>
		/// <param name="args">
		///	An array of <see cref="String">strings</see>, 
		///	representing command-line arguments.
		/// </param>
		public static void Main (string [] args)
		{
			try {
				NDesk.DBus.BusG.Init ();
			} catch {}

			try {
				Global.SetProcessName ("muine");
			} catch {}

			Application.Init ();
			Gnome.Vfs.Vfs.Initialize ();

			// Try to find a running Muine
			try {
				dbus_object = DBusLib.Player.FindInstance ();
			} catch {
			}

			// Check if an instance of Muine is already running
			if (dbus_object != null) {

				// Handle command line args and exit.
				if (args.Length > 0)
					ProcessCommandLine (args);
				else
					dbus_object.SetWindowVisible (true, 0);
				
				Gdk.Global.NotifyStartupComplete ();
				return;
			}

			Catalog.Init ("muine", Defines.GNOME_LOCALE_DIR);

			new Gnome.Program
			  ("muine", Defines.VERSION, Gnome.Modules.UI, args);

			// Initialize D-Bus
			//   We initialize here but don't connect to it until later.
			try {
				dbus_object = new DBusLib.Player ();

				DBusService.Instance.RegisterObject (dbus_object, 
					"/org/gnome/Muine/Player");

			} catch (Exception e) {
				Console.WriteLine (string_dbus_failed, e.Message);
			}

			// Init GConf
			Config.Init ();

			// Init files
			try {
				FileUtils.Init ();

			} catch (Exception e) {
				Error (e.Message);
			}

			// Register stock icons
			StockIcons.Initialize ();

			// Set default window icon
			SetDefaultWindowIcon ();

			// Open cover database
			try {
				cover_db = new CoverDatabase (3);

			} catch (Exception e) {
				Error (String.Format (string_coverdb_failed, e.Message));
			}

			cover_db.DoneLoading += OnCoversDoneLoading;

			// Load song database
			try {
				db = new SongDatabase (6);

			} catch (Exception e) {
				Error (String.Format (string_songdb_failed, e.Message));
			}

			db.Load ();

			// Setup Actions
			actions = new Actions ();

			// Create playlist window
			try {
				playlist = new PlaylistWindow ();
			} catch (PlayerException e) {
				Error (e.Message);
			}

			// D-Bus
			// 	Hook up D-Bus object before loading any songs into the
			//	playlist, to make sure that the song change gets emitted
			//	to the bus 
			Muine.DBusLib.Player exported_dbus_object = dbus_object as Muine.DBusLib.Player;
			if (exported_dbus_object != null) {
				exported_dbus_object.HookUp (playlist);
			}
		
			// PluginManager
			//	Initialize plug-ins (also before loading any songs, to make
			//	sure that the song change gets through to all the plug-ins)
			new PluginManager (playlist);

			// Hook up multimedia keys
			if (!GnomeMMKeys.Initialize ()) {
				new MmKeys (playlist);
			}

			// Process command line options
			bool opened_file = ProcessCommandLine (args);

			// Load playlist
			if (!opened_file)
				playlist.RestorePlaylist ();

			// Show UI
			playlist.Run ();

			while (MainContext.Pending ())
				Gtk.Main.Iteration ();

			// Load Covers
			cover_db.Load ();

			// Hook up to the session manager
			session_client = Gnome.Global.MasterClient ();
			session_client.Die          += OnDieEvent;
			session_client.SaveYourself += OnSaveYourselfEvent;

			// Run!
			Application.Run ();
		}

		// Methods
		// Methods :: Public :: Exit
		/// <summary>
		///	Exit the program.
		/// </summary>
		public static void Exit ()
		{
			if (GnomeMMKeys.IsLoaded) {
				GnomeMMKeys.Shutdown ();
			}
			Environment.Exit (0);
		}

		// Methods :: Private
		// Methods :: Private :: ProcessCommandLine 
		/// <summary>
		///	Process command-line arguments.
		/// </summary>
		/// <remarks>
		///	Files listed on the command-line may be playlists or
		///	music files. They are all added, and the first song 
		///	begins playing.
		/// </remarks>
		/// <param name="args">
		///	An array of <see cref="String">strings</see>,
		///	representing command-line arguments.
		/// </param>
		/// <returns>
		///	True, if a file was added to the playlist, 
		///	False otherwise.
		/// </returns>
		private static bool ProcessCommandLine (string [] args)
		{
			bool opened_file = false;

			foreach (string arg in args) {
				System.IO.FileInfo finfo = new System.IO.FileInfo (arg);
				
				if (!finfo.Exists)
					continue;

				opened_file = true;

				// See the file is a Playlist
				if (FileUtils.IsPlaylist (arg)) { // load as playlist
					dbus_object.OpenPlaylist (finfo.FullName);
					continue;
				}
				
				// Must be a music file
				
				// If it's the first song, start playing it.
				if (arg == args [0])
					dbus_object.PlayFile (finfo.FullName);
				else // Else, queue.
					dbus_object.QueueFile (finfo.FullName);
			}

			return opened_file;
		}

		// Methods :: Private :: SetDefaultWindowIcon
		/// <summary>
		///	Set the default window icon.
		/// </summary>
		/// <remarks>
		///	The default window icon is stored as a resource in the
		///	assembly under the name "muine.png".
		/// </remarks>
		private static void SetDefaultWindowIcon ()
		{
			Pixbuf [] default_icon_list = { new Pixbuf (null, "muine.png") };
			Gtk.Window.DefaultIconList = default_icon_list;
		}

		// Methods :: Private :: Error
		/// <summary>
		///	Display a fatal error dialog with message.
		/// </summary>
		/// <remarks>
		///	Exits immediately with an error status of 1.
		/// </remarks>
		/// <param name="message">
		///	Message to be used as the secondary text.
		/// </param>
		private static void Error (string message)
		{
			new ErrorDialog (string_error_initializing, message);

			Environment.Exit (1);
		}

		// Handlers
		// Handlers :: OnCoversDoneLoading
		/// <summary>
		///	Handler called when covers are done downloading.
		/// </summary>
		/// <remarks>
		///	Now that the covers are done, we begin looking for changes.
		/// </remarks>
		private static void OnCoversDoneLoading ()
		{
			db.CheckChanges ();
		}

		// Handlers :: OnDieEvent
		/// <summary>
		///	Handler called on <see cref="Gnome.Client.Die" /> event.
		/// </summary>
		/// <remarks>
		///	Calls <see cref="Exit" />.
		/// </remarks>
		/// <param name="o">
		/// 	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private static void OnDieEvent (object o, EventArgs args)
		{
			Exit ();
		}

		// Handlers :: OnSaveYourselfEvent
		//	TODO: Actually set the restart command to something useful.
		/// <summary>
		///   Handler called on <see cref="Gnome.Client.SaveYourself" /> event.
		/// </summary>
		/// <param name="o">
		///   The calling object.
		/// </param>
		/// <param name="args">
		///   The <see cref="Gnome.SaveYourselfArgs" />.
		/// </param>		
		/// <remarks>
		///   This doesn't do anything useful yet.
		/// </remarks>
		private static void OnSaveYourselfEvent
		  (object o, Gnome.SaveYourselfArgs args)
		{
			string [] argv = { "muine" };

			session_client.SetRestartCommand (1, argv);
		}

		[DllImport ("libc")]
		private static extern int prctl (int option,  
						 byte[] arg2,
						 ulong arg3,
						 ulong arg4,
						 ulong arg5);
		public static void SetProcessName (string name)
		{
			if (prctl (15, Encoding.ASCII.GetBytes (name + "\0"), 0, 0, 0) != 0) {
				throw new ApplicationException ("Error setting process name: " + Mono.Unix.Native.Stdlib.GetLastError ());
			}
		}
	}
}
