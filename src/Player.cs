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
using System.Runtime.InteropServices;

using Mono.Unix;

namespace Muine
{
	public class PlayerException : Exception
	{
		public PlayerException (IntPtr p) 
		: base (GLib.Marshaller.PtrToStringGFree (p)) 
		{
		}
	}

	public class Player : GLib.Object
	{
		// Strings
		private static readonly string string_audio_error =
			Catalog.GetString ("Audio backend error:");

		// Events
		public delegate void StateChangedHandler (bool playing);
		public event         StateChangedHandler StateChanged;

		public delegate void TickEventHandler (int pos);
		public event         TickEventHandler TickEvent;

		public delegate void EndOfStreamEventHandler ();
		public event         EndOfStreamEventHandler EndOfStreamEvent;

		public delegate void InvalidSongHandler (Song song);
		public event         InvalidSongHandler InvalidSong;

		// Callbacks
		private SignalUtils.SignalDelegateInt tick_cb ;
		private SignalUtils.SignalDelegate    eos_cb  ;
		private SignalUtils.SignalDelegateStr error_cb;

		// Objects
		private Song song = null;
		
		// Variables
		private bool stopped = true;
		private bool playing;
		private uint set_file_idle_id = 0;

		// Constructor
		[DllImport ("libmuine")]
		private static extern IntPtr player_new (out IntPtr error_ptr);

		public Player () : base (IntPtr.Zero)
		{
			IntPtr error_ptr;
			
			Raw = player_new (out error_ptr);

			if (error_ptr != IntPtr.Zero)
				throw new PlayerException (error_ptr);
			
			tick_cb  = new SignalUtils.SignalDelegateInt (OnTick       );
			eos_cb   = new SignalUtils.SignalDelegate    (OnEndOfStream);
			error_cb = new SignalUtils.SignalDelegateStr (OnError      );

			SignalUtils.SignalConnect (Raw, "tick"         , tick_cb );
			SignalUtils.SignalConnect (Raw, "end_of_stream", eos_cb  );
			SignalUtils.SignalConnect (Raw, "error"        , error_cb);

			playing = false;
			song = null;
		}

		// Destructor
		~Player ()
		{
			Dispose ();
		}

		// Properties
		// Properties :: Song (set; get;)
		[DllImport ("libmuine")]
		private static extern bool player_set_file
		  (IntPtr player, string filename, out IntPtr error_ptr);

		[DllImport ("libmuine")]
		private static extern void player_set_replaygain
		  (IntPtr player, double gain, double peak);

		public Song Song {
			set {
				stopped = false;
				
				song = value;

				if (playing)
					player_pause (Raw);

				if (set_file_idle_id > 0)
					GLib.Source.Remove (set_file_idle_id);

				GLib.IdleHandler func =
				  new GLib.IdleHandler (SetFileIdleFunc);

				set_file_idle_id = GLib.Idle.Add (func);
			}

			get { return song; }
		}

		// Properties :: Playing (get;)
		public bool Playing {
			get { return playing; }
		}

		// Properties :: Position (set; get;)
		[DllImport ("libmuine")]
		private static extern void player_seek (IntPtr player, int t);

		[DllImport ("libmuine")]
		private static extern int player_tell (IntPtr player);

		public int Position {
			set {
				player_seek (Raw, value);

				if (TickEvent != null)
					TickEvent (value);
			}

			get { return (set_file_idle_id > 0) ? 0 : player_tell (Raw); }
		}

		// Properties :: Volume (set; get;)
		[DllImport ("libmuine")]
		private static extern void player_set_volume
		  (IntPtr player, int volume);

		[DllImport ("libmuine")]
		private static extern int player_get_volume (IntPtr player);

		public int Volume {
			set { player_set_volume (Raw, value); }
			get { return player_get_volume (Raw); }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Play
		[DllImport ("libmuine")]
		private static extern void player_play (IntPtr player);

		public void Play ()
		{
			if (playing)
				return;
					
			playing = true;

			if (set_file_idle_id == 0)
				player_play (Raw);

			if (StateChanged != null)
				StateChanged (playing);
		}

		// Methods :: Public :: Pause
		[DllImport ("libmuine")]
		private static extern void player_pause (IntPtr player);

		public void Pause ()
		{
			if (!playing)
				return;
				
			playing = false;
			
			player_pause (Raw);

			if (StateChanged != null)
				StateChanged (playing);
		}

		// Methods :: Public :: Stop
		[DllImport ("libmuine")]
		private static extern void player_stop (IntPtr player);

		public void Stop ()
		{
			if (stopped)
				return;
				
			player_stop (Raw);
			stopped = true;

			if (!playing)
				return;

			playing = false;

			if (StateChanged != null)
				StateChanged (playing);
		}
		
		// Methods :: Private
		// Methods :: Private :: SetFileIdleFunc
		private bool SetFileIdleFunc ()
		{
			set_file_idle_id = 0;

			IntPtr error_ptr;

			player_set_file (Raw, song.Filename, out error_ptr);

			if (error_ptr != IntPtr.Zero) {
				if (InvalidSong != null)
					InvalidSong (song);

				return false;
			}
				
			player_set_replaygain (Raw, song.Gain, song.Peak);

			if (playing)
				player_play (Raw);

			if (TickEvent != null)
				TickEvent (0);

			return false;
		}

		// Handlers
		// Handlers :: OnTick
		private void OnTick (IntPtr obj, int pos)
		{	
			if (TickEvent != null)
				TickEvent (pos);
		}

		// Handlers :: OnEndOfStream
		private void OnEndOfStream (IntPtr obj)
		{
			if (EndOfStreamEvent != null)
				EndOfStreamEvent ();
		}

		// Handlers :: OnError
		private void OnError (IntPtr obj, string error)
		{
			new ErrorDialog (Global.Playlist, string_audio_error, error);
		}
	}
}
