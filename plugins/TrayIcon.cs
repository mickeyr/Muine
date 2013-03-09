/*
 * Copyright (C) 2003, 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Runtime.InteropServices;

using GConf;
using Gtk;
using Gdk;

using Mono.Unix;

using Muine.PluginLib;

namespace Muine
{
	public class TrayIcon : Plugin
	{
		// Strings
		private static readonly string string_program =
			Catalog.GetString ("Muine music player");

		// Strings :: Tooltip Format
		//	song artists - song title
		private static readonly string string_tooltip_format = 
			Catalog.GetString ("{0} - {1}");

		// Strings :: Notification Format
		//	song artists - song title
		private static readonly string string_notification_summary_format = 
			Catalog.GetString ("Now playing: {0}");

		// Strings :: Notification Format
		//	Album name
		private static readonly string string_notification_message_format = 
			Catalog.GetString ("by {0}");

                private const string GConfKeyShowNotifications = "/apps/muine/show_notifications";
		// Widgets
		private Plug icon;
		private EventBox ebox;
		private Gtk.Image image;
		private Tooltips tooltips;
		private Menu menu;

		// Objects
		private IPlayer player;

		// Variables
		private int menu_x;
		private int menu_y;
		
		private string tooltip = String.Empty; 

		private static bool showNotifications = false;

		private bool playing = false;

		// GConf settings (mouse button behaviour)
		private GConf.Client gconf_client;
		private bool old_mouse_behaviour;
		private const string old_behaviour_key = "/apps/muine/plugins/trayicon/old_behaviour";

		// Plugin initializer
		public override void Initialize (IPlayer player)
		{
			// Initialize gettext
			Catalog.Init ("muine", Defines.GNOME_LOCALE_DIR);

                        GConf.Client gconf_client = new GConf.Client ();
                        gconf_client.AddNotify (GConfKeyShowNotifications, 
                                                new GConf.NotifyEventHandler (OnShowNotificationsChanged));
                        showNotifications = (bool) gconf_client.Get (GConfKeyShowNotifications);

			// Load stock icons
			InitStockIcons ();

			// Connect to player
			this.player = player;
			
			player.SongChangedEvent  += OnSongChangedEvent ;
			player.StateChangedEvent += OnStateChangedEvent;

			// Install "Hide Window" menu item
			player.UIManager.AddUi (player.UIManager.NewMergeId (), 
			                        "/MenuBar/FileMenu/ExtraFileActions", "ToggleVisibleMenuItem",
			                        "ToggleVisible", UIManagerItemType.Menuitem, false);
			
			// Build menu
			player.UIManager.AddUiFromResource ("TrayIcon.xml");

			// Setup GConf
			gconf_client = new GConf.Client();
			gconf_client.AddNotify (old_behaviour_key, BehaviourNotify);
			try {
				old_mouse_behaviour = (bool) gconf_client.Get(old_behaviour_key);
			} catch {
				old_mouse_behaviour = false;
                                gconf_client.Set(old_behaviour_key, false);
			}

			// Maybe prevent 'delete' event from closing window by intercepting it
			player.Window.WidgetEvent += new WidgetEventHandler (OnWindowEvent);
			
			menu = (Menu) player.UIManager.GetWidget ("/Menu");
			menu.Deactivated += OnMenuDeactivated;

			// Init tooltips -- we init into "not playing" state
			tooltips = new Tooltips ();
			tooltips.Disable ();

			// init icon
			Init ();
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Init
		public void Init ()
		{
			icon = new NotificationArea (string_program);

			icon.DestroyEvent += OnDestroyEvent;

			ebox = new EventBox ();
			ebox.ButtonPressEvent += OnButtonPressEvent;
			
			image = new Gtk.Image ();

			ebox.Add (image);
			icon.Add (ebox);

			UpdateImage ();
			UpdateTooltip ();

			icon.ShowAll ();
		}

		// Methods :: Private
		// Methods :: Private :: UpdateTooltip
		private void UpdateTooltip ()
		{
			tooltips.SetTip (icon, tooltip, null);
		}

		// Methods :: Private :: UpdateImage
		private void UpdateImage ()
		{
			string icon = (playing) ? "muine-tray-playing" : "muine-tray-paused";
			image.SetFromStock (icon, IconSize.Menu);
		}

		// Methods :: Private :: PositionMenu
		private void PositionMenu (Menu menu, out int x, out int y, out bool push_in)
		{
			x = menu_x;
			y = menu_y;

			int           monitor = ((Widget) menu).Screen.GetMonitorAtPoint  (x, y   );
			Gdk.Rectangle rect    = ((Widget) menu).Screen.GetMonitorGeometry (monitor);

			int space_above = y - rect.Y;
			int space_below = rect.Y + rect.Height - y;

			Requisition requisition = menu.SizeRequest ();

			if (requisition.Height <= space_above ||
			    requisition.Height <= space_below) {

				if (requisition.Height <= space_below)
					y += ebox.Allocation.Height;
				else
					y -= requisition.Height;

			} else if (requisition.Height > space_below && 
				   requisition.Height > space_above) {

				if (space_below >= space_above)
					y = rect.Y + rect.Height - requisition.Height;
				else
					y = rect.Y;

			} else {
				y = rect.Y;
			}

			push_in = true;
		}

		// Methods :: Private :: CreateTooltip
		private string CreateTooltip (ISong song)
		{
			return String.Format (string_tooltip_format,
				StringUtils.JoinHumanReadable (song.Artists), song.Title);
		}

		// Methods :: Private :: InitStockIcons
		private void InitStockIcons ()
		{
			string [] stock_icons = {
				"muine-tray-paused",
				"muine-tray-playing"
			};

			IconFactory factory = new IconFactory ();
			factory.AddDefault ();

			// Stock Icons
			foreach (string name in stock_icons) {
				Pixbuf  pixbuf  = new Pixbuf  (null, name + ".png");
				IconSet iconset = new IconSet (pixbuf);

				factory.Add (name, iconset);
			}
		}

		// Handlers
		// Handlers :: OnButtonPressEvent
		private void OnButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Type != EventType.ButtonPress)
				return;

			switch (args.Event.Button)
			{
			case 1:
			case 3:
				if (!old_mouse_behaviour && args.Event.Button == 1)
				{
					player.SetWindowVisible (!player.WindowVisible, args.Event.Time);
					break;
				}

				icon.State = StateType.Active;

				menu_x = (int) args.Event.XRoot - (int) args.Event.X;
				menu_y = (int) args.Event.YRoot - (int) args.Event.Y;

				menu.Popup (null, null, new MenuPositionFunc (PositionMenu),
				            args.Event.Button, args.Event.Time);
				
				break;

			case 2:
				if (old_mouse_behaviour)
					player.SetWindowVisible (!player.WindowVisible, args.Event.Time);
				else
					player.Playing = !player.Playing;

				break;

			default:
				break;
			}

			args.RetVal = false;
		}

		// Handlers :: OnMenuDeactivated
		private void OnMenuDeactivated (object o, EventArgs args)
		{
			icon.State = StateType.Normal;
		}

		// Handlers :: OnDestroyEvent
		private void OnDestroyEvent (object o, DestroyEventArgs args)
		{
			Init ();
		}

                // Handlers :: OnShowNotificationsChanged
                private void OnShowNotificationsChanged (object o, GConf.NotifyEventArgs args)
                {
                        showNotifications = (bool) args.Value;
                }

		// Handlers :: OnSongChangedEvent
		private void OnSongChangedEvent (ISong song)
		{
			tooltip = (song == null) ? null : CreateTooltip (song);

			UpdateTooltip ();

			if (showNotifications && (song != null)) {
				Notify(
						String.Format(string_notification_summary_format, song.Title),
						String.Format(
							string_notification_message_format,
							StringUtils.JoinHumanReadable (song.Artists)),
						song.CoverImage,
						ebox);
			}
		}

		// Handlers :: OnStateChangedEvent
		private void OnStateChangedEvent (bool playing)
		{
			if (playing)
				tooltips.Enable ();
			else
				tooltips.Disable ();

			this.playing = playing;

			UpdateImage ();
		}

		/* Libnotify bindings */

		[DllImport("notify")]
		private static extern bool notify_init(string app_name);

		[DllImport("notify")]
		private static extern void notify_uninit();

		[DllImport("notify")]
		private static extern IntPtr notify_notification_new(string summary, string message,
				string icon, IntPtr widget);

		[DllImport("notify")]
		private static extern void notify_notification_set_timeout(IntPtr notification,
				int timeout);
		
		[DllImport("notify")]
		private static extern void notify_notification_set_urgency(IntPtr notification,
				int urgency);

		[DllImport("notify")]
		private static extern void notify_notification_set_icon_from_pixbuf(IntPtr notification, IntPtr icon);

		[DllImport("notify")]
		private static extern bool notify_notification_show(IntPtr notification, IntPtr error);

		[DllImport("gobject-2.0")]
		private static extern void g_object_unref(IntPtr o);

		public static void Notify(string summary, string message,
				Pixbuf cover, Widget widget)
		{
                        if (!showNotifications)
				return;

			try {
				if(!notify_init("Muine"))
					return;

				summary = StringUtils.EscapeForPango(summary);
				message = StringUtils.EscapeForPango(message);

				IntPtr notif = notify_notification_new(summary, message, null, widget.Handle);
				notify_notification_set_timeout(notif, 4000);
				notify_notification_set_urgency(notif, 0);
				if (cover != null) {
					cover = cover.ScaleSimple(42, 42, InterpType.Bilinear);
					notify_notification_set_icon_from_pixbuf(notif, cover.Handle);
				}
				notify_notification_show(notif, IntPtr.Zero);
				g_object_unref(notif);
				notify_uninit();

			} catch (Exception) {
				showNotifications = false;
			}
		}

		private void OnWindowEvent (object o, WidgetEventArgs args)
                {
			if (args.Event.Type == EventType.Delete && !old_mouse_behaviour) {
                                player.SetWindowVisible (false, 0);
                                args.RetVal = true;
                        }
                }

		private void BehaviourNotify (object sender, NotifyEventArgs args)
		{
			old_mouse_behaviour = (bool) args.Value;
		}
	}
}
