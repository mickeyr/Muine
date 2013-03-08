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
using Mono.Unix;

using Gnome.Vfs;

namespace Muine
{
	public class AddSongWindow : AddWindow
	{
		// GConf
		// GConf :: Width
		private const string GConfKeyWidth = "/apps/muine/add_song_window/width";
		private const int GConfDefaultWidth = 500;

		// GConf :: Height
		private const string GConfKeyHeight = "/apps/muine/add_song_window/height";
		private const int GConfDefaultHeight = 475;  


		// Strings
		private static readonly string string_title = 
			Catalog.GetString ("Play Song");


		// Static
		// Static :: Objects
		// Static :: Objects :: DnD targets
		private static Gtk.TargetEntry [] source_entries = {
			DndUtils.TargetMuineSongList,
			DndUtils.TargetUriList
		};


		// Constructor
		/// <summary>Creates a new Add Song window.</summary>
		/// <remarks>This is created when "Play Song" is clicked.</remarks>
		public AddSongWindow ()
		{
			base.Title = string_title;

			base.SetGConfSize
			  (GConfKeyWidth , GConfDefaultWidth,
			   GConfKeyHeight, GConfDefaultHeight);

			base.Items = Global.DB.Songs.Values;
						
			base.List.Model.SortFunc = new HandleModel.CompareFunc (SortFunc);

			// Column
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			col.Sizing = Gtk.TreeViewColumnSizing.Fixed;
			col.PackStart (base.TextRenderer, true);
			
			Gtk.TreeCellDataFunc func =
			  new Gtk.TreeCellDataFunc (CellDataFunc);

			col.SetCellDataFunc (base.TextRenderer, func);

			base.List.AppendColumn (col);

			// Setup drag and drop			
			base.List.DragSource = source_entries;
			base.List.DragDataGet += OnDragDataGet;

			// Setup handlers
			Global.DB.SongAdded   += base.OnAdded;
			Global.DB.SongChanged += base.OnChanged;
			Global.DB.SongRemoved += base.OnRemoved;
		}


		// Methods
		// Methods :: Private
		// Methods :: Private :: GetArtists
		private string GetArtists (Song song)
		{
			string tmp = StringUtils.JoinHumanReadable (song.Artists);
			return StringUtils.EscapeForPango (tmp);
		}

		// Methods :: Private :: GetSong
		private Song GetSong (Gtk.TreeIter iter)
		{
			IntPtr ptr = base.List.Model.HandleFromIter (iter);
			return GetSong (ptr);
		}

		private Song GetSong (int ptr_i)
		{
			IntPtr ptr = new IntPtr (ptr_i);
			return GetSong (ptr);
		}

		private Song GetSong (IntPtr ptr)
		{
			return Song.FromHandle (ptr);
		}

		// Methods :: Private :: GetTitle
		private string GetTitle (Song song)
		{
			return StringUtils.EscapeForPango (song.Title);
		}

		// Methods :: Private :: SetText
		private void SetText (Gtk.CellRendererText cell, Gtk.TreeIter iter)
		{			
			// Song
			Song song = GetSong (iter);
			
			// Info
			string title   = GetTitle   (song);
			string artists = GetArtists (song);

			// Format
			string markup = String.Format ("<b>{0}</b>", title);
			markup += Environment.NewLine;
			markup += artists;
			cell.Markup = markup;
		}

		// Methods :: Private :: ProcessDragDataGetSongList
		private void ProcessDragDataGetSongList
		  (GLib.List songs, Gtk.DragDataGetArgs args)
		{
			string target = DndUtils.TargetMuineSongList.Target;
	
			string data = String.Format ("\t{0}\t", target);
			
			foreach (int ptr_i in songs) {
				IntPtr ptr = new IntPtr (ptr_i);
				string ptr_s = ptr.ToString ();
				data += (ptr_s + "\r\n");
			}
			
			SetSelectionData (target, data, args);
		}

		// Methods :: Private :: ProcessDragDataGetUriList
		private void ProcessDragDataGetUriList
		  (GLib.List songs, Gtk.DragDataGetArgs args)
		{
			string data = String.Empty; 

			foreach (int song_ptr_i in songs) {
				Song song = GetSong (song_ptr_i);
				string uri = Gnome.Vfs.Uri.GetUriFromLocalPath (song.Filename);
				data += (uri + "\r\n");
			}

			SetSelectionData (DndUtils.TargetUriList.Target, data, args);
		}

		// Methods :: Private :: SetSelectionData
		private void SetSelectionData
		  (string target, string data, Gtk.DragDataGetArgs args)
		{
			Gdk.Atom atom = Gdk.Atom.Intern (target, false);
			byte [] bytes = System.Text.Encoding.ASCII.GetBytes (data); 
			args.SelectionData.Set (atom, 8, bytes);
		}


		// Handlers
		// Handlers :: OnDragDataGet (Gtk.DragDataGetHandler)
		/// <summary>Handler to be activated when Drag-and-Drop data is
		///   requested.</summary>
		/// <remarks>Songs may be copied by dragging them to
		///   Nautilus.</remarks>
		private void OnDragDataGet (object o, Gtk.DragDataGetArgs args)
		{
			GLib.List songs = base.List.SelectedHandles;

			switch (args.Info) {

			// Uri list
			case (uint) DndUtils.TargetType.UriList:
				ProcessDragDataGetUriList (songs, args);
				break;
			
			// Song list
			case (uint) DndUtils.TargetType.SongList:
				ProcessDragDataGetSongList (songs, args);
				break;

			// Default
			default:
				break;	
			}
		}


		// Delegate Functions
		// Delegate Functions :: SortFunc
		/// <summary>Delegate used in sorting the song list.</summary>		
		/// <param name="a_ptr">Handler for first
		///   <see cref="Song" />.</param>
		/// <param name="b_ptr">Handler for second
		///   <see cref="Song" />.</param>
		/// <returns>The result of comparing the songs with
		///   <see cref="Item.CompareTo" />.</returns>
		/// <seealso cref="Item.CompareTo" />
		private int SortFunc (IntPtr a_ptr, IntPtr b_ptr)
		{
			Song a = GetSong (a_ptr);
			Song b = GetSong (b_ptr);

			return a.CompareTo (b);
		}

		// Delegate Functions :: CellDataFunc
		/// <summary>Delegate used to render the song text.</summary>
		private void CellDataFunc
		  (Gtk.TreeViewColumn col, Gtk.CellRenderer cell, Gtk.TreeModel model,
		   Gtk.TreeIter iter)
		{
			Gtk.CellRendererText cell_txt = (Gtk.CellRendererText) cell;
			SetText (cell_txt, iter);
		}		
	}
}
