/*
 * Copyright (C) 2004 Sergio Rubio <sergio.rubio@hispalinux.es>
 *           (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General public virtual License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General public virtual License for more details.
 *
 * You should have received a copy of the GNU General public virtual
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
 */

using System;
using System.Collections;

using NDesk.DBus;

using Muine.PluginLib;

namespace Muine.DBusLib
{
	public class Player : Muine.DBusLib.IPlayer
	{
		public static Muine.DBusLib.IPlayer FindInstance ()
		{
			// XXX: Move these strings to someplace useful.
			if(!Bus.Session.NameHasOwner("org.gnome.Muine")) {
				return null;
			}
			ObjectPath opath = new ObjectPath ("/org/gnome/Muine/Player");
			
			return Bus.Session.GetObject<Muine.DBusLib.IPlayer> ("org.gnome.Muine", opath);
		}

		private Muine.PluginLib.IPlayer player = null;

		public void HookUp (Muine.PluginLib.IPlayer player)
		{
			this.player = player;
		
			player.SongChangedEvent  += OnSongChangedEvent ;
			player.StateChangedEvent += OnStateChangedEvent;
		}

		public virtual bool GetPlaying ()
		{
			return player.Playing;
		}
	
		public virtual void SetPlaying (bool playing)
		{
			player.Playing = playing;
		}

		public virtual bool HasNext ()
		{
			return player.HasNext;
		}

		public virtual void Next ()
		{
			player.Next ();
		}

		public virtual bool HasPrevious ()
		{
			return player.HasPrevious;
		}

		public virtual void Previous ()
		{
			player.Previous ();
		}

		public virtual string GetCurrentSong ()
		{
			string str = String.Empty;
		
			if (player.PlayingSong != null)
				str = SongToString (player.PlayingSong);
		
			return str;
		}

		public virtual bool GetWindowVisible ()
		{
			return player.WindowVisible;
		}
	
		public virtual void SetWindowVisible (bool visible, uint time)
		{
			player.SetWindowVisible (visible, time);
		}

		public virtual int GetVolume ()
		{
			return player.Volume;
		}

		public virtual void SetVolume (int volume)
		{
			player.Volume = volume;
		}

		public virtual int GetPosition ()
		{
			return player.Position;
		}

		public virtual void SetPosition (int pos)
		{
			player.Position = pos;
		}

		public virtual void PlayAlbum (uint time)
		{
			player.PlayAlbum (time);
		}

		public virtual void PlaySong (uint time)
		{
			player.PlaySong (time);
		}

		public virtual void OpenPlaylist (string uri)
		{
			player.OpenPlaylist (uri);
		}

		public virtual void PlayFile (string uri)
		{
			player.PlayFile (uri);
		}

		public virtual void QueueFile (string uri)
		{
			player.QueueFile (uri);
		}

		public virtual bool WriteAlbumCoverToFile (string file)
		{
			if (player.PlayingSong == null ||
			    player.PlayingSong.CoverImage == null)
				return false;
			
			try {
				player.PlayingSong.CoverImage.Save (file, "png");
			} catch {
				return false;
			}

			return true;
		}

		public virtual byte [] GetAlbumCover ()
		{
			if (player.PlayingSong == null ||
			    player.PlayingSong.CoverImage == null)
				return new byte[0];

			Gdk.Pixdata pixdata = new Gdk.Pixdata ();
			pixdata.FromPixbuf (player.PlayingSong.CoverImage, true);
			
			return pixdata.Serialize ();
		}

		public virtual void Quit ()
		{
			player.Quit ();
		}

		public event SongChangedHandler SongChanged;

		private void OnSongChangedEvent (ISong song)
		{
			string str = String.Empty;
		
			if (song != null)
				str = SongToString (song);

			if (SongChanged != null)
				SongChanged (str);
		}

		public event StateChangedHandler StateChanged;

		private void OnStateChangedEvent (bool playing)
		{
			if (StateChanged != null)
				StateChanged (playing);
		}

		private string SongToString (ISong song)
		{
			string nl = Environment.NewLine;

			string str = String.Empty;

			str += "uri: " + song.Filename + nl;
			str += "title: " + song.Title + nl;

			foreach (string s in song.Artists)
				str += "artist: " + s + nl;
			
			foreach (string s in song.Performers)
				str += "performer: " + s + nl;

			if (song.Album.Length > 0)
				str += "album: " + song.Album + nl;

			if (song.Year.Length > 0)
				str += "year: " + song.Year + nl;

			if (song.TrackNumber >= 0)
				str += "track_number: " + song.TrackNumber + nl;
			
			if (song.DiscNumber >= 0)
				str += "disc_number: " + song.DiscNumber + nl;

			str += "duration: " + song.Duration;

			return str;
		}
	}
}
