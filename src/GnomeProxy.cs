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
using System.Net;

namespace Muine
{
	public class GnomeProxy
	{
		// GConf
		private const string GConfProxyPath = "/system/http_proxy";

		// GConf :: Use
		private const string GConfKeyUse = GConfProxyPath + "/use_http_proxy";
		private const bool   GConfDefaultUse = false;

		// GConf :: Host
		private const string GConfKeyHost = GConfProxyPath + "/host";
		private const string GConfDefaultHost = "";

		// GConf :: Port
		private const string GConfKeyPort = GConfProxyPath + "/port";
		private const int    GConfDefaultPort = 8080;

		// GConf :: UseAuth
		private const string GConfKeyUseAuth = GConfProxyPath + "/use_authentication";
		private const bool   GConfDefaultUseAuth = false;

		// GConf :: User
		private const string GConfKeyUser = GConfProxyPath + "/authentication_user";
		private const string GConfDefaultUser = "";

		// GConf :: Pass
		private const string GConfKeyPass = GConfProxyPath + "/authentication_password";
		private const string GConfDefaultPass = "";

		// Objects
		private WebProxy proxy;

		// Variables		
		private bool use;

		// Constructor		
		/// <summary>
		///	Create a new <see cref="GnomeProxy" /> object.
		/// </summary>
		public GnomeProxy ()
		{
			Setup ();

			Config.AddNotify (GConfProxyPath, 
				new GConf.NotifyEventHandler (OnConfigChanged));
		}

		// Properties
		// Properties :: Use (get;)
		/// <summary>
		///	Whether or not to use the proxy.
		/// </summary>
		/// <returns>
		///	True if the proxy should be used, False otherwise.
		/// </returns>
		public bool Use {
			get { return use; }
		}

		// Properties :: Proxy (get;)
		/// <summary>
		///	The proxy.
		/// </summary>
		/// <returns>
		///	A <see cref="WebProxy" /> object.
		/// </returns>
		public WebProxy Proxy {
			get { return proxy; }
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: Setup
		/// <summary>
		///	Setup the proxy based on GConf information.
		/// </summary>
		/// <remarks>
		///	This is separate so we can re-run it if things change.
		/// </remarks>
		private void Setup ()
		{
			proxy = null;

			use = (bool) Config.Get (GConfKeyUse, GConfDefaultUse);

			if (!use)
				return;

			// Host / Proxy
			string host =
			  (string) Config.Get (GConfKeyHost, GConfDefaultHost);

			int port = (int) Config.Get (GConfKeyPort, GConfDefaultPort);
			
			try {
				proxy = new WebProxy (host, port);

			} catch {
				use = false;
				return;
			}

			// Authentication
			bool use_auth =
			  (bool) Config.Get (GConfKeyUseAuth, GConfDefaultUseAuth);

			if (!use_auth)
				return;

			string user =
			  (string) Config.Get (GConfKeyUser, GConfDefaultUser);

			string passwd =
			  (string) Config.Get (GConfKeyPass, GConfDefaultPass);
					
			try {
				proxy.Credentials = new NetworkCredential (user, passwd);

			} catch {
				use_auth = false;
			}
		}

		// Handlers
		// Handlers :: OnConfigChanged
		/// <summary>
		///	Handler called when a GConf key has been changed.
		/// </summary>
		/// <remarks>
		///	This re-runs <see cref="Setup" />.
		/// </remarks>
		private void OnConfigChanged (object o, GConf.NotifyEventArgs args)
		{
			Setup ();
		}
	}
}
