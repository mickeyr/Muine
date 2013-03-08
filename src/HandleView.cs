/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jorn.baayen@gmail.com>
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

using Gtk;
using GLib;

namespace Muine
{
	public class HandleView : TreeView
	{
		// Variables
		private HandleModel model;

		// Constructor
		public HandleView ()
		{
			this.model = new HandleModel ();
			((TreeView) this).Model = model;

			RulesHint       = true;
			EnableSearch    = false;
			HeadersVisible  = false;
                        FixedHeightMode = true;
		}

		// Properties
		// Properties :: Model (get;)
		public new HandleModel Model {
			get { return model; }
		}

		// Properties :: SelectedHandles (get;)
		public List SelectedHandles {
			get {
				GetSelectionData d = new GetSelectionData ();
				Selection.SelectedForeach (new TreeSelectionForeachFunc (d.Func));
				return d.Selected;
			}
		}

		// Methods :: Public :: SelectFirst
		public void SelectFirst ()
		{
			TreePath path = TreePath.NewFirst ();
			SetCursor (path, Columns [0], false);
		}

		// Methods :: Public :: SelectNext
		public bool SelectNext ()
		{
			TreeModel model;
			TreePath [] sel = Selection.GetSelectedRows (out model);
			if (sel.Length == 0)
				return false;

			TreePath last = sel [sel.Length - 1];
			last.Next ();

			TreeIter iter;
			if (model.GetIter (out iter, last)) {
				SetCursor (last, Columns [0], false);
				return true;
			}

			return false;
		}

		// Methods :: Public :: SelectPrevious
		public bool SelectPrevious ()
		{
			TreeModel model;
			TreePath [] sel = Selection.GetSelectedRows (out model);
			if (sel.Length == 0)
				return false;

			TreePath first = sel [0];
			if (first.Prev ()) {
				SetCursor (first, Columns [0], false);
				return true;
			}
			
			return false;
		}

		// Methods :: Public :: Select
		public void Select (IntPtr handle)
		{
			Select (handle, true);
		}

		public void Select (IntPtr handle, bool center)
		{
			TreePath path = model.PathFromHandle (handle);

			if (center)
				ScrollToCell (path, Columns [0], true, 0.5f, 0.5f);

			SetCursor (path, Columns [0], false);
		}

		// Methods :: Public :: ForwardKeyPress
		// 	Hack to forward key press events to the treeview
		public bool ForwardKeyPress (Widget orig_widget, Gdk.EventKey e)
		{
			bool go  = false;		
			Gdk.ModifierType mod = 0;

			bool has_state = (e.State != 0);
			bool has_ctrl  = ((e.State & Gdk.ModifierType.ControlMask) != 0);
			bool has_alt   = ((e.State & Gdk.ModifierType.Mod1Mask   ) != 0);
			bool has_shift = ((e.State & Gdk.ModifierType.ShiftMask  ) != 0);
			
			bool has_mod = KeyUtils.HaveModifier (e);
			bool is_mod  = KeyUtils.IsModifier   (e);

			if        (has_state && has_ctrl) {
				go = true;
				mod = Gdk.ModifierType.ControlMask;

			} else if (has_state && has_alt) {
				go = true;
				mod = Gdk.ModifierType.Mod1Mask;

			} else if (has_state && has_shift) {
				go = true;
				mod = Gdk.ModifierType.ShiftMask;

			} else if (!has_mod && !is_mod) {
				go = true;
				mod = 0;
			}

			if (!go)
				return false;

			Gdk.GC saved_gc = Style.BaseGC (StateType.Selected);
			
			Gdk.GC active_gc = Style.BaseGC (StateType.Active);
			Style.SetBaseGC (StateType.Selected, active_gc);

			GrabFocus ();

			bool ret = Gtk.Global.BindingsActivate (this, (uint) e.Key, mod);

			Style.SetBaseGC (StateType.Selected, saved_gc);
			orig_widget.GrabFocus ();

			return ret;
		}

		// Internal Classes
		// Internal Classes :: GetSelectionData
		private class GetSelectionData
		{
			// Objects
			// Objects :: Selected
			//   We use a GLib.List here for consistency with
			//   HandleModel.Contents
			private List selected = new List (typeof (int));

			// Properties
			// Properties :: Selected (get;)
			public List Selected {
				get { return selected; }
			}

			// Delegate Functions
			// Delegate Functions :: Func
			public void Func (TreeModel model, TreePath path, TreeIter iter)
			{
				HandleModel m = (HandleModel) model;
				selected.Append (m.HandleFromIter (iter));
			}
		}
	}
}
