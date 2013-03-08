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
using System.Collections;
using System.IO;
using Gtk;
using Mono.Unix;

namespace Muine
{
	public class ImportDialog : FileChooserDialog
	{	
		// GConf
		private const string GConfKeyImportFolder     = "/apps/muine/default_import_folder";
		private const string GConfDefaultImportFolder = "~";

		// Strings		
		private static readonly string string_title =
			Catalog.GetString ("Import Folder");

		private static readonly string string_button =
			Catalog.GetString ("_Import");

		// Constructor
		public ImportDialog ()
		  : base (string_title, Global.Playlist,
		    FileChooserAction.SelectFolder)
		{
			base.LocalOnly      = true;
			base.SelectMultiple = true;

			base.AddButton (Stock.Cancel , ResponseType.Cancel);
			base.AddButton (string_button, ResponseType.Ok    );

			base.DefaultResponse = ResponseType.Ok;

			base.Response += OnResponse;

			// Load Start Directory
			string start_dir = (string) Config.Get (GConfKeyImportFolder, 
				GConfDefaultImportFolder);

			start_dir = start_dir.Replace ("~", FileUtils.HomeDirectory);
			base.SetCurrentFolder (start_dir);

			// Show
			base.Visible = true;
		}

		// Handlers
		// Handlers :: OnResponse
		private void OnResponse (object o, ResponseArgs args)
		{
			// If response wasn't "Ok", do nothing
			if (args.ResponseId != ResponseType.Ok) {
				base.Destroy ();
				return;
			}

			// Save Start Directory
			Config.Set (GConfKeyImportFolder, base.CurrentFolder);

			// Check that Directories exist
			ArrayList new_dinfos = new ArrayList ();

			foreach (string dir in base.Filenames) {
				DirectoryInfo dinfo = new DirectoryInfo (dir);
				
				if (!dinfo.Exists)
					continue;

				new_dinfos.Add (dinfo);
			}

			// Check if we have any Directories to add
			if (new_dinfos.Count > 0)
				Global.DB.AddWatchedFolders (new_dinfos);

			base.Destroy ();
		}
	}
}
