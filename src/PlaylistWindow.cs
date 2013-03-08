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

using Gtk;
using GLib;
using Gnome.Vfs;
using Bacon;

using Mono.Unix;

using Muine.PluginLib;

namespace Muine
{
	public class PlaylistWindow : Window, IPlayer
	{
		// Constants
		// Constants :: Step
		//	Number of seconds to skip back and forth
		private const int Step = 5;
		
		// Constants :: MinRestartTime
		//	Seconds after which song will restart on "previous"
		private const int MinRestartTime = 3; 

		// Constants :: MinShowHours
		//	Seconds over which to show remaining time in hours
		private const int MinShowHours = 6000; 

		// Constnats :: MinShowMinutes
		//	Seconds over which to show remaining time in minutes
		private const int MinShowMinutes = 60;
		
		// GConf
		// GConf :: Width
		private const string GConfKeyWidth = "/apps/muine/playlist_window/width";
		private const int    GConfDefaultWidth = -1; 

		// GConf :: Height
		private const string GConfKeyHeight = "/apps/muine/playlist_window/height";
		private const int    GConfDefaultHeight = 450;
		
		private const string GConfKeyPosX = "/apps/muine/playlist_window/pos_x";
		private const int    GConfDefaultPosX = 0; 

		private const string GConfKeyPosY = "/apps/muine/playlist_window/pos_y";
		private const int    GConfDefaultPosY = 0;

		// GConf :: Volume
		private const string GConfKeyVolume = "/apps/muine/volume";
		private const int    GConfDefaultVolume = 50;

		// GConf :: Repeat
		private const string GConfKeyRepeat = "/apps/muine/repeat";
		private const bool   GConfDefaultRepeat = false;

		// Strings
		private static readonly string string_program = 
			Catalog.GetString ("Muine Music Player");

		private static readonly string string_playlist_filename =
			Catalog.GetString ("Playlist.m3u");

		private static readonly string string_playlist = 
			Catalog.GetString ("<b>Playlist</b>");

		private static readonly string string_playlist_repeating =
			Catalog.GetString ("<b>Playlist</b> (Repeating)");

		private static readonly string string_playlist_under_minute =
			Catalog.GetString ("<b>Playlist</b> (Less than one minute remaining)");

		private static readonly string string_from_album =
			Catalog.GetString ("From \"{0}\"");

		private static readonly string string_album_unknown =
			Catalog.GetString ("Album unknown");

		private static readonly string string_performers =
			Catalog.GetString ("Performed by {0}");

		// Strings :: Window Titles
		private static readonly string string_title_main =
			Catalog.GetString ("{0} - Muine Music Player");

		// Strings :: Tooltips
		private static readonly string string_tooltip_toggle_play =
			Catalog.GetString ("Switch music playback on or off");

		private static readonly string string_tooltip_previous =
			Catalog.GetString ("Play the previous song");

		private static readonly string string_tooltip_next =
			Catalog.GetString ("Play the next song");

		private static readonly string string_tooltip_add_album =
			Catalog.GetString ("Add an album to the playlist");

		private static readonly string string_tooltip_add_song =
			Catalog.GetString ("Add a song to the playlist");

		private static readonly string string_tooltip_volume =
			Catalog.GetString ("Change the volume level");

		private static readonly string string_tooltip_cover =
			Catalog.GetString ("Drop an image here to use it as album cover");

		// Strings :: Errors
		private static readonly string string_error_audio =
			Catalog.GetString ("Failed to initialize the audio backend");

		private static readonly string string_error_read =
			Catalog.GetString ("Failed to read {0}:");

		private static readonly string string_error_close =
			Catalog.GetString ("Failed to close {0}:");

		private static readonly string string_error_write =
			Catalog.GetString ("Failed to write {0}:");

		// Events
		// Events :: SongChangedEvent (IPlayer)
		public event SongChangedEventHandler SongChangedEvent;
		
		// Events :: StateChangedEvent (IPlayer)
		public event StateChangedEventHandler StateChangedEvent;

		// Events :: TickEvent (IPlayer)
		public event TickEventHandler TickEvent;
		
		// Events :: PlaylistChangedEvent (IPlayer)
		public event GenericEventHandler PlaylistChangedEvent;
		
		// Events :: SelectionChangedEvent (IPlayer)
		public event GenericEventHandler SelectionChangedEvent;

		// Events :: WatchedFoldersChangedEvent (IPlayer)
		public event GenericEventHandler WatchedFoldersChangedEvent;

		// Delegates
		private delegate void PlaylistForeachFunc
		  (Song song, bool playing, object user_data);

		// Widgets
		[Glade.Widget] private VBox           main_vbox     ;
		[Glade.Widget] private Box            menu_bar_box  ;
		[Glade.Widget] private ScrolledWindow scrolledwindow;

		private Gdk.Pixbuf                empty_pixbuf   ;
		private CellRendererText          text_renderer  ;
		private ColoredCellRendererPixbuf pixbuf_renderer;
		
		// Widgets :: Containers
		[Glade.Widget] private Container volume_button_container;
		[Glade.Widget] private Container cover_image_container  ;

		// Widgets :: Toolbar
		[Glade.Widget] private ToggleButton toggle_play_button;
		[Glade.Widget] private Button       previous_button   ;
		[Glade.Widget] private Button       next_button       ;
		[Glade.Widget] private Button       add_song_button   ;
		[Glade.Widget] private Button       add_album_button  ;		

		private Bacon.VolumeButton volume_button;

		// Widgets :: Player
		[Glade.Widget] private Label song_label;
		[Glade.Widget] private Label time_label;

		private CoverImage       cover_image ;

		// Widgets :: Playlist
		[Glade.Widget] private Label    playlist_label          ;
		[Glade.Widget] private EventBox playlist_label_event_box;

		private HandleView playlist;

		// Windows
		private SkipToWindow   skip_to_window   = null;
		private AddSongWindow  add_song_window  = null;
		private AddAlbumWindow add_album_window = null;

		// Objects
		// Objects :: Player
		private Player player;
		private bool had_last_eos;
		private bool ignore_song_change;

		// Drag-and-Drop
		private static TargetEntry [] drag_entries = {
			DndUtils.TargetUriList
		};

		private static TargetEntry [] playlist_source_entries = {
			DndUtils.TargetMuineTreeModelRow,
			DndUtils.TargetUriList
		};
			
		private static TargetEntry [] playlist_dest_entries = {
			DndUtils.TargetMuineTreeModelRow,
			DndUtils.TargetMuineSongList,
			DndUtils.TargetMuineAlbumList,
			DndUtils.TargetUriList
		};

		// Variables
		private uint busy_level = 0;

		private int last_x = -1;
		private int last_y = -1;
		private bool window_visible;

		private long remaining_songs_time;

		private Hashtable random_sort_keys;

		private bool repeat;

		// Constructor
		public PlaylistWindow () : base (WindowType.Toplevel)
		{
			// Build the interface
			Glade.XML glade_xml =
			  new Glade.XML (null, "PlaylistWindow.glade", "main_vbox", null);

			glade_xml.Autoconnect (this);	

			base.Add (main_vbox);

			// Hook up window signals
			base.WindowStateEvent += OnWindowStateEvent;
			base.DeleteEvent      += OnDeleteEvent;
			base.DragDataReceived += OnDragDataReceived;

			Gtk.Drag.DestSet
			  (this, DestDefaults.All, drag_entries, Gdk.DragAction.Copy);

			// Keep track of window visibility
			base.VisibilityNotifyEvent += OnVisibilityNotifyEvent;
			AddEvents ((int) Gdk.EventMask.VisibilityNotifyMask);

			// Set up various other UI bits
			// Player has to be first, others need the Player object
			SetupPlayer (); 

			// Setup Menus
			base.AddAccelGroup (Global.Actions.UIManager.AccelGroup);
			menu_bar_box.Add (Global.Actions.MenuBar);
			
			SetupButtons ();
			SetupPlaylist ();

			// Connect to song database signals
			Global.DB.SongChanged           += OnSongChanged;
			Global.DB.SongRemoved           += OnSongRemoved;
			Global.DB.WatchedFoldersChanged += OnWatchedFoldersChanged;

			// Make sure the interface is up to date
			SelectionChanged ();
			StateChanged (false, true);
		}

		// Properties
		// 	Useful for Plug-Ins and DBus
		// Properties :: PlayingSong (get;) (IPlayer)
		public ISong PlayingSong {
			get {
				if (playlist.Model.Playing == IntPtr.Zero)
					return null;

				return Song.FromHandle (playlist.Model.Playing);
			}
		}

		// Properties :: Playing (set; get;) (IPlayer)
		public bool Playing {
			set {
				if (!playlist.Model.HasFirst)
					return;

				if (value) {
					if (had_last_eos) {
						PlayFirstAndSelect ();
						PlaylistChanged ();
					}

					player.Play ();
					
					return;
				}

				player.Pause ();
			}

			get { return player.Playing; }
		}

		// Properties :: Volume (set; get;) (IPlayer)
		public int Volume {
			set {
				if (value > 100 || value < 0)
					value = GConfDefaultVolume;

				player.Volume = value;
				volume_button.Volume = value;

				Config.Set (GConfKeyVolume, value);
			}
			
			get { return player.Volume; }
		}

		// Properties :: Position (set; get;) (IPlayer)
		public int Position {
			set { SeekTo (value); }
			get { return player.Position; }
		}

		// Properties :: HasNext (get;) (IPlayer)
		public bool HasNext {
			get { return playlist.Model.HasNext; }
		}

		// Properties :: HasPrevious (get;) (IPlayer)
		public bool HasPrevious {
			get { return playlist.Model.HasPrevious; }
		}

		// Properties :: Playlist (get;) (IPlayer)
		public ISong [] Playlist {
			get { return ArrayFromList (playlist.Model.Contents); }
		}

		// Properties :: Selection (get;) (IPlayer)
		public ISong [] Selection {
			get { return ArrayFromList (playlist.SelectedHandles); }
		}

		// Properties :: AllSongs (get;) (IPlayer)
		public ISong [] AllSongs {
			get {
				lock (Global.DB) {
					ISong [] array = new ISong [Global.DB.Songs.Count];
					// We copy, to avoid bothering plugins with locking
					// issues.
					Global.DB.Songs.Values.CopyTo (array, 0);
					return array;
				}
			}
		}

		// Properties :: UIManager (get;) (IPlayer)
		public UIManager UIManager {
			get { return Global.Actions.UIManager; }
		}

		// Properties :: Window (get;) (IPlayer)
		public Window Window {
			get { return this; }
		}

		// Properties :: BusyLevel (set; get;) (IPlayer)
		public uint BusyLevel {
			set {
				if (busy_level == 0 && value > 0) {
					base.Realize ();

					base.GdkWindow.Cursor =
					  new Gdk.Cursor (Gdk.CursorType.Watch);

					base.GdkWindow.Display.Flush ();

				} else if (busy_level > 0 && value == 0) {
					base.GdkWindow.Cursor = null;
				}

				busy_level = value;
			}

			get { return busy_level; }
		}

		// Properties :: WatchedFolders (set; get;) (IPlayer)
		public string [] WatchedFolders {
			set { Global.DB.WatchedFolders = value; }
			get { return Global.DB.WatchedFolders ; }
		}

		// Properties :: WindowVisible (get;) (IPlayer)
		public bool WindowVisible {
			get { return window_visible; }
		}

		// Properties :: Repeat (set; get;)
		public bool Repeat {
			set {
				repeat = value;

				ToggleAction act =
				  (ToggleAction) Global.Actions ["ToggleRepeat"];

				act.Active = value;

				Config.Set (GConfKeyRepeat, value);			

				PlaylistChanged ();
			}

			get { return repeat; }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: RestorePlaylist
		public void RestorePlaylist ()
		{
			// Load last playlist
			if (!System.IO.File.Exists (FileUtils.PlaylistFile))
				return;

			OpenPlaylistInternal (FileUtils.PlaylistFile,
				new PlaylistForeachFunc (RestorePlaylistForeachFunc), null);

			EnsurePlaying ();
		}

		// Methods :: Public :: Run
		public void Run ()
		{
			if (!playlist.Model.HasFirst)
				SongChanged (true); // make sure the UI is up to date

			RestoreState ();
			SetWindowVisible (true, 0);
		}

		// Methods :: Public :: Quit (IPlayer)
		public void Quit ()
		{
			SaveWindowPosition ();
			Global.Exit ();
		}

		// Methods :: Public :: PackWidget (IPlayer)
		public void PackWidget (Widget widget)
		{
			main_vbox.PackEnd (widget);
		}

		// Methods :: Public :: UpdateWindowVisibilityUI
		public void UpdateWindowVisibilityUI ()
		{
			// Select playing song if the window is visible
			bool has_playing = (playlist.Model.Playing != IntPtr.Zero);
			if (WindowVisible && has_playing)
				playlist.Select (playlist.Model.Playing);
			
			// Toggle the visible action
			ToggleAction act =
			  (ToggleAction) Global.Actions ["ToggleVisible"];

			act.Active = Visible;
		}

		// Methods :: Public :: PlayFile (IPlayer)
		public void PlayFile (string file)
		{
			Song song = GetSingleSong (file, false);

			if (song == null)
				return;

			IntPtr p = AddSong (song);
			PlayAndSelect (p);
			PlaylistChanged ();
			player.Play ();
		}

		// Methods :: Public :: QueueFile (IPlayer)
		public void QueueFile (string file)
		{
			Song song = GetSingleSong (file, false);

			if (song == null)
				return;

			AddSong (song);
			EnsurePlaying ();
			PlaylistChanged ();
		}

		// Methods :: Public :: OpenPlaylist (IPlayer)
		public void OpenPlaylist (string fn)
		{
			BusyLevel ++;

			ClearPlaylist ();
			OpenPlaylistInternal (fn,
				new PlaylistForeachFunc (RegularPlaylistForeachFunc), null);

			EnsurePlaying ();
			PlaylistChanged ();
			Playing = true;

			BusyLevel --;
		}

		// Methods :: Public :: Previous (IPlayer)
		public void Previous ()
		{
			if (!playlist.Model.HasFirst)
				return;

			if (player.Position < MinRestartTime) {
				if (playlist.Model.HasPrevious) {
					playlist.Model.Previous ();
					PlaylistChanged ();

				} else if (repeat) {
					playlist.Model.Last ();
					PlaylistChanged ();

				} else {
					player.Position = 0;
				}

			} else {
				player.Position = 0;
			}

			playlist.Select (playlist.Model.Playing);
			player.Play ();
		}

		// Methods :: Public :: Next (IPlayer)
		public void Next ()
		{
			if (playlist.Model.HasNext)
				playlist.Model.Next ();

			else if (repeat && playlist.Model.HasFirst)
				playlist.Model.First ();

			else
				return;

			playlist.Select (playlist.Model.Playing);
			PlaylistChanged ();
			player.Play ();
		}

		// Methods :: Public :: PlaySong (IPlayer)
		public void PlaySong (uint time)
		{
			if (add_song_window == null) {
				add_song_window = new AddSongWindow ();

				add_song_window.QueueEvent += OnQueueSongsEvent;
				add_song_window.PlayEvent  += OnPlaySongsEvent ;
			}

			AddChildWindowIfVisible (add_song_window);
			add_song_window.Run (time);
		}

		// Methods :: Public :: PlayAlbum (IPlayer)
		public void PlayAlbum (uint time)
		{
			if (add_album_window == null) {
				add_album_window = new AddAlbumWindow ();
				
				add_album_window.QueueEvent += OnQueueAlbumsEvent;
				add_album_window.PlayEvent  += OnPlayAlbumsEvent ;
			}

			AddChildWindowIfVisible (add_album_window);
			add_album_window.Run (time);
		}

		// Methods :: Public :: SetWindowVisible (IPlayer)
		public void SetWindowVisible (bool visible, uint time)
		{
			window_visible = visible;

			if (window_visible) {
				if (!Visible && last_x >= 0 && last_y >= 0)
					Move (last_x, last_y);

				Show ();

				GdkWindow.Focus (time);

			} else {
				GetPosition (out last_x, out last_y);
				Visible = false;
			}

			UpdateWindowVisibilityUI ();
		}

		// Methods :: Public :: AddSong (IPlayer)
		public ISong AddSong (string path)
		{
			return GetSingleSong (path, true);
		}

		// Methods :: Public :: SyncSong (IPlayer)
		public void SyncSong (string path)
		{
			lock (Global.DB) {
				Song song = (Song) Global.DB.Songs [path];

				if (song != null)
					Global.DB.SyncSong (song);
			}
		}

		// Methods :: Public :: SyncSong (IPlayer)
		public void SyncSong (ISong song)
		{
			Global.DB.SyncSong ((Song) song);
		}

		// Methods :: Public :: RemoveSong (IPlayer)
		public void RemoveSong (string path)
		{
			lock (Global.DB) {
				Song song = (Song) Global.DB.Songs [path];

				if (song != null)
					Global.DB.RemoveSong (song);
			}
		}

		// Methods :: Public :: RemoveSong (IPlayer)
		public void RemoveSong (ISong song)
		{
			Global.DB.RemoveSong ((Song) song);
		}

		// Methods :: Public :: AddFolder (IPlayer)
		public void AddFolder (string folder)
		{
			Global.DB.AddFolder (folder);
		}

		// Methods :: Public :: RemoveFolder (IPlayer)
		public void RemoveFolder (string folder)
		{
			Global.DB.RemoveFolder (folder);
		}

		// Methods :: Public :: AddChildWindowIfVisible
		public void AddChildWindowIfVisible (Window window)
		{
			window.TransientFor = (WindowVisible) ? this : null;
		}

		// Methods :: Public :: RunSkipToDialog
		public void RunSkipToDialog ()
		{
			playlist.Select (playlist.Model.Playing);

			if (skip_to_window == null)
				skip_to_window = new SkipToWindow (this);

			skip_to_window.Run ();
		}

		// Methods :: Public :: SkipBackwards		
		public void SkipBackwards ()
		{				
			SeekTo (player.Position - Step);
		}

		// Methods :: Public :: SkipForward
		public void SkipForward ()
		{
			SeekTo (player.Position + Step);
		}

		// Methods :: Public :: RemoveSelected
		public void RemoveSelected ()
		{
			List selected_pointers = playlist.SelectedHandles;

			int counter = 0;
			int selected_pointers_count = selected_pointers.Count;
			bool song_changed = false;

			// HACK: To improve performance, only load new song once
			ignore_song_change = true; 

			foreach (int i in selected_pointers) {
				IntPtr sel = new IntPtr (i);

				if (sel == playlist.Model.Playing) {
					OnPlayingSongRemoved ();		
					song_changed = true;
				}
				
				if (counter == selected_pointers_count - 1) {
					bool go_next = playlist.SelectNext ();
					if (!go_next)
						playlist.SelectPrevious ();
				}

				RemoveSong (sel);

				counter ++;
			}

			ignore_song_change = false;

			if (song_changed)
				SongChanged (true);

			PlaylistChanged ();
		}
		
		// Methods :: Public :: RemovePlayed
		public void RemovePlayed ()
		{
			if (playlist.Model.Playing == IntPtr.Zero)
				return;

			if (had_last_eos) {
				Clear ();
				return;
			}

			foreach (int i in playlist.Model.Contents) {
				IntPtr current = new IntPtr (i);

				if (current == playlist.Model.Playing)
					break;

				RemoveSong (current);
			}

			playlist.Select (playlist.Model.Playing);
			PlaylistChanged ();
		}
		
		// Methods :: Public :: Clear
		public void Clear ()
		{
			ClearPlaylist ();
			PlaylistChanged ();
			SongChanged (false);
		}
		
		// Methods :: Public :: Shuffle
		public void Shuffle ()
		{
			Random rand = new Random ();

			random_sort_keys = new Hashtable ();

			foreach (int i in playlist.Model.Contents) {
				int playing_i = (int) playlist.Model.Playing;
				double val = (i == playing_i) ? -1.0 : rand.NextDouble ();
				random_sort_keys.Add (i, val);
			}

			playlist.Model.Sort (new HandleModel.CompareFunc (ShuffleFunc));

			random_sort_keys = null;

			PlaylistChanged ();

			if (playlist.Model.Playing == IntPtr.Zero)
				return;

			playlist.Select (playlist.Model.Playing);
		}
		
		// Methods :: Public :: SavePlaylist
		public void SavePlaylist
		  (string fn, bool exclude_played, bool store_playing)
		{
			bool remote = FileUtils.IsRemote (fn);

			// Increase BusyLevel
			if (remote)
				BusyLevel ++;

			// Open
			VfsStream stream;
			StreamWriter writer;
			
			try {
				stream = new VfsStream (fn, System.IO.FileMode.Create);
				writer = new StreamWriter (stream);

			} catch (Exception e) {
				string fn_readable = FileUtils.MakeHumanReadable (fn);
				string msg = String.Format (string_error_write, fn_readable);
				new ErrorDialog (this, msg, e.Message);

				if (remote)
					BusyLevel --;

				return;
			}

			// Write
			if (!(exclude_played && had_last_eos)) {
				bool had_playing_song = false;
				foreach (int i in playlist.Model.Contents) {
					IntPtr ptr = new IntPtr (i);

					if (exclude_played) {
						if (ptr == playlist.Model.Playing)
							had_playing_song = true;

						else if (!had_playing_song)
							continue;
					}
				
					if (store_playing && ptr == playlist.Model.Playing)
						writer.WriteLine ("# PLAYING");

					Song song = Song.FromHandle (ptr);

					writer.WriteLine (song.Filename);
				}
			}

			// Close
			try {
				writer.Close ();

			} catch (Exception e) {
				string msg = String.Format (string_error_close, 
					FileUtils.MakeHumanReadable (fn));

				new ErrorDialog (this, msg, e.Message);
			}

			// Decrease BusyLevel
			if (remote)
				BusyLevel --;
		}

		// Methods :: Private
		// Methods :: Private :: SetupButtons
		private void SetupButtons ()
		{
			// Callbacks
			toggle_play_button.Clicked += OnTogglePlayButtonClicked;
			previous_button   .Clicked += OnPreviousButtonClicked;
			next_button       .Clicked += OnNextButtonClicked;
			add_song_button   .Clicked += OnAddSongButtonClicked;
			add_album_button  .Clicked += OnAddAlbumButtonClicked;

			// Volume
			volume_button = new Bacon.VolumeButton ();
			volume_button_container.Add (volume_button);
			volume_button.Visible = true;
			volume_button.VolumeChanged += OnVolumeChanged;

			// Tooltips
			next_button.TooltipText	       = string_tooltip_next;
			volume_button.TooltipText      = string_tooltip_volume;
			add_song_button.TooltipText    = string_tooltip_add_song;
			previous_button.TooltipText    = string_tooltip_previous;
			add_album_button.TooltipText   = string_tooltip_add_album;
			toggle_play_button.TooltipText = string_tooltip_toggle_play;
		}

		// Methods :: Private :: SetupPlaylist
		private void SetupPlaylist ()
		{
			playlist = new HandleView ();

			playlist.Selection.Mode = SelectionMode.Multiple;

			/* Stock Cell Renderer
			pixbuf_renderer = new Gtk.CellRendererPixbuf ();
                        pixbuf_renderer.FollowState = true;
			*/
			pixbuf_renderer = new ColoredCellRendererPixbuf ();

			text_renderer   = new Gtk.CellRendererText ();
			text_renderer.Ellipsize = Pango.EllipsizeMode.End;

			// Column			
			TreeViewColumn col = new TreeViewColumn ();
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.Spacing = 4;

			col.PackStart (pixbuf_renderer, false);
			col.PackStart (text_renderer  , true );
			
			TreeCellDataFunc func1 = new TreeCellDataFunc (PixbufCellDataFunc);
			TreeCellDataFunc func2 = new TreeCellDataFunc (TextCellDataFunc  );

			col.SetCellDataFunc (pixbuf_renderer, func1);
			col.SetCellDataFunc (text_renderer  , func2);

			playlist.AppendColumn (col);
			
			playlist.RowActivated         += OnPlaylistRowActivated;
			playlist.Selection.Changed    += OnPlaylistSelectionChanged;
			playlist.Model.PlayingChanged += OnPlaylistPlayingChanged;
			
			Gdk.DragAction act =
			  ( Gdk.DragAction.Copy
			  | Gdk.DragAction.Link
			  | Gdk.DragAction.Ask );

			playlist.EnableModelDragSource
			  (Gdk.ModifierType.Button1Mask, playlist_source_entries, act);

			playlist.EnableModelDragDest
			  (playlist_dest_entries, Gdk.DragAction.Copy);

			playlist.DragDataGet      += OnPlaylistDragDataGet;
			playlist.DragDataReceived += OnPlaylistDragDataReceived;

			playlist.Show ();

			scrolledwindow.Add (playlist);
			
			empty_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");
		}

		// Methods :: Private :: SetupPlayer
		private void SetupPlayer ()
		{
			// Create Player
			try {
				player = new Player ();

			} catch (Exception e) {
				throw new Exception (string_error_audio, e);
			}

			// Setup Player
			player.EndOfStreamEvent += OnEndOfStreamEvent;
			player.TickEvent        += OnTickEvent;
			player.StateChanged     += OnStateChanged;
			player.InvalidSong      += OnInvalidSong;

			// Cover Image
			cover_image = new CoverImage ();
			cover_image_container.Add (cover_image);
			cover_image.ShowAll ();

			// Playlist Label DnD
			playlist_label_event_box.DragDataGet +=
			  OnPlaylistLabelDragDataGet;
				
			Gtk.Drag.SourceSet
			  (playlist_label_event_box, Gdk.ModifierType.Button1Mask,
			   drag_entries, Gdk.DragAction.Move);

			// FIXME: Depends on Ximian Bugzilla #71060
			// string icon = Gnome.Icon.Lookup
			//   (IconTheme.GetForScreen (this.Screen), null, null, null,
			//    null, "audio/x-mpegurl", Gnome.IconLookupFlags.None, null);

			Gtk.Drag.SourceSetIconStock
			  (playlist_label_event_box, "gnome-mime-audio");
		}

		// Methods :: Private :: PlayAndSelect
		private void PlayAndSelect (IntPtr ptr)
		{
			playlist.Model.Playing = ptr;
			playlist.Select (playlist.Model.Playing);
		}

		// Methods :: Private :: PlayFirstAndSelect
		private void PlayFirstAndSelect ()
		{
			playlist.Model.First ();
			playlist.Select (playlist.Model.Playing);
		}

		// Methods :: Private :: EnsurePlaying
		private void EnsurePlaying ()
		{
			bool has_playing_song = (playlist.Model.Playing != IntPtr.Zero);
			if (has_playing_song || !playlist.Model.HasFirst)
				return;

			PlayFirstAndSelect ();
		}

		// Methods :: Private :: AddSong
		private IntPtr AddSong (Song song)
		{
			return AddSong (song.Handle);
		}

		private IntPtr AddSong (IntPtr p)
		{
			IntPtr ret =
			  AddSongAtPos (p, IntPtr.Zero, TreeViewDropPosition.Before);

			if (had_last_eos)
				PlayAndSelect (ret);

			return ret;
		}

		// Methods :: Private :: AddSongAtPos
		private IntPtr AddSongAtPos
		  (IntPtr p, IntPtr pos, TreeViewDropPosition dp)
		{
			IntPtr new_p = p;

			if (playlist.Model.Contains (p)) {
				Song song = Song.FromHandle (p);
				new_p = song.RegisterExtraHandle ();
			} 
		
			if (pos == IntPtr.Zero)
				playlist.Model.Append (new_p);
			else
				playlist.Model.Insert (new_p, pos, dp);

			return new_p;
		}

		// Methods :: Private :: RemoveSong
		private void RemoveSong (IntPtr p)
		{
			playlist.Model.Remove (p);

			Song song = Song.FromHandle (p);

			if (song.IsExtraHandle (p))
				song.UnregisterExtraHandle (p);
		}

		// Methods :: Private :: UpdateTimeLabels
		private void UpdateTimeLabels (int time)
		{
			if (playlist.Model.Playing == IntPtr.Zero) {
				time_label.Text = String.Empty; 
				playlist_label.Markup = string_playlist;
				return;
			}
			
			Song song = Song.FromHandle (playlist.Model.Playing);

			String pos   = StringUtils.SecondsToString (time         );
			String total = StringUtils.SecondsToString (song.Duration);

			time_label.Text = String.Format ("{0} / {1}", pos, total);

			// Calculate remaining time
			long r_seconds;

			if (this.repeat)
				r_seconds = remaining_songs_time;
			else
				r_seconds = remaining_songs_time + song.Duration - time;

			double r_seconds_d = (double) r_seconds;

			double hours_d = (r_seconds_d / 3600.0 + 0.5);
			int hours   = (int) Math.Floor (hours_d);

			double minutes_d = (r_seconds_d / 60.0 + 0.5);
			int minutes = (int) Math.Floor (minutes_d);

			// Possible strings
			string string_repeat_hour = Catalog.GetPluralString (
				"<b>Playlist</b> (Repeating {0} hour)", 
				"<b>Playlist</b> (Repeating {0} hours)", 
				hours);

			string string_repeat_minute = Catalog.GetPluralString (
				"<b>Playlist</b> (Repeating {0} minute)", 
				"<b>Playlist</b> (Repeating {0} minutes)", 
				minutes);

			string string_normal_hour = Catalog.GetPluralString (
				"<b>Playlist</b> ({0} hour remaining)", 
				"<b>Playlist</b> ({0} hours remaining)", 
				hours);
							 
			string string_normal_minute = Catalog.GetPluralString (
				"<b>Playlist</b> ({0} minute remaining)", 
				"<b>Playlist</b> ({0} minutes remaining)", 
				minutes);

			// Choose string for each scenario based on whether we are
			// repeating or not
			string string_hour;
			string string_minute;
			string string_second;
			
			if (repeat) {
				string_hour   = string_repeat_hour  ;
				string_minute = string_repeat_minute;
				string_second = string_playlist_repeating;
			} else {
				string_hour   = string_normal_hour  ;
				string_minute = string_normal_minute;
				string_second = string_playlist_under_minute;
			}
		
			// Set the label
			if (r_seconds > MinShowHours)
				playlist_label.Markup = String.Format (string_hour, hours);

			else if (r_seconds > MinShowMinutes)
				playlist_label.Markup = String.Format (string_minute, minutes);

			else if (r_seconds > 0)
				playlist_label.Markup = string_second;

			else
				playlist_label.Markup = string_playlist;
		}

		// Methods :: Private :: PlaylistChanged
		private void PlaylistChanged ()
		{
			bool start_counting = this.repeat;
			
			remaining_songs_time = 0;

			foreach (int i in playlist.Model.Contents) {
				IntPtr current = new IntPtr (i);

				if (start_counting) {
					Song song = Song.FromHandle (current);
					remaining_songs_time += song.Duration;
				}
					
				if (current != playlist.Model.Playing)
					continue;

				start_counting = true;
			}

			bool has_first = playlist.Model.HasFirst;

			previous_button   .Sensitive = has_first;
			toggle_play_button.Sensitive = has_first;

			bool do_loop = (this.repeat && has_first);
			next_button.Sensitive = (playlist.Model.HasNext || do_loop);

			Global.Actions ["TogglePlay"   ].Sensitive = previous_button   .Sensitive;
			Global.Actions ["Previous"     ].Sensitive = toggle_play_button.Sensitive;
			Global.Actions ["Next"         ].Sensitive = next_button       .Sensitive;
			
			Global.Actions ["SkipTo"       ].Sensitive = has_first;
			Global.Actions ["SkipBackwards"].Sensitive = has_first;
			Global.Actions ["SkipForward"  ].Sensitive = has_first;
			Global.Actions ["Shuffle"      ].Sensitive = has_first;

			UpdateTimeLabels (player.Position);

			SavePlaylist (FileUtils.PlaylistFile, !repeat, true);

			// Run PlaylistChangedEvent Handlers
			if (PlaylistChangedEvent != null)
				PlaylistChangedEvent ();
		}

		// Methods :: Private :: SongChanged
		private void SongChanged (bool restart)
		{
			SongChanged (restart, true);
		}

		// Methods :: Private :: SongChanged
		private void SongChanged (bool restart, bool fire_signal)
		{
			if (playlist.Model.Playing != IntPtr.Zero) {
				string tip;
				Song song = Song.FromHandle (playlist.Model.Playing);

				cover_image.Song = song;

				cover_image.HasTooltip = true;

				if (song.Album.Length > 0)
					tip = String.Format (string_from_album, song.Album);
				else
					tip = string_album_unknown;

				if (song.Performers.Length > 0) {
					tip += Environment.NewLine;
					tip += Environment.NewLine;
					string performers_readable = StringUtils.JoinHumanReadable (song.Performers);
					tip += String.Format (string_performers, performers_readable); 
				}
					
				if (song.CoverImage == null && !Global.CoverDB.Loading) {
					tip += Environment.NewLine;
					tip += Environment.NewLine;
					tip += string_tooltip_cover;
				}
				
				cover_image.TooltipText = tip;
				
				// Song label
				string title = StringUtils.EscapeForPango (song.Title);
				
				string artists_tmp =
				  StringUtils.JoinHumanReadable (song.Artists);

				string artists = StringUtils.EscapeForPango (artists_tmp);

				string markup = String.Format ("<span size=\"large\" weight=\"bold\">{0}</span>", title);
				markup += Environment.NewLine;
				markup += artists;
				song_label.Markup = markup;

				// Title
				this.Title = String.Format (string_title_main, song.Title);

				if (player.Song != song || restart)
					player.Song = song;

				if (fire_signal && SongChangedEvent != null)
				    SongChangedEvent (song);

			} else {
				cover_image.Song = null;

				cover_image.HasTooltip = false;

				song_label.Markup = String.Empty;
				time_label.Text   = String.Empty;

				this.Title = string_program;

				if (skip_to_window != null)
					skip_to_window.Hide ();

				if (SongChangedEvent != null)
				    SongChangedEvent (null);
			}
			
			if (restart)
				had_last_eos = false;
		}

		// Methods :: Private :: SelectionChanged
		private void SelectionChanged ()
		{
			int rows = playlist.Selection.CountSelectedRows ();
			Global.Actions ["Remove"].Sensitive = (rows > 0);

			// Run SelectionChangedEvent Handlers
			if (SelectionChangedEvent != null)
				SelectionChangedEvent ();
		}

		// Methods :: Private :: StateChanged
		private new void StateChanged (bool playing, bool dont_signal)
		{
			// Update action entry and button states
			((ToggleAction) Global.Actions ["TogglePlay"]).Active = playing;
			toggle_play_button.Active        = playing;

			// Update
			if (playlist.Model.Playing != IntPtr.Zero)
				playlist.Model.Changed (playlist.Model.Playing);

			// Run StateChangedEvent Handlers
			if (!dont_signal && StateChangedEvent != null)
				StateChangedEvent (playing);
		}

		// Methods :: Private :: ClearPlaylist
		private void ClearPlaylist ()
		{
			playlist.Model.Clear ();
			player.Stop ();
		}

		// Methods :: Private :: SeekTo
		private void SeekTo (int seconds)
		{
			Song song = Song.FromHandle (playlist.Model.Playing);

			if (seconds >= song.Duration) {
				EndOfStream (song, true);

			} else {
				player.Position = (seconds < 0) ? 0 : seconds;
				player.Play ();
			}

			playlist.Select (playlist.Model.Playing);
		}

		// Methods :: Private :: OpenPlaylistInternal
		private void OpenPlaylistInternal
		  (string fn, PlaylistForeachFunc func, object user_data)
		{
			VfsStream stream;
			StreamReader reader;

			try {
				stream = new VfsStream (fn, System.IO.FileMode.Open);
				reader = new StreamReader (stream);

			} catch (Exception e) {
				string fn_readable = FileUtils.MakeHumanReadable (fn);
				string msg = String.Format (string_error_read, fn_readable);
				new ErrorDialog (this, msg, e.Message);
				return;
			}

			string line = null;

			bool playing_song = false;

			while ((line = reader.ReadLine ()) != null) {
				if (line.Length == 0)
					continue;

				if (line.StartsWith ("#")) {
					if (line == "# PLAYING")
						playing_song = true;

					continue;
				}

				// DOS-to-UNIX
				line.Replace ('\\', '/');

				string basename = String.Empty;

				try {
					basename = System.IO.Path.GetFileName (line);

				} catch {
					continue;
				}

				// Get Song
				Song song = Global.DB.GetSong (line);
				
				// If that didn't work, try harder...
				if (song == null) { 
					lock (Global.DB) {
						foreach (string key in Global.DB.Songs.Keys) {
							string key_basename =
							  System.IO.Path.GetFileName (key);

							if (basename != key_basename)
								continue;

							song = Global.DB.GetSong (key);
							break;
						}
					}
				}

				// If we don't have it in our Database, try adding it.
				if (song == null)
					song = AddSongToDB (line);

				// Give up if we don't have the song by now.
				if (song == null)
					return; 

				// Add song (and play) 
				func (song, playing_song, user_data);
				playing_song = false;
			}

			// Close File
			try {
				reader.Close ();

			} catch (Exception e) {
				string fn_readable = FileUtils.MakeHumanReadable (fn);
				string msg = String.Format (string_error_close, fn_readable);
				new ErrorDialog (this, msg, e.Message);
				return;
			}
		}

		// Methods :: Private :: AddSongToDB
		private Song AddSongToDB (string file)
		{
			// Increase BusyLevel
			BusyLevel ++;
			
			// Song
			Song song;
			try {
				song = new Song (file);

			} catch {
				BusyLevel --;
				return null;
			}

			// Add Song
			Global.DB.AddSong (song);

			// Decrease BusyLevel
			BusyLevel --;

			return song;
		}

		// Methods :: Private :: GetSingleSong
		private Song GetSingleSong (string file, bool sync)
		{
			// Get Song
			Song song = Global.DB.GetSong (file);

			// If we don't have it, try adding it
			if (song == null)
				song = AddSongToDB (file);
			else if (sync)
				Global.DB.SyncSong (song);

			return song;
		}

		// Methods :: Private :: EndOfStream
		private void EndOfStream (Song song, bool update_time)
		{
			// If we can, go to the next song
			if (playlist.Model.HasNext) {
				playlist.Model.Next ();

			// If we don't have another song and we are repeating,
			// go to the beginning.
			} else if (repeat) {
				playlist.Model.First ();

			// We have nothing else to play.
			} else {
				if (update_time)
					player.Position = song.Duration;

				had_last_eos = true;

				player.Pause ();
			}

			// Update Changes
			PlaylistChanged ();
		}

		// Methods :: Private :: DragAddSong
		private IntPtr DragAddSong (Song song, DragAddSongPosition pos)
		{
			if (pos.Pointer == IntPtr.Zero) {
				pos.Pointer = AddSong (song.Handle);
			} else {
				pos.Pointer = AddSongAtPos
				  (song.Handle, pos.Pointer, pos.Position);
			}

			pos.Position = TreeViewDropPosition.After;
				
			if (pos.First) {
				playlist.Select (pos.Pointer, false);
				pos.First = false;
			}

			return pos.Pointer;
		}

		// Methods :: Private :: ArrayFromList
		private ISong [] ArrayFromList (List list)
		{
			ISong [] array = new ISong [list.Count];

			for (int i = 0; i < list.Count; i++) {
				int p = (int) list [i];
				IntPtr ptr = new IntPtr (p);
				array [i] = Song.FromHandle (ptr);
			}

			return array;
		}

		// Methods :: Private :: RestoreState
		private void RestoreState ()
		{
			// Window size
			int width  = (int) Config.Get (GConfKeyWidth , GConfDefaultWidth );
			int height = (int) Config.Get (GConfKeyHeight, GConfDefaultHeight);

			// Window position
			int pos_x = (int) Config.Get (GConfKeyPosX, GConfDefaultPosX);
			int pos_y = (int) Config.Get (GConfKeyPosY, GConfDefaultPosY);

			SetDefaultSize (width, height);
			Move (pos_x, pos_y);

			SizeAllocated += OnSizeAllocated;

			// Volume
			Volume = (int) Config.Get (GConfKeyVolume, GConfDefaultVolume);

			Config.AddNotify (GConfKeyVolume,
				new GConf.NotifyEventHandler (OnConfigVolumeChanged));

			// Repeat
			Repeat = (bool) Config.Get (GConfKeyRepeat, GConfDefaultRepeat);

			Config.AddNotify (GConfKeyRepeat,
				new GConf.NotifyEventHandler (OnConfigRepeatChanged));
		}
		
		//Methods:: Private :: SaveWindowPosition
		private void SaveWindowPosition ()
		{
			// Get Window position
			int pos_x, pos_y;
			GetPosition (out pos_x, out pos_y);

			// Save it to GConf
			Config.Set (GConfKeyPosX , pos_x );
			Config.Set (GConfKeyPosY, pos_y);
		}

		// Handlers
		// Handlers :: OnStateChanged
		private void OnStateChanged (bool playing)
		{
			StateChanged (playing, false);
		}

		// Handlers :: OnInvalidSong
		private void OnInvalidSong (Song song)
		{
			Global.DB.RemoveSong (song);
		}

		// Handlers :: OnWindowStateEvent
		private void OnWindowStateEvent (object o, WindowStateEventArgs args)
		{
			// If know we're not even visible, return
			if (!this.Visible)
				return;
			
			// If we're not Iconified or Withdrawn, show the window
			bool old_window_visible = window_visible;
			
			bool is_iconified =
			  (args.Event.NewWindowState == Gdk.WindowState.Iconified);

			bool is_withdrawn =
			  (args.Event.NewWindowState == Gdk.WindowState.Withdrawn);

			window_visible = (!is_iconified && !is_withdrawn);

			// If we changed, update
			if (old_window_visible != window_visible)
				UpdateWindowVisibilityUI ();
		}

		// Handlers :: OnVisibilityNotifyEvent
		private void OnVisibilityNotifyEvent
		  (object o, VisibilityNotifyEventArgs args)
		{
			// If we're not visible, iconified, or withdrawn, return.
			bool is_iconified =
			  (GdkWindow.State == Gdk.WindowState.Iconified);

			bool is_withdrawn =
			  (GdkWindow.State == Gdk.WindowState.Withdrawn);
			
			if (!Visible || is_iconified || is_withdrawn)
				return;

			// See if we became visible (not FullyObscured)
			bool old_window_visible = window_visible;

			window_visible =
			  (args.Event.State != Gdk.VisibilityState.FullyObscured);

			// If we did, update
			if (old_window_visible != window_visible)
				UpdateWindowVisibilityUI ();

			args.RetVal = false;
		}

		// Handlers :: OnDeleteEvent
		private void OnDeleteEvent (object o, DeleteEventArgs args)
		{
			Quit ();
		}

		// Handlers :: OnSizeAllocated
		private void OnSizeAllocated (object o, SizeAllocatedArgs args)
		{
			// Get Window size
			int width, height;
			GetSize (out width, out height);

			// Save it to GConf
			Config.Set (GConfKeyWidth , width );
			Config.Set (GConfKeyHeight, height);
		}

		// Handlers :: OnVolumeChanged
		private void OnVolumeChanged (int vol)
		{
			// Update volume if changed
			if (vol == this.Volume)
				return;

			this.Volume = vol;
		}

		// Handlers :: OnConfigVolumeChanged
		private void OnConfigVolumeChanged
		  (object o, GConf.NotifyEventArgs args)
		{
			// Get new volume from GConf
			int vol = (int) args.Value;

			// Updated volume if changed
			if (vol == this.Volume)
				return;

			this.Volume = (int) args.Value;
		}

		// Handlers :: OnQueueSongsEvent
		private void OnQueueSongsEvent (List songs)
		{
			bool start_playing = (had_last_eos || !playlist.Model.HasFirst);
			
			// Add Songs
			foreach (int i in songs)
				AddSong (new IntPtr (i));

			// Play
			EnsurePlaying ();
			
			// Update
			PlaylistChanged ();

			// Start playing if necessary
			if (!start_playing)
				return;

			player.Play ();
		}

		// Handlers :: OnPlaySongsEvent		
		private void OnPlaySongsEvent (List songs)
		{
			// Add Songs
			bool first = true;
			foreach (int i in songs) {
				IntPtr p = new IntPtr (i);
				
				IntPtr new_p = AddSong (p);
				
				if (!first)
					continue;

				// Select and Play the first song
				PlayAndSelect (new_p);
				
				// We only have one first
				first = false;
			}

			// Update
			PlaylistChanged ();
			player.Play ();
		}

		// Handlers :: OnQueueAlbumsEvent
		private void OnQueueAlbumsEvent (List albums)
		{
			bool start_playing = (had_last_eos || !playlist.Model.HasFirst);

			// Add songs from albums
			foreach (int i in albums) {
				Album a = Album.FromHandle (new IntPtr (i));

				foreach (Song s in a.Songs)
					AddSong (s);
			}

			// Play
			EnsurePlaying ();
			
			// Update
			PlaylistChanged ();

			// Start playing if necessary
			if (!start_playing)
				return;

			player.Play ();
		}

		// Handlers :: OnPlayAlbumsEvent
		private void OnPlayAlbumsEvent (List albums)
		{
			// Add songs from albums
			bool first = true;
			foreach (int i in albums) {
				Album a = Album.FromHandle (new IntPtr (i));

				foreach (Song s in a.Songs) {
					IntPtr new_p = AddSong (s);

					if (!first)
						continue;

					// Select and play the first song
					PlayAndSelect (new_p);

					// There's only one first
					first = false;
				}
			}

			// Update
			PlaylistChanged ();
			player.Play ();
		}

		// Handlers :: OnTickEvent
		private void OnTickEvent (int pos)
		{
			UpdateTimeLabels (pos);

			if (TickEvent != null)
				TickEvent (pos);
		}

		// Handlers :: OnEndOfStreamEvent
		private void OnEndOfStreamEvent ()
		{
			// Get current song
			Song song = Song.FromHandle (playlist.Model.Playing);

			// If we're not really at the end, we must have had bad info
			// Update the SongDB with the new length
			if (song.Duration != player.Position) {
				song.Duration = player.Position;
				Global.DB.SaveSong (song);

				// So that any people listening to tick events
				// update their time labels with the new duration
				if (TickEvent != null)
					TickEvent (song.Duration);
			}
			
			// Do what else we need to do at the EOS
			EndOfStream (song, false);
		}

		// Handlers :: OnConfigRepeatChanged
		private void OnConfigRepeatChanged
		  (object o, GConf.NotifyEventArgs args)
		{
			// Get new repeat setting from GConf
			bool val = (bool) args.Value;

			// If it changed, update.
			if (val == this.repeat)
				return;

			this.Repeat = val;
		}

		// Handlers :: OnPlaylistRowActivated
		private void OnPlaylistRowActivated (object o, RowActivatedArgs args)
		{
			IntPtr handle = playlist.Model.HandleFromPath (args.Path);
			
			// Play selected song
			playlist.Model.Playing = handle;
			PlaylistChanged ();
			player.Play ();
		}

		// Handlers :: OnPlaylistSelectionChanged
		private void OnPlaylistSelectionChanged (object o, EventArgs args)
		{
			SelectionChanged ();
		}

		// Handlers :: OnWatchedFoldersChanged
		private void OnWatchedFoldersChanged ()
		{
			if (WatchedFoldersChangedEvent != null)
				WatchedFoldersChangedEvent ();
		}

		// Handlers :: OnPlaylistPlayingChanged
		private void OnPlaylistPlayingChanged (IntPtr playing)
		{
			if (ignore_song_change)
				return;

			SongChanged (true);
		}

		// Handlers :: OnSongChanged
		private void OnSongChanged (Song song)
		{
			bool song_changed = false;
			foreach (IntPtr h in song.Handles) {
				if (!playlist.Model.Contains (h))
					continue;

				song_changed = true;
				
				// Use overload of SongChanged that won't fire the
				// "SongChanged" event, since we really only want to update
				// the pixbuf, labels, etc.
				if (h == playlist.Model.Playing)
					SongChanged (false, false);

				playlist.Model.Changed (h);
			}
			
			if (!song_changed)
				return;

			PlaylistChanged ();
		}

		// Handlers :: OnPlayingSongRemoved
		private void OnPlayingSongRemoved ()
		{
			// Try going forward
			if (playlist.Model.HasNext) {
				playlist.Model.Next ();
				return;
			}

			// Try going backwards
			if (playlist.Model.HasPrevious) {
				playlist.Model.Previous ();
				return;
			}

			// Playlist must be empty
			playlist.Model.Playing = IntPtr.Zero;
			player.Stop ();
		}

		// Handlers :: OnSongRemoved
		private void OnSongRemoved (Song song)
		{
			bool n_songs_changed = false;
			
			foreach (IntPtr h in song.Handles) {
				if (!playlist.Model.Contains (h))
					continue;

				n_songs_changed = true;
				
				if (h == playlist.Model.Playing)
					OnPlayingSongRemoved ();

				if ((playlist.Selection.CountSelectedRows () == 1) &&
				    ((int) playlist.SelectedHandles [0] == (int) h &&
				    !playlist.SelectNext ()))
					playlist.SelectPrevious ();

				playlist.Model.Remove (h);
			}
			
			if (!n_songs_changed)
				return;

			PlaylistChanged ();
		}

		// Handlers :: OnPlaylistDragDataGet
		private void OnPlaylistDragDataGet (object o, DragDataGetArgs args)
		{
			List songs = playlist.SelectedHandles;

			string target;
			Gdk.Atom atom;
			byte [] bytes;

			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string files = String.Empty;

				foreach (int p in songs) {
					IntPtr s = new IntPtr (p);
					
					Song song = Song.FromHandle (s);
					string uri = Gnome.Vfs.Uri.GetUriFromLocalPath (song.Filename);
					
					files += (uri + "\r\n");
				}

				target = DndUtils.TargetUriList.Target;
				atom = Gdk.Atom.Intern (target, false);
				bytes = System.Text.Encoding.UTF8.GetBytes (files);
				args.SelectionData.Set (atom, 8, bytes);

				break;

			case (uint) DndUtils.TargetType.ModelRow:
				target = DndUtils.TargetMuineTreeModelRow.Target;

				string ptrs = String.Format ("\t{0}\t", target);

				foreach (int p in songs) {
					IntPtr s = new IntPtr (p);
					ptrs += s.ToString () + "\r\n";
				}

				atom = Gdk.Atom.Intern (target, false);
				bytes = System.Text.Encoding.ASCII.GetBytes (ptrs);
				args.SelectionData.Set (atom, 8, bytes);

				break;

			default:
				break;	
			}
		}

		// Handlers :: OnPlaylistDragDataReceived
		private void OnPlaylistDragDataReceived
		  (object o, DragDataReceivedArgs args)
		{
			string data = DndUtils.SelectionDataToString (args.SelectionData);

			bool success = true;

			DragAddSongPosition pos = new DragAddSongPosition ();
			pos.First = true;

			// Get path and position
			TreePath path;
			TreeViewDropPosition tmp_pos;

			bool has_dest =
			  playlist.GetDestRowAtPos (args.X, args.Y, out path, out tmp_pos);

			if (has_dest) {
				pos.Pointer = playlist.Model.HandleFromPath (path);
				pos.Position = tmp_pos;
			}

			// Work around Gtk bug #164085
			string target;
			string fmt = "\t{0}\t";
			
			target = DndUtils.TargetMuineTreeModelRow.Target;
			string tree_model_row = String.Format (fmt, target);

			target = DndUtils.TargetMuineSongList.Target;
			string song_list      = String.Format (fmt, target);
			
			target = DndUtils.TargetMuineAlbumList.Target;
			string album_list     = String.Format (fmt, target);
			
			bool is_tree_model = data.StartsWith (tree_model_row);
			bool is_song_list  = data.StartsWith (song_list     );
			bool is_album_list = data.StartsWith (album_list    );

			// Type			
			uint type;
			
			if      (is_tree_model) type = (uint) DndUtils.TargetType.ModelRow ;
			else if (is_song_list ) type = (uint) DndUtils.TargetType.SongList ;
			else if (is_album_list) type = (uint) DndUtils.TargetType.AlbumList;
			else                    type = (uint) DndUtils.TargetType.UriList  ;

			// Head
			string head;
			
			if      (is_tree_model) head = tree_model_row;
			else if (is_song_list ) head = song_list;
			else if (is_album_list) head = album_list;
			else                    head = String.Empty;

			data = data.Substring (head.Length);

			// Type
			//	TODO: Refactor these
			string [] bits = DndUtils.SplitSelectionData (data);

			switch (type) {
			
			// Song or Row
			case (uint) DndUtils.TargetType.SongList:
			case (uint) DndUtils.TargetType.ModelRow:
				foreach (string s in bits) {
					IntPtr ptr;

					try { 
						ptr = new IntPtr (Int64.Parse (s)); 
					} catch { 	
						continue;
					}

					Song song = Song.FromHandle (ptr);

					bool play = false;

					// Reorder part 1: remove old row
					if (type == (uint) DndUtils.TargetType.ModelRow) {
						if (ptr == pos.Pointer)
							break;

						if (ptr == playlist.Model.Playing) {
							play = true;
							ignore_song_change = true;
						}
						
						RemoveSong (ptr);
					}

					ptr = DragAddSong (song, pos);

					// Reorder part 2: if the row was playing, keep it playing
					if (!play)
						continue;
					
					playlist.Model.Playing = ptr;
					ignore_song_change = false;
				}

				EnsurePlaying ();
				PlaylistChanged ();

				break;

			// Album
			case (uint) DndUtils.TargetType.AlbumList:
				foreach (string s in bits) {
					IntPtr ptr;
					
					try {
						ptr = new IntPtr (Int64.Parse (s));

					} catch {
						continue;
					}
					
					Album album = Album.FromHandle (ptr);
					
					foreach (Song song in album.Songs)
						DragAddSong (song, pos);
				}
				
				EnsurePlaying ();
				PlaylistChanged ();

				break;

			// Uri
			case (uint) DndUtils.TargetType.UriList:
				success = false;

				bool added_files = false;

				ArrayList new_dinfos = new ArrayList ();

				foreach (string s in bits) {
					string fn = Gnome.Vfs.Uri.GetLocalPathFromUri (s);

					if (fn == null)
						continue;
		
					DirectoryInfo dinfo = new DirectoryInfo (fn);
					
					if (dinfo.Exists) {
						new_dinfos.Add (dinfo);
						continue;
					}

					System.IO.FileInfo finfo = new System.IO.FileInfo (fn);
						
					if (!finfo.Exists)
						continue;
						
					if (FileUtils.IsPlaylist (fn)) {
						BusyLevel ++;

						OpenPlaylistInternal (fn,
							new PlaylistForeachFunc (DragPlaylistForeachFunc),
							pos);

						BusyLevel --;

						added_files = true;
						
						continue;
					}

					Song song = GetSingleSong (finfo.FullName, false);
						
					if (song != null) {
						DragAddSong (song, pos);
						added_files = true;
					}
				}

				if (added_files) {
					EnsurePlaying ();
					PlaylistChanged ();
					success = true;
				}

				if (new_dinfos.Count > 0) {
					Global.DB.AddWatchedFolders (new_dinfos);
					success = true;
				}

				break;

			// Default
			default:
				break;
			}

			Drag.Finish (args.Context, success, false, args.Time);
		}

		// Handlers :: OnTogglePlayButtonClicked
		private void OnTogglePlayButtonClicked (object o, EventArgs args)
		{
			if (toggle_play_button.Active == Playing)
				return;

			Playing = toggle_play_button.Active;
		}
		
		// Handlers :: OnPreviousButtonClicked
		private void OnPreviousButtonClicked (object o, EventArgs args)
		{
			Previous ();
		}
		
		// Handlers :: OnNextButtonClicked
		private void OnNextButtonClicked (object o, EventArgs args)
		{
			Next ();
		}
		
		// Handlers :: OnAddSongButtonClicked
		private void OnAddSongButtonClicked (object o, EventArgs args)
		{
			PlaySong (Gtk.Global.CurrentEventTime);
		}
		
		// Handlers :: OnAddAlbumButtonClicked
		private void OnAddAlbumButtonClicked (object o, EventArgs args)
		{
			PlayAlbum (Gtk.Global.CurrentEventTime);
		}
		
		// Handlers :: OnPlaylistLabelDragDataGet
		private void OnPlaylistLabelDragDataGet
		  (object o, DragDataGetArgs args)
		{
			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string file =
				  System.IO.Path.Combine
				    (FileUtils.TempDirectory, string_playlist_filename);

				SavePlaylist (file, false, false);
				
				string uri = Gnome.Vfs.Uri.GetUriFromLocalPath (file);

				string target = DndUtils.TargetUriList.Target;
				Gdk.Atom atom = Gdk.Atom.Intern (target, false);
				byte [] bytes = System.Text.Encoding.UTF8.GetBytes (uri);
				args.SelectionData.Set (atom, 8, bytes);

				break;

			default:
				break;
			}
		}

		// Handlers :: OnDragDataReceived
		private void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			if (args.Info != (uint) DndUtils.TargetType.UriList) {
				Drag.Finish (args.Context, false, false, args.Time);
				return;
			}

			string [] bits = DndUtils.SplitSelectionData (args.SelectionData);

			ArrayList new_dinfos = new ArrayList ();

			bool success = false;

			foreach (string s in bits) {
				string fn = Gnome.Vfs.Uri.GetLocalPathFromUri (s);

				if (fn == null)
					continue;
		
				DirectoryInfo dinfo = new DirectoryInfo (fn);
					
				if (dinfo.Exists) {
					new_dinfos.Add (dinfo);
					continue;
				}

				System.IO.FileInfo finfo = new System.IO.FileInfo (fn);
						
				if (!finfo.Exists)
					continue;
						
				if (!FileUtils.IsPlaylist (fn))
					continue;

				OpenPlaylist (fn);

				success = true;
			}

			if (new_dinfos.Count > 0) {
				Global.DB.AddWatchedFolders (new_dinfos);
				success = true;
			}

			Drag.Finish (args.Context, success, false, args.Time);
		}
		
		// Delegate Functions
		// Delegate Functions :: PixbufCellDataFunc
		private void PixbufCellDataFunc
		  (TreeViewColumn col, CellRenderer cell, TreeModel model,
		   TreeIter iter)
		{
			/*
			Gtk.CellRendererPixbuf r = (Gtk.CellRendererPixbuf) cell;
			*/
			ColoredCellRendererPixbuf r = (ColoredCellRendererPixbuf) cell;

			IntPtr handle = playlist.Model.HandleFromIter (iter);

			if (handle == playlist.Model.Playing) {
				string icon = (player.Playing) ? "muine-playing" : "muine-paused";				
				r.Pixbuf = playlist.RenderIcon (icon, IconSize.Menu, null);

			} else {
				r.Pixbuf = empty_pixbuf;
			}
		}

		// Delegate Functions :: TextCellDataFunc
		private void TextCellDataFunc
		  (TreeViewColumn col, CellRenderer cell, TreeModel model,
		   TreeIter iter)
		{
			IntPtr song_ptr = playlist.Model.HandleFromIter (iter);
			Song song = Song.FromHandle (song_ptr);
			CellRendererText r = (CellRendererText) cell;

			string title = StringUtils.EscapeForPango (song.Title);
			string artists_tmp = StringUtils.JoinHumanReadable (song.Artists);
			string artists = StringUtils.EscapeForPango (artists_tmp);

			string markup = String.Format ("<b>{0}</b>", title);
			markup += Environment.NewLine;
			markup += artists;
			r.Markup = markup;
		}

		// Delegate Functions :: ShuffleFunc		
		private int ShuffleFunc (IntPtr ap, IntPtr bp)
		{
			double a = (double) random_sort_keys [(int) ap];
			double b = (double) random_sort_keys [(int) bp];

			return a.CompareTo (b);
		}		

		// Delegate Functions :: DragPlaylistForeachFunc
		private void DragPlaylistForeachFunc
		  (Song song, bool playing, object user_data)
		{
			DragAddSongPosition pos = (DragAddSongPosition) user_data;
			DragAddSong (song, pos);
		}

		// Delegate Functions :: RestorePlaylistForeachFunc
		private void RestorePlaylistForeachFunc
		  (Song song, bool playing, object user_data)
		{
			IntPtr p = AddSong (song);

			if (!playing)
				return;

			PlayAndSelect (p);
		}
		
		// Delegate Functions :: RegularPlaylistForeachFunc
		private void RegularPlaylistForeachFunc
		  (Song song, bool playing, object user_data)
		{
			AddSong (song);
		}

		// Internal Classes
		// Internal Classes :: DragAddSongPosition
		//	FIXME: Jorn says this needs to be a class, not a struct
		//	I'm still not understanding quite why...
		private class DragAddSongPosition 
		{
			public IntPtr               Pointer ;
			public TreeViewDropPosition Position;
			public bool                 First   ;
		}
	}
}
