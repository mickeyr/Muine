/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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

namespace Muine.PluginLib
{
	public interface IPlayer
	{
		ISong PlayingSong {
			get;
		}

		bool Playing {
			get;
			set;
		}

		int Volume {
			get;
			set;
		}
	
		int Position {
			get;
			set;
		}
	
		bool HasNext {
			get;
		}
	
		bool HasPrevious {
			get;
		}
	
		void Next ();
		void Previous ();

		void PlaySong (uint time);
		void PlayAlbum (uint time);

		ISong [] Playlist {
			get;
		}

		ISong [] Selection {
			get;
		}

		ISong [] AllSongs {
			get;
		}

		void OpenPlaylist (string uri);

		void PlayFile (string uri);
		void QueueFile (string uri);

		void Quit ();

		bool WindowVisible {
			get;
		}

		void SetWindowVisible (bool visible, uint time);
	
		Gtk.UIManager UIManager {
			get;
		}

		void PackWidget (Gtk.Widget widget);

		Gtk.Window Window {
			get;
		}

		uint BusyLevel {
			set;
			get;
		}

		string [] WatchedFolders {
			set;
			get;
		}

		void AddFolder (string folder);
		void RemoveFolder (string folder);

		ISong AddSong (string path);
		void SyncSong (string path);
		void SyncSong (ISong song);
		void RemoveSong (string path);
		void RemoveSong (ISong song);

		event SongChangedEventHandler SongChangedEvent;
	
		event StateChangedEventHandler StateChangedEvent;

		event TickEventHandler TickEvent;

		event GenericEventHandler PlaylistChangedEvent;

		event GenericEventHandler SelectionChangedEvent;

		event GenericEventHandler WatchedFoldersChangedEvent;
	}

	public delegate void SongChangedEventHandler (ISong song);
	public delegate void StateChangedEventHandler (bool playing);
	public delegate void TickEventHandler (int position);
	public delegate void GenericEventHandler ();
}
