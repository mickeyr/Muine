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
	public class SaveDialog : FileSelector
	{	
		// GConf
		private const string GConfKeyDefaultPlaylistFolder = 
			"/apps/muine/default_playlist_folder";
		
		// Strings
		private static readonly string string_title =
			Catalog.GetString ("Save Playlist");

		private static readonly string string_save_default =
			Catalog.GetString ("Untitled");

		// Constructor
		public SaveDialog ()
		  : base (string_title, FileChooserAction.Save,
		    GConfKeyDefaultPlaylistFolder)
		{
			base.CurrentName = string_save_default;
			base.Response += OnResponse;
			base.Visible = true;
		}

		// Handlers
		// Handlers :: OnResponse
		private void OnResponse (object o, ResponseArgs args)
		{
			if (args.ResponseId != ResponseType.Ok) {
				base.Destroy ();
				return;
			}

			string fn = base.Uri;

			// make sure the extension is ".m3u"
			if (!FileUtils.IsPlaylist (fn))
				fn += ".m3u";

			if (FileUtils.Exists (fn)) {
				OverwriteDialog d = new OverwriteDialog (this, fn);
				bool overwrite = d.GetAnswer ();

				if (!overwrite)
					return;
			}
			
			base.Destroy ();

			Global.Playlist.SavePlaylist (fn, false, false);
		}
	}
}
