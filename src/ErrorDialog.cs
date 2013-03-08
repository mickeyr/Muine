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

using Gtk;
using GLib;

using Mono.Unix;

namespace Muine
{
	public class ErrorDialog
	{
		// Widgets
		private MessageDialog window;

		// Constructor
		//	TODO: Shouldn't the ErrorDialog be able to be
		//	associated with *any* window, not just the
		//	PlaylistWindow?
		/// <summary>
		///	Create a new <see cref="ErrorDialog" />.
		/// </summary>
		/// <param name="playlist">
		///	The <see cref="PlaylistWindow" /> which should be the
		/// 	parent of this window.
		/// </param>
		/// <param name="s1">
		///	The <see cref="String" /> to be used as the primary text.
		/// </param>
		/// <param name="s2">
		///	The <see cref="String" /> to be used as the secondary text.
		/// </param>
		public ErrorDialog (PlaylistWindow playlist, string s1, string s2)
		  : this (s1, s2)
		{
			window.TransientFor = playlist;
		}

		/// <summary>
		///	Create a new <see cref="ErrorDialog" />.
		/// </summary>
		/// <param name="s1">
		///	The <see cref="String" /> to be used as the primary text.
		/// </param>
		/// <param name="s2">
		///	The <see cref="String" /> to be used as the secondary text.
		/// </param>
		public ErrorDialog (string s1, string s2)
		{
			string s1_esc = StringUtils.EscapeForPango (s1);
			string s2_esc = StringUtils.EscapeForPango (s2);

			string markup = String.Format ("<span size=\"large\" weight=\"bold\">{0}</span>", s1_esc);

			window = new Gtk.MessageDialog (null, DialogFlags.DestroyWithParent,
							MessageType.Error, ButtonsType.Close, true, markup);

			window.SecondaryText = s2_esc;
			window.Title = Catalog.GetString("Error");

			window.Run ();
			window.Destroy ();
		}		
	}
}
