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
using System.Reflection;
using System.IO;

using Mono.Unix;

using Muine.PluginLib;

namespace Muine
{
	public class PluginManager
	{
		// Strings
		private static readonly string string_error_load =
			Catalog.GetString ("Error loading plug-in {0}: {1}");
	
		// Objects
		private IPlayer player;

		// Constructor
		public PluginManager (IPlayer player)
		{
			this.player = player;

			string path =
			  Environment.GetEnvironmentVariable ("MUINE_PLUGIN_PATH");

			if (path != null) {
				string [] path_parts = path.Split (':');
				foreach (string dir in path_parts)
					FindAssemblies (dir);
			}

			FindAssemblies (FileUtils.SystemPluginDirectory);
			FindAssemblies (FileUtils.UserPluginDirectory);
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: ScanAssemblyForPlugins
		private void ScanAssemblyForPlugins (Assembly a)
		{
			Type [] types = a.GetTypes ();
			foreach (Type t in types) {
				Type plugin_type = typeof (Plugin);
				bool is_plugin = t.IsSubclassOf (plugin_type);
				if (!is_plugin || t.IsAbstract)
					continue;

				Plugin plugin = (Plugin) Activator.CreateInstance (t);
				plugin.Initialize (player);
			}
		}

		// Methods :: Private :: FindAssemblies
		private void FindAssemblies (string dir)
		{
			if (dir == null || dir == String.Empty)
				return;

			DirectoryInfo info = new DirectoryInfo (dir);
			if (!info.Exists)
				return;

			foreach (FileInfo file in info.GetFiles ()) {
				if (file.Extension != ".dll")
					continue;

				try {
					Assembly a = Assembly.LoadFrom (file.FullName);
					ScanAssemblyForPlugins (a);

				} catch (Exception e) {
					Console.WriteLine
					  (string_error_load, file.Name, e.Message);
				}
			}
		}
	}
}
