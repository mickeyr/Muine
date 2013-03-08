/*
 * Copyright (C) 2004 Sergio Rubio <sergio.rubio@hispalinux.es>
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

// TODO: Can DBusService inherit from DBus.Service?

using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace Muine
{
	public sealed class DBusService
	{
		// Static
		// Static :: Objects
		private static DBusService instance;

		// Static :: Properties
		// Static :: Properties :: Instance (get;)
		/// <summary>
		///	Retreives a new or existing <see cref="DBusService" />.
		/// </summary>
		/// <remarks>
		///	This only allows a single <see cref="DBusService" /> 
		///	to exist for Muine.
		/// </remarks>
		/// <returns>
		///	A <see cref="DBusService" />.
		/// </returns>
		public static DBusService Instance {
			get {
				if (instance == null)
					instance = new DBusService ();
				return instance;
			}
		}

		// Constructor
		/// <summary>
		///	Create a new <see cref="DBusService" />.
		/// </summary>
		/// <remarks>
		///	<para>
		///	The <see cref="Instance" /> property should be used
		/// 	instead of this constructor to ensure that only one 
		///	<see cref="DBusService" /> exists for Muine.
		///	</para>
		///
		///	<para>
		///	The name of the <see cref="Service"/ > is "org.gnome.Muine".
		///	</para>
		/// </remarks>
		private DBusService ()
		{
			try {
				// XXX: Check the result of requesting the name.
				Bus.Session.RequestName("org.gnome.Muine");
			} catch { }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: RegisterObject
		/// <summary>
		///	Registers an object with DBus.
		/// </summary>
		/// <remarks>
		///	This just invokes <see cref="DBus.Service.RegisterObject" />
		///	for the service.
		/// </remarks>
		/// <param name="obj">
		///	The <see cref="Object" /> to register.
		/// </param>
		/// <param name="path">
		///	The path to register the object at.
		/// </param>
		public void RegisterObject (object obj, string path)
		{
			Bus.Session.Register ("/org/gnome/Muine/", new ObjectPath (path), obj);
		}
	}
}
