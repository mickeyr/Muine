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

using Gtk;
using GLib;

namespace Muine
{
	public class FileSelector : FileChooserDialog
	{
		// Constants
		private const string GConfDefaultStartDir = "~";
		
		// Variables
		private string gconf_path;

		// Constructor
		/// <summary>
		///	Creates a new <see cref="FileSelector" />.
		/// </summary>
		/// <param name="title">
		///	The window title.
		/// </param>
		/// <param name="action">
		///	The <see cref="FileChooserAction" /> to do.
		/// </param>
		/// <param name="gconf_path">
		///	The GConf path which holds the starting directory.
		/// </param>
		public FileSelector
		  (string title, FileChooserAction action, string gconf_path) 
		  : base (title, Global.Playlist, action, "gnome-vfs")
		{
			this.gconf_path = gconf_path;

			base.LocalOnly = false;

			base.AddButton (Stock.Cancel, ResponseType.Cancel);

			switch (action) {
			case FileChooserAction.Open:
				base.AddButton (Stock.Open, ResponseType.Ok);
				break;
			
			case FileChooserAction.Save:
				base.AddButton (Stock.Save, ResponseType.Ok);
				break;
			
			default:
				break;
			}
			
			base.DefaultResponse = ResponseType.Ok;

			string start_dir =
			  (string) Config.Get (gconf_path, GConfDefaultStartDir);

			start_dir = start_dir.Replace ("~",
				Gnome.Vfs.Uri.GetUriFromLocalPath (FileUtils.HomeDirectory));

			SetCurrentFolderUri (start_dir);

			base.Response += OnResponse;
		}

		// Handlers
		// Handlers :: OnResponse
		/// <summary>
		///	Handler called when a response has been chosen.
		/// </summary>
		/// <remarks>
		///	If the response is "Ok", then save the current
		///	directory to GConf. Otherwise, just close
		/// </remarks>
		/// <param name="o">
		///	The calling object.
		/// </param>
		/// <param name="args">
		///	The <see cref="ResponseArgs" />.
		/// </param>
		private void OnResponse (object o, ResponseArgs args)
		{
			if (args.ResponseId != ResponseType.Ok)
				return;

			Config.Set (gconf_path, CurrentFolderUri);
		}
	}
}
