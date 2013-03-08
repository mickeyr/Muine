/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jorn.baayen@gmail.com>
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

using Gtk;
using GLib;

using Mono.Unix;

namespace Muine
{
	public class ProgressWindow
	{
		// Strings
		private static readonly string string_title =
			Catalog.GetString ("Importing \"{0}\"");

		private static readonly string string_loading =
			Catalog.GetString ("Loading:");


		// Widgets
		[Glade.Widget] private Window window;
		[Glade.Widget] private Label  loading_label;
		[Glade.Widget] private Label  file_label;

		// Variables
		private bool canceled = false;
		private Gdk.Geometry geo_no_resize_height;

		// Constructor
		public ProgressWindow (Window parent)
		{
			Glade.XML gxml =
			  new Glade.XML (null, "ProgressWindow.glade", "window", null);

			gxml.Autoconnect (this);

			window.TransientFor = parent;

			window.SetDefaultSize (300, -1);

			string string_loading_esc =
			  StringUtils.EscapeForPango (string_loading);

			loading_label.Markup =
			  String.Format ("<b>{0}</b>", string_loading_esc);

			geo_no_resize_height = new Gdk.Geometry ();
			geo_no_resize_height.MaxWidth = Int32.MaxValue;
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Report
		public bool Report (string folder, string file)
		{
			if (canceled)
				return true;

			window.Title = String.Format (string_title, folder);

			if (file != null)
				file_label.Text = file;

			window.Visible = true;

			return false;
		}

		// Methods :: Public :: Done
		public void Done ()
		{
			window.Destroy ();
		}

		// Handlers
		// Handlers :: OnWindowResponse
		public void OnWindowResponse (object o, ResponseArgs a)
		{
			window.Visible = false;
			canceled = true;
		}

		// Handlers :: OnWindowDeleteEvent
		public void OnWindowDeleteEvent (object o, DeleteEventArgs args)
		{
			window.Visible = false;
			args.RetVal = true;
			canceled = true;
		}

		// Handlers :: OnWindowSizeRequested
		public void OnWindowSizeRequested (object o, SizeRequestedArgs args)
		{
			if (geo_no_resize_height.MaxHeight == args.Requisition.Height)
				return;

			geo_no_resize_height.MaxHeight = args.Requisition.Height;

			window.SetGeometryHints
			  (window, geo_no_resize_height, Gdk.WindowHints.MaxSize);
		}
	}
}
