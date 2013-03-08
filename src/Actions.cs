/*
 * Copyright (C) 2005 Tamara Roberson <tamara.roberson@gmail.com>
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
using System.Collections;

using Mono.Unix;

using Gtk;

namespace Muine
{
	public class Actions : ActionGroup
	{
		// Strings
		// Strings :: Menu
		private static readonly string string_file_menu =
			Catalog.GetString ("_File");

		private static readonly string string_song_menu =
			Catalog.GetString ("_Song");

		private static readonly string string_playlist_menu =
			Catalog.GetString ("_Playlist");

		private static readonly string string_help_menu =
			Catalog.GetString ("_Help");

		// Strings :: Menu :: File
		private static readonly string string_import =
			Catalog.GetString ("_Import Folder...");

		private static readonly string string_open =
			Catalog.GetString ("_Open...");

		private static readonly string string_save =
			Catalog.GetString ("_Save As...");

		private static readonly string string_toggle_visible =
			Catalog.GetString ("Show _Window");

		// Strings :: Menu :: Song
		private static readonly string string_toggle_play =
			Catalog.GetString ("_Play");

		private static readonly string string_previous =
			Catalog.GetString ("P_revious");

		private static readonly string string_next =
			Catalog.GetString ("_Next");

		private static readonly string string_skip_to =
			Catalog.GetString ("_Skip to...");

		private static readonly string string_skip_backwards =
			Catalog.GetString ("Skip _Backwards");

		private static readonly string string_skip_forward =
			Catalog.GetString ("Skip _Forward");

		// Strings :: Menu :: Playlist
		private static readonly string string_play_song =
			Catalog.GetString ("Play _Song...");

		private static readonly string string_play_album =
			Catalog.GetString ("Play _Album...");

		private static readonly string string_remove =
			Catalog.GetString ("_Remove Song");

		private static readonly string string_remove_played =
			Catalog.GetString ("Remove _Played Songs");

		private static readonly string string_clear =
			Catalog.GetString ("_Clear");

		private static readonly string string_toggle_repeat =
			Catalog.GetString ("R_epeat");

		private static readonly string string_shuffle =
			Catalog.GetString ("Shu_ffle");

		// Strings :: Menu :: Help
		private static readonly string string_about =
			Catalog.GetString ("_About");

		// Static
		// Static :: Objects
		// Static :: Objects :: Entries
		private static ActionEntry [] entries = {
			new ActionEntry ("FileMenu", null, string_file_menu,
				null, null, null),

			new ActionEntry ("SongMenu", null, string_song_menu,
				null, null, null),

			new ActionEntry ("PlaylistMenu", null, string_playlist_menu,
				null, null, null),

			new ActionEntry ("HelpMenu", null, string_help_menu,
				null, null, null),

			new ActionEntry ("Import", Stock.Execute, string_import,
				null, null, null),

			new ActionEntry ("Open", Stock.Open, string_open,
				"<control>O", null, null),

			new ActionEntry ("Save", Stock.SaveAs, string_save,
				"<shift><control>S", null, null),

			new ActionEntry ("Quit", Stock.Quit, null,
				"<control>Q", null, null),
			
			new ActionEntry ("Previous", "stock_media-prev", string_previous,
				"B", null, null),

			new ActionEntry ("Next", "stock_media-next", string_next,
				"N", null, null),

			new ActionEntry ("SkipTo", Stock.JumpTo, string_skip_to,
				"T", null, null),

			new ActionEntry ("SkipBackwards", "stock_media-rew", string_skip_backwards,
				"<control>Left", null, null),

			new ActionEntry ("SkipForward", "stock_media-fwd", string_skip_forward,
				"<control>Right", null, null),

			new ActionEntry ("PlaySong", Stock.Add, string_play_song,
				"S", null, null),

			new ActionEntry ("PlayAlbum", "stock_music-library", string_play_album,
				"A", null, null),

			new ActionEntry ("Remove", Stock.Remove, string_remove,
				"Delete", null, null),

			new ActionEntry ("RemovePlayed", null, string_remove_played,
				"<control>Delete", null, null),

			new ActionEntry ("Clear", Stock.Clear, string_clear,
				"<control>L", null, null),

			new ActionEntry ("Shuffle", "stock_shuffle", string_shuffle,
				"<control>S", null, null),

			new ActionEntry ("About", Gnome.Stock.About, string_about,
				null, null, null)
		};

		// Static :: Objects :: Toggle Entries
		private static ToggleActionEntry [] toggle_entries = {
			new ToggleActionEntry ("TogglePlay", "stock_media-play", string_toggle_play,
			       "P", null, null, false),

			new ToggleActionEntry ("ToggleRepeat", null, string_toggle_repeat,
			       "<control>R", null, null, false),

			new ToggleActionEntry ("ToggleVisible", null, string_toggle_visible,
				"Escape", null, null, true),

		};

		// Static :: Properties :: Entries (get;)
		/// <summary>
		/// 	The defined actions.
		/// </summary>
		/// <returns>
		///	An array of <see cref="ActionEntry" />.
		/// </returns>
		public static ActionEntry [] Entries {
			get { return entries; }
		}
		
		// Static :: Properties :: ToggleEntries (get;)
		/// <summary>
		/// 	The defined toggle actions.
		/// </summary>
		/// <returns>
		///	An array of <see cref="ToggleActionEntry" />
		/// </returns>
		public static ToggleActionEntry [] ToggleEntries {
			get { return toggle_entries; }
		}

		// Objects
		private UIManager ui_manager = new UIManager ();

		// Constructor
		/// <summary>
		///	Create a new <see cref="Actions" /> object.
		/// </summary>
		/// <remarks>
		/// 	This object manages the actions for menu items.
		/// </remarks>
		public Actions () : base ("Actions")
		{
			Add (entries);
			Add (toggle_entries);

			ui_manager.InsertActionGroup (this, 0);
			ui_manager.AddUiFromResource ("PlaylistWindow.xml");
			
			// Setup Callbacks
			this ["Import"       ].Activated += OnImport;
			this ["Open"         ].Activated += OnOpen;
			this ["Save"         ].Activated += OnSave;
			this ["ToggleVisible"].Activated += OnToggleVisible;
			this ["Quit"         ].Activated += OnQuit;
			this ["Previous"     ].Activated += OnPrevious;
			this ["Next"         ].Activated += OnNext;
			this ["SkipTo"       ].Activated += OnSkipTo;
			this ["SkipBackwards"].Activated += OnSkipBackwards;
			this ["SkipForward"  ].Activated += OnSkipForward;
			this ["PlaySong"     ].Activated += OnPlaySong;
			this ["PlayAlbum"    ].Activated += OnPlayAlbum;
			this ["Remove"       ].Activated += OnRemove;
			this ["RemovePlayed" ].Activated += OnRemovePlayed;
			this ["Clear"        ].Activated += OnClear;
			this ["Shuffle"      ].Activated += OnShuffle;
			this ["About"        ].Activated += OnAbout;
			this ["TogglePlay"   ].Activated += OnTogglePlay;
			this ["ToggleRepeat" ].Activated += OnToggleRepeat;
		}

		// Properties
		// Properties :: UIManager (get;)
		/// <summary>
		/// 	The UIManager.
		/// </summary>
		/// <returns>
		///	A <see cref="UIManager" />.
		/// </returns>
		public UIManager UIManager {
			get { return ui_manager; }
		}
		
		// Properties :: MenuBar (get;)
		//	TODO: Change return type to Gtk.MenuBar?
		/// <summary>
		/// 	Contains the MenuBar.
		/// </summary>
		/// <returns>
		///	A <see cref="Gtk.Widget" />.
		/// </returns>
		public Gtk.Widget MenuBar {
			get { return ui_manager.GetWidget ("/MenuBar"); }
		}
		
		// Handlers
		// Handlers :: OnImport
		/// <summary>
		/// 	Handler called when the Import action is activated.
		/// </summary>
		/// <remarks>
		///	This opens the <see cref="ImportDialog" /> window.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnImport (object o, EventArgs args)
		{
			new ImportDialog ();
		}

		// Handlers :: OnOpen
		/// <summary>
		/// 	Handler called when the Open action is activated.
		/// </summary>
		/// <remarks>
		///	This opens the <see cref="OpenDialog" /> window.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnOpen (object o, EventArgs args)
		{
			new OpenDialog ();
		}

		// Handlers :: OnSave
		/// <summary>
		/// 	Handler called when the Save action is activated.
		/// </summary>
		/// <remarks>
		///	This opens the <see cref="SaveDialog" /> window.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnSave (object o, EventArgs args)
		{
			new SaveDialog ();
		}
		
		// Handlers :: OnToggleWindowVisible
		/// <summary>
		/// 	Handler called when the ToggleVisible action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.ToggleVisible" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnToggleVisible (object o, EventArgs args)
		{
			ToggleAction a = (ToggleAction) o;

			if (a.Active == Global.Playlist.Visible)
				return;

			Global.Playlist.SetWindowVisible
			  (!Global.Playlist.WindowVisible, Gtk.Global.CurrentEventTime);
		}

		// Handlers :: OnQuit
		/// <summary>
		/// 	Handler called when the Quit action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.Quit" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnQuit (object o, EventArgs args)
		{
			Global.Playlist.Quit ();
		}

		// Handlers :: OnPrevious
		/// <summary>
		/// 	Handler called when the Previous action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.Previous" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnPrevious (object o, EventArgs args)
		{
			Global.Playlist.Previous ();
		}
		
		// Handlers :: OnNext
		/// <summary>
		/// 	Handler called when the Next action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.Next" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnNext (object o, EventArgs args)
		{
			Global.Playlist.Next ();
		}
		
		// Handlers :: OnSkipTo
		/// <summary>
		/// 	Handler called when the SkipTo action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.RunSkipToDialog" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnSkipTo (object o, EventArgs args)
		{
			Global.Playlist.RunSkipToDialog ();
		}

		// Handlers :: OnSkipBackwards
		/// <summary>
		/// 	Handler called when the SkipBackwards action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.SkipBackwards" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnSkipBackwards (object o, EventArgs args)
		{
			Global.Playlist.SkipBackwards ();
		}

		// Handlers :: OnSkipForward
		/// <summary>
		/// 	Handler called when the SkipForward action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.SkipForward" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnSkipForward (object o, EventArgs args)
		{
			Global.Playlist.SkipForward ();
		}

		// Handlers :: OnPlaySong
		/// <summary>
		/// 	Handler called when the PlaySong action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.PlaySong" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnPlaySong (object o, EventArgs args)
		{
			Global.Playlist.PlaySong (Gtk.Global.CurrentEventTime);
		}

		// Handlers :: OnPlayAlbum
		/// <summary>
		///	Handler called when the PlayAlbum action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.PlayAlbum" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnPlayAlbum (object o, EventArgs args)
		{
			Global.Playlist.PlayAlbum (Gtk.Global.CurrentEventTime);
		}

		// Handlers :: OnRemove
		/// <summary>
		/// 	Handler called when the Remove action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.RemoveSelected" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnRemove (object o, EventArgs args)
		{
			Global.Playlist.RemoveSelected ();
		}

		// Handlers :: OnRemovePlayed
		/// <summary>
		/// 	Handler called when the RemovePlayed action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.RemovePlayed" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnRemovePlayed (object o, EventArgs args)
		{
			Global.Playlist.RemovePlayed ();
		}

		// Handlers :: OnClear
		/// <summary>
		/// 	Handler called when the Clear action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.Clear" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnClear (object o, EventArgs args)
		{
			Global.Playlist.Clear ();
		}

		// Handlers :: OnShuffle
		/// <summary>
		/// 	Handler called when the Shuffle action is activated.
		/// </summary>
		/// <remarks>
		///	This calls <see cref="PlaylistWindow.Shuffle" />.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnShuffle (object o, EventArgs args)
		{
			Global.Playlist.Shuffle ();
		}

		// Handlers :: OnAbout
		/// <summary>
		/// 	Handler called when the About action is activated.
		/// </summary>
		/// <remarks>
		///	This creates a new <see cref="Muine.About" /> window.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnAbout (object o, EventArgs args)
		{
			new Muine.About (Global.Playlist);
		}

		// Handlers :: OnTogglePlay
		/// <summary>
		/// 	Handler called when the TogglePlay action is activated.
		/// </summary>
		/// <remarks>
		///	This sets <see cref="PlaylistWindow.Playing" /> to the
		///	state of the TogglePlay action.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnTogglePlay (object o, EventArgs args)
		{
			ToggleAction a = (ToggleAction) o;

			if (a.Active == Global.Playlist.Playing)
				return;

			Global.Playlist.Playing = a.Active;
		}

		// Handlers :: OnToggleRepeat
		/// <summary>
		/// 	Handler called when the ToggleRepeat action is activated.
		/// </summary>
		/// <remarks>
		///	This sets <see cref="PlaylistWindow.Repeat" /> to the
		///	state of the ToggleRepeat action.
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="EventArgs" />.
		/// </param>
		private void OnToggleRepeat (object o, EventArgs args)
		{
			ToggleAction a = (ToggleAction) o;

			if (a.Active == Global.Playlist.Repeat)
				return;

			Global.Playlist.Repeat = a.Active;
		}
	}
}
