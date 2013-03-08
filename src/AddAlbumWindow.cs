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
	public class AddAlbumWindow : AddWindow
	{
		// GConf
		// GConf :: Width
		private const string GConfKeyWidth = "/apps/muine/add_album_window/width";
		private const int GConfDefaultWidth = 500;

		// GConf :: Height
		private const string GConfKeyHeight = "/apps/muine/add_album_window/height";
		private const int GConfDefaultHeight = 475; 


		// Strings
		private static readonly string string_title = 
			Catalog.GetString ("Play Album");

		private static readonly string string_artists = 
			Catalog.GetString ("Performed by {0}");


		// Static
		// Static :: Objects
		// Static :: Objects :: DnD Targets
		private static Gtk.TargetEntry [] source_entries = {
			DndUtils.TargetMuineAlbumList,
			DndUtils.TargetUriList
		};


		// Widgets
		private Gtk.CellRenderer pixbuf_renderer =
		  new Gtk.CellRendererPixbuf ();

		private Gdk.Pixbuf nothing_pixbuf =
		  new Gdk.Pixbuf (null, "muine-nothing.png");


		// Variables
		private bool drag_dest_enabled = false;
		private int pixbuf_column_width = CoverDatabase.CoverSize + (5 * 2);


		// Constructor
		/// <summary>Creates a new Add Album window.</summary>
		/// <remarks>This is created when "Play Album" is clicked.</remarks>
		public AddAlbumWindow ()
		{
			base.Title = string_title;

			base.SetGConfSize
			  (GConfKeyWidth , GConfDefaultWidth,
			   GConfKeyHeight, GConfDefaultHeight);

			base.Items = Global.DB.Albums.Values;
						
			base.List.Model.SortFunc = new HandleModel.CompareFunc (SortFunc);

			// Column
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			col.Sizing = Gtk.TreeViewColumnSizing.Fixed;
			col.Spacing = 4;

			col.PackStart (pixbuf_renderer  , false);
			col.PackStart (base.TextRenderer, true );

			// Data functions
			Gtk.TreeCellDataFunc func;
			
			func = new Gtk.TreeCellDataFunc (PixbufCellDataFunc);
			col.SetCellDataFunc (pixbuf_renderer, func);

			func = new Gtk.TreeCellDataFunc (TextCellDataFunc);			
			col.SetCellDataFunc (base.TextRenderer, func);

			// Add column
			base.List.AppendColumn (col);

			// Setup drag and drop
			base.List.DragSource = source_entries;
			base.List.DragDataGet += OnDragDataGet;

			// Setup handlers
			Global.DB.AlbumAdded   += base.OnAdded;
			Global.DB.AlbumChanged += base.OnChanged;
			Global.DB.AlbumRemoved += base.OnRemoved;

			Global.CoverDB.DoneLoading += OnCoversDoneLoading;

			// Enable drag and drop if we're not busy loading covers.
			if (!Global.CoverDB.Loading)
				EnableDragDest ();
		}


		// Methods
		// Methods :: Private
		// Methods :: Private :: EnableDragDest
		/// <summary>Turns on Drag-and-Drop.</summary>
		private void EnableDragDest ()
		{
			// Return if already enabled
			if (drag_dest_enabled)
				return;

			// Add handler
			base.List.DragDataReceived += OnDragDataReceived;

			// Gtk settings
			Gtk.Drag.DestSet
			  (base.List, Gtk.DestDefaults.All, CoverImage.DragEntries,
			   Gdk.DragAction.Copy);

			// Mark as enabled
			drag_dest_enabled = true;
		}

		// Methods :: Private :: GetAlbum
		private Album GetAlbum (Gtk.TreeIter iter)
		{
			IntPtr ptr = base.List.Model.HandleFromIter (iter);
			return GetAlbum (ptr);
		}
		
		private Album GetAlbum (int x, int y)
		{
			Gtk.TreePath path = null;
			base.List.GetPathAtPos (x, y, out path);

			if (path == null)
				return null;
		
			IntPtr ptr = base.List.Model.HandleFromPath (path);
			return GetAlbum (ptr);
		}

		private Album GetAlbum (int ptr_i)
		{
			IntPtr ptr = new IntPtr (ptr_i);
			return GetAlbum (ptr);
		}

		private Album GetAlbum (IntPtr ptr)
		{
			return Album.FromHandle (ptr);
		}

		// Methods :: Private :: GetAlbumName
		private string GetAlbumName (Album album)
		{
			return StringUtils.EscapeForPango (album.Name);			
		}

		// Methods :: Private :: GetArtists
		private string GetArtists (Album album)
		{
			string tmp = StringUtils.JoinHumanReadable (album.Artists, 3);
			return StringUtils.EscapeForPango (tmp);
		}

		// Methods :: Private :: GetCoverImage
		private Gdk.Pixbuf GetCoverImage (Album album)
		{
			if (album.CoverImage != null)
				return album.CoverImage;

			if (Global.CoverDB.Loading)
				return Global.CoverDB.DownloadingPixbuf;

			return nothing_pixbuf;			
		}

		// Methods :: Private :: GetPerformers
		private string GetPerformers (Album album)
		{			
			if (album.Performers.Length < 1)
				return String.Empty;
			
			string tmp = StringUtils.JoinHumanReadable (album.Performers, 2);
			string tmp2 = String.Format (string_artists, tmp);
			return StringUtils.EscapeForPango (tmp2);
		}

		// Methods :: Private :: ProcessDragDataGetUriList
		private void ProcessDragDataGetUriList
		  (GLib.List albums, Gtk.DragDataGetArgs args)
		{
			string data = String.Empty;

			foreach (int album_ptr_i in albums) {
				Album album = GetAlbum (album_ptr_i);

				foreach (Song song in album.Songs) {
					string uri = Gnome.Vfs.Uri.GetUriFromLocalPath (song.Filename);
					data += (uri + "\r\n");
				}
			}

			SetSelectionData (DndUtils.TargetUriList.Target, data, args);
		}

		// Methods :: Private :: ProcessDragDataGetAlbumList
		private void ProcessDragDataGetAlbumList
		  (GLib.List albums, Gtk.DragDataGetArgs args)
		{
			string target = DndUtils.TargetMuineAlbumList.Target;
		
			string data = String.Format ("\t{0}\t", target);
			
			foreach (int ptr_i in albums) {
				IntPtr ptr = new IntPtr (ptr_i);
				string ptr_s = ptr.ToString ();
				data += (ptr_s + "\r\n");
			}
			
			SetSelectionData (target, data, args);
		}

		// Methods :: Private :: SetCoverImage
		private void SetCoverImage
		  (Gtk.CellRendererPixbuf cell, Gtk.TreeIter iter)
		{			
			Album album = GetAlbum (iter);
			cell.Pixbuf = GetCoverImage (album);
			cell.Width = cell.Height = pixbuf_column_width;
		}

		// Methods :: Private :: SetText
		private void SetText (Gtk.CellRendererText cell, Gtk.TreeIter iter)
		{			
			// Album
			Album album = GetAlbum (iter);
			
			// Info
			string name       = GetAlbumName  (album);
			string performers = GetPerformers (album);
			string artists    = GetArtists    (album);

			// Format
			string markup = String.Format ("<b>{0}</b>", name);
			markup += Environment.NewLine;
			markup += artists;
			markup += Environment.NewLine;
			markup += Environment.NewLine;
			markup += performers;
			cell.Markup = markup;
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
		// Handlers :: OnCoversDoneLoading
		/// <summary>Handler called when the album covers are done
		///   loading.</summary>
		/// <remarks>Enables Drag-and-Drop and redraws the list.</remarks>
		private void OnCoversDoneLoading ()
		{
			EnableDragDest ();
			base.List.QueueDraw ();
		}

		// Handlers :: OnDragDataGet
		/// <summary>Handler to be activated when Drag-and-Drop data is
		///   requested.</summary>
		/// <remarks>Albums may be copied by dragging them to
		///   Nautilus.</remarks>
		private void OnDragDataGet
		  (object o, Gtk.DragDataGetArgs args)
		{
			GLib.List albums = base.List.SelectedHandles;

			switch (args.Info) {

			// Uri list
			case (uint) DndUtils.TargetType.UriList:
				ProcessDragDataGetUriList (albums, args);
				break;

			// Album list
			case (uint) DndUtils.TargetType.AlbumList:
				ProcessDragDataGetAlbumList (albums, args);
				break;

			// Default
			default:
				break;	
			}
		}

		// Handlers :: OnDragDataReceived
		/// <summary>Handler called when Drag-and-Drop data is
		///   received.</summary>
		/// <remarks>External covers may be Drag-and-Dropped onto an
		///   album.</remarks>
		private void OnDragDataReceived
		  (object o, Gtk.DragDataReceivedArgs args)
		{
			Album album = GetAlbum (args.X, args.Y);
			Song song = (Song) album.Songs [0];
			CoverImage.HandleDrop (song, args);
		}


		// Delegate Functions		
		// Delegate Functions :: SortFunc
		/// <summary>Delegate used in sorting the album list.</summary>
		/// <param name="a_ptr">Handler for first
		///   <see cref="Album" />.</param>
		/// <param name="b_ptr">Handler for second
		///   <see cref="Album" />.</param>
		/// <returns>The result of comparing the albums with
		///   <see cref="Item.CompareTo" />.</returns>
		/// <seealso cref="Item.CompareTo" />
		private int SortFunc (IntPtr a_ptr, IntPtr b_ptr)
		{
			Album a = GetAlbum (a_ptr);
			Album b = GetAlbum (b_ptr);

			return a.CompareTo (b);
		}

		// Delegate Functions :: Data Function
		// Delegate Functions :: Data Function :: PixbufCellDataFunc
		/// <summary>Delegate used to render the covers.</summary>
		private void PixbufCellDataFunc
		  (Gtk.TreeViewColumn col, Gtk.CellRenderer cell, Gtk.TreeModel model,
		   Gtk.TreeIter iter)
		{
			Gtk.CellRendererPixbuf cell_pb = (Gtk.CellRendererPixbuf) cell;
			SetCoverImage (cell_pb, iter);
		}
	

		// Delegate Functions :: Data Function :: TextCellDataFunc
		/// <summary>Delegate used to render the album text.</summary>		
		private void TextCellDataFunc
		  (Gtk.TreeViewColumn col, Gtk.CellRenderer cell, Gtk.TreeModel model,
		   Gtk.TreeIter iter)
		{
			Gtk.CellRendererText cell_txt = (Gtk.CellRendererText) cell;
			SetText (cell_txt, iter);
		}		
	}
}
