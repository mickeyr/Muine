/*
 * Copyright (C) 2005 Tamara Roberson <tamara.roberson@gmail.com>
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

namespace Muine
{
	public class AddWindowList : HandleView
	{
		// Constructor
		/// <summary>
		/// 	Creates a new <see cref="HandleView">HandleView</see>.
		/// </summary>
		/// <remarks>
		///	This widget is intended for use with 
		///	<see cref="AddWindow" /> and should not be used by
		///	other classes.
		/// </remarks>
		public AddWindowList () : base ()
		{
			base.Selection.Mode = Gtk.SelectionMode.Multiple;
		}
		
		// Properties
		// Properties :: HasSelection (get;)
		/// <summary>
		/// 	Whether any rows are currently selected.
		/// </summary>
		/// <returns>
		/// 	True if at least one row is selected, otherwise false.
		/// </returns>
		public bool HasSelection {
			get { return (base.Selection.CountSelectedRows () > 0); }
		}
		
		// Properties :: DragSource (set;)
		/// <summary>
		/// 	Set the Drag-and-Drop types for the list.
		/// </summary>
		public Gtk.TargetEntry [] DragSource {
			set {
				Gdk.DragAction act =
				  ( Gdk.DragAction.Copy
				  | Gdk.DragAction.Link
				  | Gdk.DragAction.Ask);

				base.EnableModelDragSource
				  (Gdk.ModifierType.Button1Mask, value, act);
			}
		}

		// Properties :: Selected (get;)
		//	TODO: Does it *have* to be a GLib.List?
		/// <summary>
		/// 	A <see cref="GLib.List" /> of selected items.
		/// </summary>
		public GLib.List Selected {
			get { return base.SelectedHandles; }
		}

		// Methods
		// Methods :: Public 
		// Methods :: Public :: HandleAdded
		/// <summary>
		/// 	Append the item given by <paramref name="ptr" /> if it
		/// 	<paramref name="fits" />.
		/// </summary>
		/// <remarks>
		/// 	This is used when an <see cref="Item" /> has been added
		///	to the database.
		/// </summary>
		/// <param name="ptr">
		/// 	<see cref="Item" /> handle to add.
		/// </param>
		/// <param name="fits">
		/// 	Whether the item fits or not, as returned by
		/// 	<see cref="Item.FitsCriteria" />.
		/// </param>
		public void HandleAdded (IntPtr ptr, bool fits)
		{
			if (fits)
				base.Model.Append (ptr);
		}

		// Methods :: Public :: HandleChanged
		/// <summary>
		/// 	Modify the item given by <paramref name="ptr" /> if it
		/// 	<paramref name="fits" />.
		/// </summary>
		/// <remarks>
		///	This is used when an <see cref="Item" /> has been changed.
		/// </remarks>
		/// <param name="ptr">
		/// 	<see cref="Item" /> handle.
		/// </param>
		/// <param name="fits">
		/// 	Whether the item fits or not, as returned by
		/// 	<see cref="Item.FitsCriteria" />.
		/// </param>
		public void HandleChanged (IntPtr ptr, bool fits)
		{
			if (fits) {
				if (base.Model.Contains (ptr))
					base.Model.Changed (ptr);
				else
					base.Model.Append (ptr);
			} else {
				base.Model.Remove (ptr);
			}

			SelectFirstIfNeeded ();	
		}

		// Methods :: Public :: HandleRemoved
		/// <summary>
		/// 	Remove the <see cref="Item" /> given by 
		///	<paramref name="ptr" />.
		/// </summary>
		/// <remarks>
		///	This is used when an <see cref="Item" /> has been removed.
		/// </remarks>
		/// <param name="ptr">
		/// 	<see cref="Item" /> handle to remove.
		/// </param>
		public void HandleRemoved (IntPtr ptr)
		{
			base.Model.Remove (ptr);

			SelectFirstIfNeeded ();	
		}

		// Methods :: Private
		// Methods :: Private :: SelectFirstIfNeeded
		/// <summary>
		///	Select the first item in the list if an item is not
		///	currently selected and there are items in the list.
		/// </summary>
		private void SelectFirstIfNeeded ()
		{
			if (!this.HasSelection && this.Model.Length > 0)
				SelectFirst ();
		}
	}
}
