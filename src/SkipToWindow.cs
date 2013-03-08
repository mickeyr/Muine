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

using Muine.PluginLib;

namespace Muine
{
	public class SkipToWindow
	{
		// Widgets
		[Glade.Widget] private Window window;
		[Glade.Widget] private HScale song_slider;
		[Glade.Widget] private Label  song_position;

		// Objects
		private IPlayer player;

		// Variables
		private bool from_tick;
		private const uint set_position_timeout = 100;
		private uint set_position_timeout_id;
		private Gdk.Geometry geo_no_resize_height;
		
		// Constructor
		public SkipToWindow (IPlayer p)
		{
			Glade.XML gxml = new Glade.XML (null, "SkipToWindow.glade", "window", null);
			gxml.Autoconnect (this);

			window.TransientFor = p.Window;

			geo_no_resize_height = new Gdk.Geometry ();
			geo_no_resize_height.MaxWidth = Int32.MaxValue;

			player = p;
			player.TickEvent += OnTickEvent;

			OnTickEvent (player.Position);
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Run
		public void Run ()
		{
			window.Visible = true;
			song_slider.GrabFocus ();
		}

		// Methods :: Public :: Hide
		public void Hide ()
		{
			window.Visible = false;
		}

		// Methods :: Private
		// Methods :: Private :: SetPositionTimeoutFunc
		private bool SetPositionTimeoutFunc ()
		{
			set_position_timeout_id = 0;
			player.Position = (int) song_slider.Value;
			
			return false;
		}

		// Methods :: Private :: UpdateLabel
		private void UpdateLabel (int pos)
		{
			string position =
			  StringUtils.SecondsToString (pos);

			string total_time =
			  StringUtils.SecondsToString (player.PlayingSong.Duration);

			song_position.Text =
			  String.Format ("{0} / {1}", position, total_time);
		}

		// Handlers
		// Handlers :: OnTickEvent
		private void OnTickEvent (int pos) 
		{
			if (set_position_timeout_id > 0)
				return;

			UpdateLabel (pos);

			// Update slider
			from_tick = true;
			song_slider.SetRange (0, player.PlayingSong.Duration);

			if (pos > player.PlayingSong.Duration)
				return;

			song_slider.Value = pos; 
		}

		// Handlers :: OnSongSliderValueChanged (Glade)
		public void OnSongSliderValueChanged (object o, EventArgs a) 
		{
			if (from_tick) {
				from_tick = false;
				return;
			}

			if (set_position_timeout_id > 0)
				GLib.Source.Remove (set_position_timeout_id);

			GLib.TimeoutHandler func =
			  new GLib.TimeoutHandler (SetPositionTimeoutFunc);

			set_position_timeout_id =
			  GLib.Timeout.Add (set_position_timeout, func);

			UpdateLabel ((int) song_slider.Value);
		}
		
		// Handlers :: OnSongSliderKeyPressEvent
		[GLib.ConnectBefore]
		public void OnSongSliderKeyPressEvent (object obj, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.Left:
				Global.Playlist.SkipBackwards ();
				args.RetVal = true;
				break;
			case Gdk.Key.Right:
				Global.Playlist.SkipForward ();
				args.RetVal = true;
				break;

			default:
				break;
			}
		}

		// Handlers :: OnScrollEvent
		[GLib.ConnectBefore]
			private void OnScrollEvent(object o, ScrollEventArgs args)
		{
			if(args.Event.Direction == Gdk.ScrollDirection.Up) {
				Global.Playlist.SkipBackwards ();
			} else if(args.Event.Direction == Gdk.ScrollDirection.Down) {
				Global.Playlist.SkipForward ();
			}
			args.RetVal = true;
		}

		// Handlers :: OnWindowDeleteEvent
		public void OnWindowDeleteEvent (object o, DeleteEventArgs a)
		{
			window.Visible = false;			
			DeleteEventArgs args = (DeleteEventArgs) a;
			args.RetVal = true;
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

		// Handlers :: OnCloseButtonClicked
		public void OnCloseButtonClicked (object o, EventArgs a)
		{
			window.Visible = false;
		}
	}
}
