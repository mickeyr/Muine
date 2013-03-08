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

using System.Text.RegularExpressions;

using Gtk;

namespace Muine
{
	public static class DndUtils 
	{
		// Enums
		// Enums :: Drag-and-Drop TargetType
		public enum TargetType {
			UriList,
			Uri,
			SongList,
			AlbumList,
			ModelRow,
		};

		// Drag-and-Drop Targets
		public static readonly TargetEntry TargetUriList = 
			new TargetEntry
			  ("text/uri-list", 0,
			   (uint) TargetType.UriList);
			
		public static readonly TargetEntry TargetGnomeIconList = 
			new TargetEntry
			  ("x-special/gnome-icon-list", 0,
			   (uint) TargetType.UriList);
			
		public static readonly TargetEntry TargetNetscapeUrl = 
			new TargetEntry
			  ("_NETSCAPE_URL", 0,
			   (uint) TargetType.Uri);
			
		public static readonly TargetEntry TargetMuineAlbumList = 
			new TargetEntry
			  ("MUINE_ALBUM_LIST", TargetFlags.App,
			   (uint) TargetType.AlbumList);

		public static readonly TargetEntry TargetMuineSongList = 
			new TargetEntry
			  ("MUINE_SONG_LIST", TargetFlags.App,
			   (uint) TargetType.SongList);
			
		public static readonly TargetEntry TargetMuineTreeModelRow = 
			new TargetEntry
			  ("MUINE_TREE_MODEL_ROW", TargetFlags.Widget,
			   (uint) TargetType.ModelRow);

		// Methods
		// Methods :: Public
		// Methods :: Public :: SelectionDataToString
		/// <summary>
		///	Converts <see cref="Gtk.SelectionData" /> to a 
		/// 	<see cref="String" />.
		/// </summary>
		/// <remarks>
		///	Data in <see cref="Gtk.SelectionData" /> is held as an
		/// 	array of <see cref="Byte">bytes</see>. This function 
		///	just calls <see cref="System.Text.Encoding.UTF8.GetString" />
		///	on that array.
		/// </remarks>
		/// <param name="data">
		///	A <see cref="Gtk.SelectionData" /> object.
		/// </param>
		public static string SelectionDataToString (Gtk.SelectionData data)
		{
			return System.Text.Encoding.UTF8.GetString (data.Data);
		}

		// Methods :: Public :: SplitSelectionData
		/// <summary>
		///	Split <see cref="Gtk.SelectionData" /> data into an
		/// 	array of <see cref="String">strings</see>.
		/// </summary>
		/// <remarks>
		///	Data is separated by "\r\n" pairs.
		/// </remarks>
		/// <param name="data">
		///	A <see cref="Gtk.SelectionData" /> object.
		/// </param>
		public static string [] SplitSelectionData (Gtk.SelectionData data)
		{
			string s = SelectionDataToString (data);
			return SplitSelectionData (s);
		}

		/// <summary>
		///	Split <see cref="Gtk.SelectionData" /> data into an 
		///	array of <see cref="String">strings</see>.
		/// </summary>
		/// <remarks>
		///	Data is separated by "\r\n" pairs.
		/// </remarks>
		/// <param name="data">
		///	A <see cref="String" />.
		/// </param>
		public static string [] SplitSelectionData (string data)
		{
			return Regex.Split (data, "\r\n");
		}
	}
}
