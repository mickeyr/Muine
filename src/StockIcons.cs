/*
 * Copyright (C) 2003, 2004 Jorn Baayen <jorn.baayen@gmail.com>
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

using Gtk;
using Gdk;

using System;
using System.IO;
using System.Reflection;

namespace Muine
{
	public static class StockIcons
	{
		// Variables
		private static IconSize cover_size;

		// Variables :: Stock Icons
		private static readonly string [] stock_icons = {
			"muine-default-cover",
			"muine-cover-downloading",
			"muine-playing",
			"muine-paused"
		};

		// Variables :: Themed Icons
		private static readonly string [] icon_theme_icons = {
			"stock_media-fwd",
			"stock_media-next",
			"stock_media-play",
			"stock_media-prev",
			"stock_media-rew",
                        "stock_music-library",
                        "stock_volume-0",
                        "stock_volume-min",
                        "stock_volume-med",
                        "stock_volume-max",
			"stock_shuffle",
			"stock_timer",
			"gnome-mime-audio"
		};

		// Properties
		// Properties :: CoverSize		
		public static IconSize CoverSize {
			get { return cover_size; }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Initalize		
		public static void Initialize ()
		{
			IconFactory factory = new IconFactory ();
			factory.AddDefault ();

			// Stock Icons
			foreach (string name in stock_icons) {
				Pixbuf  pixbuf  = new Pixbuf  (null, name + ".png");
				IconSet iconset = new IconSet (pixbuf);

				// Add menu variant if we have it
				Assembly a = Assembly.GetCallingAssembly ();

				Stream menu_stream =
				  a.GetManifestResourceStream (name + "-16.png");

				if (menu_stream != null) {
					IconSource source = new IconSource ();
					source.Pixbuf = new Pixbuf (menu_stream);
					source.Size = IconSize.Menu;
					source.SizeWildcarded = false;

					iconset.AddSource (source);
				}
				
				factory.Add (name, iconset);
			}

			// Themed Icons
			foreach (string name in icon_theme_icons) {
				IconSet    iconset    = new IconSet    ();
				IconSource iconsource = new IconSource ();

				iconsource.IconName = name;
				iconset.AddSource (iconsource);
				factory.Add (name, iconset);
			}

			// register cover image icon size
			cover_size = Icon.SizeRegister ("muine-album-cover-size",
				CoverDatabase.CoverSize, CoverDatabase.CoverSize);
		}
	}
}
