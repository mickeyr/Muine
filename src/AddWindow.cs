/*
 * Copyright (C) 2005 Tamara Roberson <tamara.roberson@gmail.com>
 *           (C) 2003, 2004, 2005 Jorn Baayen <jorn.baayen@gmail.com>
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
using System.Collections;

namespace Muine
{
	public abstract class AddWindow : Gtk.Dialog
	{
#region Enums
#region Enums.ResponseType
		public enum ResponseType {
			Close       = Gtk.ResponseType.Close,
			DeleteEvent = Gtk.ResponseType.DeleteEvent,
			Play        = 1,
			Queue       = 2
		};
#endregion Enums.ResponseType
#endregion Enums


#region Events
#region Events.QueueEvent
		public delegate void QueueEventHandler (GLib.List songs);
		public event QueueEventHandler QueueEvent;
#endregion Events.QueueEvent

#region Events.PlayEvent
		public delegate void PlayEventHandler (GLib.List songs);
		public event PlayEventHandler PlayEvent;
#endregion Events.PlayEvent
#endregion Events


#region Constants
		private const uint search_timeout = 100;
#endregion Constants


#region Widgets
		[Glade.Widget] private Gtk.Window         window;
		[Glade.Widget] private Gtk.Label          search_label;
		[Glade.Widget] private Gtk.Container      entry_container;
		[Glade.Widget] private Gtk.Button         play_button;
		[Glade.Widget] private Gtk.Image          play_button_image;
		[Glade.Widget] private Gtk.Button         queue_button;
		[Glade.Widget] private Gtk.Image          queue_button_image;
		[Glade.Widget] private Gtk.ScrolledWindow scrolledwindow;
		
		private AddWindowEntry entry = new AddWindowEntry ();
		private AddWindowList  list  = new AddWindowList  ();

		private Gtk.CellRendererText text_renderer =
		  new Gtk.CellRendererText ();
#endregion Widgets


#region Variables
		private string gconf_key_width, gconf_key_height;
		private int gconf_default_width, gconf_default_height;
		
		private uint search_timeout_id = 0;
		private bool first_time = true;
		private bool ignore_change = false;

		private ICollection items;
#endregion Variables


#region Constructor
		/// <summary>Create a new <see cref="AddWindow" />.</summary>
		/// <remarks>This is used as a base class for
		///   <see cref="AddAlbumWindow" /> and
		///   <see cref="AddSongWindow" />.</remarks>
		public AddWindow () : base (IntPtr.Zero)
		{
			Glade.XML gxml =
			  new Glade.XML (null, "AddWindow.glade", "window", null);

			gxml.Autoconnect (this);

			base.Raw = this.window.Handle;

			this.play_button_image.SetFromStock
			  ("stock_media-play", Gtk.IconSize.Button);

			this.queue_button_image.SetFromStock
			  ("stock_timer", Gtk.IconSize.Button);

			// Cell Renderer
			this.text_renderer.Ellipsize = Pango.EllipsizeMode.End;
			// Label
			this.search_label.MnemonicWidget = this.entry;

			// Entry
			this.entry.KeyPressEvent += OnEntryKeyPressEvent;
			this.entry.Changed       += OnEntryChanged;
			this.entry_container.Add (this.entry);

			// List
			this.Response               += OnWindowResponse;
			this.DeleteEvent            += OnWindowDeleteEvent;

			this.list.RowActivated      += OnRowActivated;
			this.list.Selection.Changed += OnSelectionChanged;
			scrolledwindow.Add (this.list);

			// Show widgets (except window)
			this.entry.Show ();
			this.list.Show ();

			// Realize
			//   Needed for the cursor changing later on
			this.window.Realize ();
		}
#endregion Constructor

#region Public
#region Public.Properties
#region Public.Properties.Entry
		/// <summary>The associated <see cref="AddWindowEntry" />.</summary>
		public AddWindowEntry Entry {
			get { return this.entry; }
		}
#endregion Public.Properties.Entry

#region Public.Properties.Items
		/// <summary>A collection of the items in the list.</summary>
		public ICollection Items {
			set { this.items = value; }
			get { return this.items;  }
		}
#endregion Public.Properties.Items

#region Public.Properties.List
		/// <summary>The associated <see cref="AddWindowList" />.</summary>
		public AddWindowList List {
			get { return this.list; }
		}
#endregion Public.Properties.List

#region Public.Properties.TextRenderer
		/// <summary>The associated <see cref="CellRenderer" />.</summary>
		public Gtk.CellRenderer TextRenderer {
			get { return this.text_renderer; }
		}
#endregion Public.Properties.TextRenderer
#endregion Public.Properties


#region Public.Methods
#region Public.Methods.Run
		/// <summary>Show the window.</summary>		
		public void Run (uint time)
		{
			if (this.first_time || this.entry.Text.Length > 0) {
				this.window.GdkWindow.Cursor =
				  new Gdk.Cursor (Gdk.CursorType.Watch);

				this.window.GdkWindow.Display.Flush ();

				this.ignore_change = true;
				this.entry.Text = String.Empty;
				this.ignore_change = false;

				GLib.IdleHandler func = new GLib.IdleHandler (ResetFunc);
				GLib.Idle.Add (func);

				this.first_time = false;

			} else {
				this.list.SelectFirst ();
			}

			this.entry.GrabFocus ();

			this.window.Show ();

			this.window.GdkWindow.Focus (time);
		}
#endregion Public.Methods.Run
#endregion Public.Methods
#endregion Public


#region Protected
#region Protected.Methods
#region Protected.Methods.SetGConfSize
		/// <summary>Set the default window size according to GConf.</summary>
		/// <param name="key_width">The GConf key where the default window
		///   width is stored.</param>
		/// <param name="default_width">The width to be used if the GConf
		///   value cannot be found or is invalid.</param>
		/// <param name="key_height">The GConf key where the default window
		///   height is stored.</param>
		/// <param name="default_height">The height to be used if the GConf
		///   value cannot be found or is invalid.</param>
		protected void SetGConfSize
		  (string key_width , int default_width,
		   string key_height, int default_height)
		{
			this.gconf_key_width  = key_width;
			this.gconf_key_height = key_height;
			
			this.gconf_default_width  = default_width;
			this.gconf_default_height = default_height;
			
			int width  = (int) Config.Get (key_width , default_width );
			int height = (int) Config.Get (key_height, default_height);

			if (width < 1)
				width = default_width;
			
			if (height < 1)
				height = default_height;

			SetDefaultSize (width, height);
			
			AssertHasGConfSize ();
			this.window.SizeAllocated += OnSizeAllocated;
		}
#endregion Protected.Methods.SetGConfSize
#endregion Protected.Methods


#region Protected.Handlers
#region Protected.Handlers.OnAdded
		/// <summary>Handler called when an <see cref="Item" /> is
		///   added.</summary>
		/// <param name="item">The <see cref="Item" /> which has been
		///   added.</param>
		protected void OnAdded (Item item)
		{
			bool fits = item.FitsCriteria (entry.SearchBits);
			list.HandleAdded (item.Handle, fits);
		}
#endregion Protected.Handlers.OnAdded

#region Protected.Handlers.OnChanged
		/// <summary>Handler called when an <see cref="Item" /> is
		///   changed.</summary>
		/// <param name="item">The <see cref="Item" /> which has been
		///   changed.</param>
		protected void OnChanged (Item item)
		{
			bool fits = item.FitsCriteria (entry.SearchBits);
			list.HandleChanged (item.Handle, fits);
		}
#endregion Protected.Handlers.OnChanged

#region Protected.Handlers.OnRemoved
		/// <summary>Handler called when an <see cref="Item" /> is
		///   removed.</summary>
		/// <param name="item">The <see cref="Item" /> which has been
		///   removed.</param>
		protected void OnRemoved (Item item)
		{
			list.HandleRemoved (item.Handle);
		}
#endregion Protected.Handlers.OnRemoved
#endregion Protected.Handlers
#endregion Protected


#region Private
#region Private.Methods
#region Private.Methods.Reset
		/// <summary>Display the new results.</summary>
		private void Reset ()
		{
			Search ();

			// We want to get the normal cursor back *after* treeview
			// has done its thing.
			GLib.IdleHandler func = new GLib.IdleHandler (RestoreCursorFunc);
			GLib.Idle.Add (func);
		}
#endregion Private.Methods.Reset

#region Private.Methods.RestoreCursor
		/// <summary>Reset the cursor to be normal.</summary>
		private void RestoreCursor ()
		{
			this.window.GdkWindow.Cursor = null;
		}
#endregion Private.Methods.RestoreCursor

#region Private.Methods.Search
		/// <summary>Execute a search according to the terms currently in the
		///   entry box.</summary>
		private bool Search ()
		{
			AssertHasItems ();

			Type int_type = typeof (int);
			GLib.List results = new GLib.List (IntPtr.Zero, int_type);

			lock (Global.DB) {
				if (this.entry.Text.Length > 0) {
					foreach (Item item in this.items) {
						bool match =
						  item.FitsCriteria (this.entry.SearchBits);

						if (!match)
							continue;

						results.Append (item.Handle);
					}
				} else {
					foreach (Item item in this.items) {
						if (!item.Public)
							continue;

						results.Append (item.Handle);
					}
				}
			}

			this.list.Model.RemoveDelta (results);

			foreach (int ptr_i in results) {
				IntPtr ptr = new IntPtr (ptr_i);
				this.list.Model.Append (ptr);
			}

			this.list.SelectFirst ();
			
			// Return
			return false;
		}
#endregion Private.Methods.Search
#endregion Private.Methods


#region Private.Delegates
#region Private.Delegates.ResetFunc
		// Implements: GLib.IdleHandler
		/// <returns>False, as we only want to run once.</return>
		private bool ResetFunc ()
		{
			Reset ();
			return false;
		}
#endregion Private.Delegates.ResetFunc

#region Private.Delegates.RestoreCursorFunc
		// Implements: GLib.IdleHandler
		/// <returns>False, as we only want to run once.</returns>
		private bool RestoreCursorFunc ()
		{
			RestoreCursor ();
			return false;
		}
#endregion Private.Delegates.RestoreCursorFunc

#region Private.Delegates.SearchFunc
		// Implements: GLib.IdleHandler
		/// <returns>False, as we only want to run once.</returns>
		private bool SearchFunc ()
		{
			Search ();
			return false;
		}
#endregion Private.Delegates.SearchFunc
#endregion Private.Delegates


#region Private.Handlers
#region Private.Handlers.Entry
#region Private.Handlers.Entry.OnEntryChanged
		// Implements: System.EventHandler
		/// <summary>Handler called when the entry has been changed.</summary>
		/// <remarks>Calls <see cref="Search" /> unless changes are currently
		///   being ignored.</remarks>
		private void OnEntryChanged (object o, EventArgs args)
		{
			if (this.ignore_change)
				return;

			if (this.search_timeout_id > 0)
				GLib.Source.Remove (this.search_timeout_id);

			GLib.TimeoutHandler func = new GLib.TimeoutHandler (SearchFunc);

			this.search_timeout_id =
			  GLib.Timeout.Add (AddWindow.search_timeout, func);
		}
#endregion Private.Handlers.Entry.OnEntryChanged

#region Private.Handlers.Entry.OnEntryKeyPressEvent
		// Implements: Gtk.KeyPressEventHandler
		/// <summary>Handler called when a key is pressed.</summary>
		/// <remarks>Forwards the value of the key on to the 
		///   <see cref="HandleView" />.</remarks>
		private void OnEntryKeyPressEvent
		  (object o, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = list.ForwardKeyPress (entry, args.Event);
		}
#endregion Private.Handlers.Entry.OnEntryKeyPressEvent
#endregion Private.Handlers.Entry


#region Private.Handlers.List
#region Private.Handlers.List.OnRowActivated
		/// <summary>Handler called when the a row is activated (such as with
		///	  a double-click).</summary>
		/// <remarks>Activating a row is the same as clicking the Play
		///   button.</remarks>
		private void OnRowActivated (object o, Gtk.RowActivatedArgs args)
		{
			play_button.Click ();
		}
#endregion Private.Handlers.List.OnRowActivated

#region Private.Handlers.List.OnSelectionChanged
		/// <summary>Handler called when the selection is changed.</summary>
		/// <remarks>If no selection is present, the Play and Queue buttons
		///	  are disabled. Otherwise, they are enabled.</remarks>
		private void OnSelectionChanged (object o, EventArgs args)
		{
			play_button.Sensitive  = list.HasSelection;
			queue_button.Sensitive = list.HasSelection;
		}
#endregion Private.Handlers.List.OnSelectionChanged
#endregion Private.Handlers.List

#region Private.Handlers.Window
#region Private.Handlers.Window.OnSizeAllocated
		/// <summary>Handler called when the window is resized.</summary>
		/// <remarks>Sets the new size in GConf if keys are present.</remarks>
		private void OnSizeAllocated
		  (object o, Gtk.SizeAllocatedArgs args)
		{
			if (!HasGConfSize ())
				return;

			int width, height;
			window.GetSize (out width, out height);

			Config.Set (gconf_key_width , width );
			Config.Set (gconf_key_height, height);
		}
#endregion Private.Handlers.Window.OnSizeAllocated

#region Private.Handlers.Window.OnWindowDeleteEvent
		//  TODO: Why not just hide the window here?
		/// <summary>Handler called when the window is closed.</summary>
		/// <remarks>This refuses to let the window close because that is
		///   handled by <see cref="OnWindowResponse" /> so it can be
		///	  hidden instead.</remarks>
		public void OnWindowDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			args.RetVal = true;
		}
#endregion Private.Handlers.Window.OnWindowDeleteEvent

#region Private.Handlers.Window.OnWindowResponse
		/// <summary>Handler called when a response has been chosen.</summary>
		/// <remarks>If the window is closed or the Close button is clicked,
		///   the window simply hides. If the Play button is clicked, the
		///   window hides and the selected item starts playing. If the Queue
		///   button is clicked, the selected item is added to the queue but
		///   the window is not hidden and the item does not start
		///   playing.</remarks>
		/// <exception cref="ArgumentException">Thrown if the response is not
		///   window deleted, close, play, or queue. Really only possible if
		///   we add another button to the window but forget to add it
		///   here.</exception>
		public void OnWindowResponse (object o, Gtk.ResponseArgs args)
		{
			switch ((int) args.ResponseId) {
			case (int) ResponseType.DeleteEvent:
			case (int) ResponseType.Close:
				window.Visible = false;
				
				break;
				
			case (int) ResponseType.Play:
				window.Visible = false;

				if (PlayEvent != null)
					PlayEvent (list.Selected);

				break;
				
			case (int) ResponseType.Queue:
				if (QueueEvent != null)
					QueueEvent (list.Selected);

				entry.GrabFocus ();
				list.SelectNext ();

				break;
				
			default:
				throw new ArgumentException ();
			}
		}
#endregion Private.Handlers.Window.OnWindowResponse
#endregion Private.Handlers.Window
#endregion Private.Handlers


#region Private.Tests
#region Private.Tests.HasGConfSize
		/// <summary>Returns whether or not GConf contains a valid window
		///   size.</summary>
		/// <returns>True if GConf keys exist for both width and height and
		///   are > 0, otherwise returns false.</returns>
		private bool HasGConfSize ()
		{
			bool has_width_key  = (gconf_key_width  != String.Empty);
			bool has_height_key = (gconf_key_height != String.Empty);
			
			bool has_width_val  = (gconf_default_width  > 0);
			bool has_height_val = (gconf_default_height > 0);
			
			bool has_width  = (has_width_key  && has_width_val );
			bool has_height = (has_height_key && has_height_val);

			return (has_width && has_height);
		}
#endregion Private.Tests.HasGConfSize

#region Private.Tests.HasItems
		/// <summary>Returns whether or not the list is empty.</summary>
		/// <returns>True if the list has items, False otherwise.</returns>
		private bool HasItems ()
		{
			return (items != null);
		}
#endregion Private.Tests.HasItems
#endregion Private.Tests


#region Private.Assertions
#region Private.Assertions.AssertHasGConfSize
		/// <summary>If GConf does not contain a valid size, then throws an
		///	  exception.</summary>
		/// <exception cref="InvalidOperationException">Thrown if GConf does
		///   not contain a valid size.</exception>
		private void AssertHasGConfSize ()
		{
			if (!HasGConfSize ())
				throw new InvalidOperationException ();		
		}
#endregion Private.Assertions.AssertHasGConfSize

#region Private.Assertions.AssertHasItems
		/// <summary>Throws an exception if the list is empty.</summary>
		/// <exception cref="InvalidOperationException">Thrown if the list is
		///   empty.</exception>
		private void AssertHasItems ()
		{
			if (!HasItems ())
				throw new InvalidOperationException ();
		}
#endregion Private.Assertions.AssertHasItems
#endregion Private.Assertions
#endregion Private
	}
}
