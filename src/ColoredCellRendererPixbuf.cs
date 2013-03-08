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
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Runtime.InteropServices;

namespace Muine
{
	/// <summary>
	/// </summary>
	/// <remarks>
	///	This renderer is similar to <see cref="Gtk.CellRendererPixbuf" />.
	/// </remarks>
	public class ColoredCellRendererPixbuf : Gtk.CellRenderer 
	{
		// Constructor
		[DllImport ("libmuine")]
		private static extern IntPtr rb_cell_renderer_pixbuf_new ();

		/// <summary>
		///	Create a new <see cref="ColoredCellRendererPixbuf" />
		///	object.
		/// </summary>
		public ColoredCellRendererPixbuf () : base (IntPtr.Zero)
		{
			base.Raw = rb_cell_renderer_pixbuf_new ();
                        System.Console.WriteLine("hi");
		}

		// Destructor
		~ColoredCellRendererPixbuf ()
		{
			Dispose ();
		}

		// Properties
		// Properties :: Pixbuf (set;)
		/// <summary>
		///	The <see cref="Gdk.Pixbuf" /> to be used.
		/// </summary>
		/// <param name="value">
		///	A <see cref="Gdk.Pixbuf" />.
		/// </param>
		public Gdk.Pixbuf Pixbuf {
			set { SetProperty ("pixbuf", new GLib.Value (value)); }
		}
	}
}
