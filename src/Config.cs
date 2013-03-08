/*
 * Copyright (C) 2005 Jorn Baayen <jorn.baayen@gmail.com>
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

namespace Muine
{
	// TODO: Can we make this inherit from GConf.Client?
	//	That would make this class nearly empty.
	public static class Config 
	{
		// Variables
		private static GConf.Client gconf_client;
		
		// Methods
		// Methods :: Public
		// Methods :: Public :: Init
		/// <summary>
		///	Initialize the Configuration system.
		/// </summary>
		public static void Init ()
		{
			gconf_client = new GConf.Client ();
		}

		// Methods :: Public :: Get
		/// <summary>
		///	Get the value of a key in GConf.
		/// </summary>
		/// <param name="key">
		///	The key.
		/// </param>
		/// <returns>
		///	The value located at <paramref name="key" /> (boxed).
		/// </returns>
		public static object Get (string key)
		{
			return gconf_client.Get (key);
		}
		
		/// <summary>
		///	Get the value of a key in GConf with default.
		/// </summary>
		/// <param name="key">
		///	The key.
		/// </param>
		/// <param name="default_val">
		///	Value to return if lookup fails.
		/// </param>
		/// <returns>
		///	The value located at <paramref name="key" /> (boxed).
		///	If that fails, <paramref name="default_val" /> is returned.
		/// </returns>
		public static object Get (string key, object default_val)
		{
			object val;

			try {
				val = Get (key);
			} catch {
				val = default_val;
			}

			return val;
		}

		// Methods :: Public :: Set
		/// <summary>
		///	Set a value in GConf.
		/// </summary>
		/// <param name="key">
		///	The key.
		/// </param>
		/// <param name="val">
		///	The value.
		/// </param>
		public static void Set (string key, object val)
		{
			gconf_client.Set (key, val);        	
		}

		// Methods :: Public :: AddNotify
		/// <summary>
		/// 	Add a <see cref="GConf.NotifyEventHandler" /> to a key.
		/// </summary>
		/// <param name="key">
		///	The key.
		/// </param>
		/// <param name="notify">
		///	The <see cref="GConf.NotifyEventHandler" />.
		/// </param>
		public static void AddNotify
		  (string key, GConf.NotifyEventHandler notify)
		{
			gconf_client.AddNotify (key, notify);
		}
	}
}
