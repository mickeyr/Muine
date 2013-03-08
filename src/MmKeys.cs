/*
 * Copyright (C) 2004 Lee Willis <lee@leewillis.co.uk>
 *           (C) 2004, 2005 Jorn Baayen <jorn.baayen@gmail.com>
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
using System.Runtime.InteropServices;

using Gtk;
using GLib;

using Muine.PluginLib;

namespace Muine
{
	public class MmKeys : GLib.Object
	{
		// Delegates
		private SignalUtils.SignalDelegate toggle_play_cb;
		private SignalUtils.SignalDelegate prev_cb;
		private SignalUtils.SignalDelegate next_cb;
		private SignalUtils.SignalDelegate stop_cb;

		// Objects
		private IPlayer player;

		// Constructor
		[DllImport ("libmuine")]
		private static extern IntPtr mmkeys_new ();

		public MmKeys (IPlayer player) : base (IntPtr.Zero)
		{
			base.Raw = mmkeys_new ();

			this.player = player;

			toggle_play_cb = new SignalUtils.SignalDelegate (OnTogglePlay);
			prev_cb        = new SignalUtils.SignalDelegate (OnPrev      );
			next_cb        = new SignalUtils.SignalDelegate (OnNext      );
			stop_cb        = new SignalUtils.SignalDelegate (OnStop      );

			SignalUtils.SignalConnect (base.Raw, "mm_playpause", toggle_play_cb);
			SignalUtils.SignalConnect (base.Raw, "mm_prev"     , prev_cb       );
			SignalUtils.SignalConnect (base.Raw, "mm_next"     , next_cb       );
			SignalUtils.SignalConnect (base.Raw, "mm_stop"     , stop_cb       );
		}

		// Destructor
		~MmKeys ()
		{
			Dispose ();
		}

		// Handlers
		// Handlers :: OnTogglePlay
		private void OnTogglePlay (IntPtr obj)
		{
			player.Playing = !player.Playing;
		}

		// Handlers :: OnNext
		private void OnNext (IntPtr obj)
		{
			player.Next ();
		}
	
		// Handlers :: OnPrev
		private void OnPrev (IntPtr obj)
		{
			player.Previous ();
		}

		// Handlers :: OnStop
		private void OnStop (IntPtr obj)
		{
			player.Playing = false;
		}
	}
}
