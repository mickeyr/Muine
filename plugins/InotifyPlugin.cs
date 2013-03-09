
using System;
using System.Collections;
using System.IO;
using Muine.PluginLib;
using Gtk;

namespace Muine
{
	public class InotifyPlugin : Plugin
	{
		private IPlayer player;
		private ThreadNotify notify;

		private ArrayList foldersToAdd = new ArrayList ();
		private ArrayList foldersToRemove = new ArrayList ();
		private ArrayList filesToAdd = new ArrayList ();
		private ArrayList filesToRemove = new ArrayList ();

		public override void Initialize (IPlayer player)
		{
			if (!Inotify.Enabled)
				return;

			this.player = player;

			notify = new ThreadNotify (new ReadyEvent (OnNotify));

			foreach (string dir in player.WatchedFolders)
				Watch (dir);

			player.WatchedFoldersChangedEvent += OnFoldersChanged;
			Inotify.Start ();
		}

		private void OnFoldersChanged ()
		{
			foreach (string dir in player.WatchedFolders)
				Watch (dir);
		}

		private void OnNotify ()
		{
			lock (this)
			{
				foreach (string folder in foldersToAdd)
				{
					//Console.WriteLine ("Adding folder: " + folder);
					Watch (folder);
					player.AddFolder (folder);
				}
				
				foreach (string folder in foldersToRemove)
				{
					//Console.WriteLine ("Removing folder: " + folder);
					player.RemoveFolder (folder);
				}
				
				foreach (string file in filesToAdd) player.AddSong (file);
				
				foreach (string file in filesToRemove) player.RemoveSong (file);
				
				foldersToAdd.Clear ();
				foldersToRemove.Clear ();
				filesToAdd.Clear ();
				filesToRemove.Clear ();
			}
		}

		private bool HasFlag (Inotify.EventType type, Inotify.EventType value)
		{
			return ((type & value) == value);
		}

		private void OnInotifyEvent (Inotify.Watch watch, string path, string subitem,
					     string srcpath, Inotify.EventType type)
		{
			/*
			Console.WriteLine ("Got event ({03}) {0}: {1}/{2}", type, path, subitem,
							   srcpath);
			*/

			string fullPath = Path.Combine (path, subitem);

			lock (this)
			{
				if (HasFlag (type, Inotify.EventType.MovedTo) ||
				    HasFlag (type, Inotify.EventType.CloseWrite))
				{
					if (HasFlag (type, Inotify.EventType.IsDirectory) &&
					    !HasFlag (type, Inotify.EventType.CloseWrite))
					{
						foldersToAdd.Add (fullPath);
						
						if (srcpath != null)
							foldersToRemove.Add (srcpath);
					}
					else
					{
						filesToAdd.Add (fullPath);
						
						if (srcpath != null)
							filesToRemove.Add (srcpath);
					}
					
				}
				else if (HasFlag (type, Inotify.EventType.Create) &&
					 HasFlag (type, Inotify.EventType.IsDirectory))
				{
					Watch (fullPath);
				}
				else if (HasFlag (type, Inotify.EventType.Delete) ||
					 HasFlag (type, Inotify.EventType.MovedFrom))
				{
					if (HasFlag (type, Inotify.EventType.IsDirectory))
						foldersToRemove.Add (fullPath);
					else
						filesToRemove.Add (fullPath);
				}
				
				notify.WakeupMain ();
			}
		}


		private void Watch (string folder)
		{
			/* Do not monitor non existing directories */
			if (!Directory.Exists (folder))
				return;

			/* Do not monitor lost+found directories, since these
			 * are not accesible to mortal users. */
			if (folder.EndsWith("lost+found"))
				return;

			/* Do not monitor trash folders */
			if (folder.EndsWith(".Trash"))
				return;

			/* Do not monitor per-user trash folders, eg. on removable
			 * devices. These are * named ".Trash-username" */
			if (Path.GetFileName(folder).StartsWith(".Trash-"))
				return;

			Inotify.Subscribe (folder, new Inotify.InotifyCallback (OnInotifyEvent),
					   Inotify.EventType.CloseWrite | Inotify.EventType.Delete |
					   Inotify.EventType.Create | Inotify.EventType.MovedFrom |
					   Inotify.EventType.MovedTo);

			foreach (string dir in Directory.GetDirectories (folder)) {
				Watch (dir);
			}
		}
	}
}
