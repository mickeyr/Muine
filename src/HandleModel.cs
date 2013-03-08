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

using System;
using System.Runtime.InteropServices;
using System.Collections;

using Gtk;

namespace Muine
{
	public class HandleModel
	  : GLib.Object, TreeModel, TreeDragSource, TreeDragDest
	{
		// Events
		public delegate void PlayingChangedHandler (IntPtr handle);
		public event         PlayingChangedHandler PlayingChanged;

		// Delegates
		// Delegates :: Public
		public delegate int CompareFunc (IntPtr a, IntPtr b);
		
		// Delegates :: Internal
		internal delegate int CompareFuncNative (IntPtr a, IntPtr b);

		// Constructor
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_new ();

		public HandleModel () : base (pointer_list_model_new ()) {}

		// Destructor
		~HandleModel ()
		{
			Dispose ();
		}

		// Properties
		// Properties :: GType (get;)
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_get_type ();

		public static new GLib.GType GType { 
			get {
				IntPtr gtype_ptr = pointer_list_model_get_type ();
				GLib.GType gtype = new GLib.GType (gtype_ptr);
				return gtype;
			}
		}

		// Properties :: SortFunc (set;)
		[DllImport("libmuine")]
		private static extern void pointer_list_model_set_sorting
		  (IntPtr raw, CompareFuncNative sort_func);

		public CompareFunc SortFunc {
			set {
				CompareFuncWrapper wrapper =
				  new CompareFuncWrapper (value, this);

				pointer_list_model_set_sorting (Raw, wrapper.NativeDelegate);
			}
		}

		// Properties :: Playing (set; get;)
		[DllImport("libmuine")]
		private static extern void pointer_list_model_set_current
		  (IntPtr raw, IntPtr pointer);

		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_get_current
		  (IntPtr raw);

		public IntPtr Playing {
			set {
				pointer_list_model_set_current (Raw, value);

				if (PlayingChanged == null)
					return;

				PlayingChanged (value);
			}

			get { return pointer_list_model_get_current (Raw); }
		}

		// Properties :: Contents (get;)
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_get_pointers
		  (IntPtr raw);

		public GLib.List Contents {
			get {
				IntPtr list_ptr = pointer_list_model_get_pointers (Raw);
				Type int_type = typeof (int);
				GLib.List list = new GLib.List (list_ptr, int_type);
				list.Managed = true;

				return list;
			}
		}

		// Properties :: Length (get;)
		public int Length {
			get { return IterNChildren (); }
		}

		// Properties :: HasFirst (get;)
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_has_first (IntPtr raw);

		public bool HasFirst {
			get { return pointer_list_model_has_first (Raw); }
		}

		// Properties :: HasPrevious (get;)
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_has_prev (IntPtr raw);

		public bool HasPrevious {
			get { return pointer_list_model_has_prev (Raw); }
		}

		// Properties :: HasNext (get;)
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_has_next (IntPtr raw);

		public bool HasNext {
			get { return pointer_list_model_has_next (Raw); }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Append
		[DllImport("libmuine")]
		private static extern void pointer_list_model_add
		  (IntPtr raw, IntPtr pointer);

		public void Append (IntPtr handle)
		{
			pointer_list_model_add (Raw, handle);
		}

		// Methods :: Public :: Insert
		[DllImport("libmuine")]
		private static extern void pointer_list_model_insert
		  (IntPtr raw, IntPtr pointer, IntPtr ins, uint pos);

		public void Insert
		  (IntPtr handle, IntPtr ins, TreeViewDropPosition pos)
		{
			pointer_list_model_insert (Raw, handle, ins, (uint) pos);
		}

		// Methods :: Public :: Contains
		[DllImport("libmuine")]
		private static extern bool pointer_list_model_contains
		  (IntPtr raw, IntPtr pointer);

		public bool Contains (IntPtr handle)
		{
			return pointer_list_model_contains (Raw, handle);
		}

		// Methods :: Public :: Changed
		public void Changed (IntPtr handle)
		{
			TreeIter iter = IterFromHandle (handle);
			EmitRowChanged (GetPath (iter), iter);
		}
		
		// Methods :: Public :: Remove
		[DllImport("libmuine")]
		private static extern void pointer_list_model_remove
		  (IntPtr raw, IntPtr pointer);

		public void Remove (IntPtr handle)
		{
			pointer_list_model_remove (Raw, handle);
		}

		// Methods :: Public :: RemoveDelta
		[DllImport("libmuine")]
		private static extern void pointer_list_model_remove_delta
		  (IntPtr raw, IntPtr delta);

		public void RemoveDelta (GLib.List delta)
		{
			pointer_list_model_remove_delta (Raw, delta.Handle);
		}

		// Methods :: Public :: Clear
		[DllImport("libmuine")]
		private static extern void pointer_list_model_clear (IntPtr raw);
		
		public void Clear ()
		{
			bool playing_changed = (Playing != IntPtr.Zero);
			
			pointer_list_model_clear (Raw);

			if (playing_changed || PlayingChanged == null)
				return;

			PlayingChanged (IntPtr.Zero);
		}

		// Methods :: Public :: HandleFromIter
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_iter_get_pointer
		  (IntPtr raw, ref TreeIter iter);

		public IntPtr HandleFromIter (TreeIter iter)
		{
			return pointer_list_model_iter_get_pointer (Raw, ref iter);
		}

		// Methods :: Public :: HandleFromPath
		public IntPtr HandleFromPath (TreePath path)
		{
			TreeIter iter;
			GetIter (out iter, path);
			return HandleFromIter (iter);
		}

		// Methods :: Public :: IterFromHandle
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_pointer_get_iter
		  (IntPtr raw, IntPtr pointer, out TreeIter iter);

		public TreeIter IterFromHandle (IntPtr handle)
		{
			TreeIter iter;
			pointer_list_model_pointer_get_iter (Raw, handle, out iter);
			return iter;
		}

		// Methods :: Public :: PathFromHandle
		public TreePath PathFromHandle (IntPtr handle)
		{
			return GetPath (IterFromHandle (handle));
		}

		// Methods :: Public :: Sort
		[DllImport("libmuine")]
		private static extern void pointer_list_model_sort (IntPtr raw, 
								    CompareFuncNative sort_func);

		public void Sort (CompareFunc func)
		{
			CompareFuncWrapper wrapper = new CompareFuncWrapper (func, this);
			pointer_list_model_sort (Raw, wrapper.NativeDelegate);
		}

		// Methods :: Public :: First
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_first (IntPtr raw);

		public IntPtr First ()
		{
			IntPtr ret = pointer_list_model_first (Raw);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Methods :: Public :: Last
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_last (IntPtr raw);

		public IntPtr Last ()
		{
			IntPtr ret = pointer_list_model_last (Raw);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Methods :: Public :: Previous
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_prev (IntPtr raw);

		public IntPtr Previous ()
		{
			IntPtr ret = pointer_list_model_prev (Raw);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Methods :: Public :: Next
		[DllImport("libmuine")]
		private static extern IntPtr pointer_list_model_next (IntPtr raw);

		public IntPtr Next ()
		{
			IntPtr ret = pointer_list_model_next (Raw);

			if (PlayingChanged != null)
				PlayingChanged (ret);

			return ret;
		}

		// Internal Classes
		// Internal Classes :: CompareFuncWrapper
		internal class CompareFuncWrapper : GLib.DelegateWrapper
		{
			protected CompareFunc _managed;
			internal CompareFuncNative NativeDelegate;

			public int NativeCallback (IntPtr a, IntPtr b)
			{
				return (int) _managed (a, b);
			}

			public CompareFuncWrapper (CompareFunc managed, object o)
			  : base (o)
			{
				NativeDelegate = new CompareFuncNative (NativeCallback);
				_managed = managed;
			}
		}

///////////////////////////// Internal fluff taken from gtk-sharp ////////////////////////////////

		public void SetValue (Gtk.TreeIter iter, int column, GLib.Value value) {}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_unref_node(IntPtr raw, ref Gtk.TreeIter iter);

		public void UnrefNode(Gtk.TreeIter iter) {
			gtk_tree_model_unref_node(Handle, ref iter);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern IntPtr gtk_tree_model_get_column_type(IntPtr raw, int index_);

		public GLib.GType GetColumnType(int index_) {
			IntPtr raw_ret = gtk_tree_model_get_column_type(Handle, index_);
			GLib.GType ret = new GLib.GType(raw_ret);
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_get_valist(IntPtr raw, ref Gtk.TreeIter iter, IntPtr var_args);

		public void GetValist(Gtk.TreeIter iter, IntPtr var_args) {
			gtk_tree_model_get_valist(Handle, ref iter, var_args);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern int gtk_tree_model_get_flags(IntPtr raw);

		public Gtk.TreeModelFlags Flags { 
			get {
				int raw_ret = gtk_tree_model_get_flags(Handle);
				Gtk.TreeModelFlags ret = (Gtk.TreeModelFlags)raw_ret;
				return ret;
			}
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_iter_next(IntPtr raw, ref Gtk.TreeIter iter);

		public bool IterNext(ref Gtk.TreeIter iter) {
			bool raw_ret = gtk_tree_model_iter_next(Handle, ref iter);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern IntPtr gtk_tree_model_get_string_from_iter(IntPtr raw, ref Gtk.TreeIter iter);

		public string GetStringFromIter(Gtk.TreeIter iter) {
			IntPtr raw_ret = gtk_tree_model_get_string_from_iter(Handle, ref iter);
			string ret = GLib.Marshaller.PtrToStringGFree(raw_ret);
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_iter_parent(IntPtr raw, out Gtk.TreeIter iter, ref Gtk.TreeIter child);

		public bool IterParent(out Gtk.TreeIter iter, Gtk.TreeIter child) {
			bool raw_ret = gtk_tree_model_iter_parent(Handle, out iter, ref child);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_row_deleted(IntPtr raw, IntPtr path);

		public void EmitRowDeleted(Gtk.TreePath path) {
			gtk_tree_model_row_deleted(Handle, path.Handle);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_row_inserted(IntPtr raw, IntPtr path, ref Gtk.TreeIter iter);

		public void EmitRowInserted(Gtk.TreePath path, Gtk.TreeIter iter) {
			gtk_tree_model_row_inserted(Handle, path.Handle, ref iter);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern IntPtr gtk_tree_model_get_path(IntPtr raw, ref Gtk.TreeIter iter);

		public Gtk.TreePath GetPath(Gtk.TreeIter iter) {
			IntPtr raw_ret = gtk_tree_model_get_path(Handle, ref iter);
			Gtk.TreePath ret;
			if (raw_ret == IntPtr.Zero)
				ret = null;
			else
				ret = new Gtk.TreePath(raw_ret);
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_get_iter(IntPtr raw, out Gtk.TreeIter iter, IntPtr path);

		public bool GetIter(out Gtk.TreeIter iter, Gtk.TreePath path) {
			bool raw_ret = gtk_tree_model_get_iter(Handle, out iter, path.Handle);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_iter_children(IntPtr raw, out Gtk.TreeIter iter, ref Gtk.TreeIter parent);

		public bool IterChildren(out Gtk.TreeIter iter, Gtk.TreeIter parent) {
			bool raw_ret = gtk_tree_model_iter_children(Handle, out iter, ref parent);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern int gtk_tree_model_iter_n_children(IntPtr raw, ref Gtk.TreeIter iter);

		public int IterNChildren(Gtk.TreeIter iter) {
			int raw_ret = gtk_tree_model_iter_n_children(Handle, ref iter);
			int ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_ref_node(IntPtr raw, ref Gtk.TreeIter iter);

		public void RefNode(Gtk.TreeIter iter) {
			gtk_tree_model_ref_node(Handle, ref iter);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_get_iter_from_string(IntPtr raw, out Gtk.TreeIter iter, string path_string);

		public bool GetIterFromString(out Gtk.TreeIter iter, string path_string) {
			bool raw_ret = gtk_tree_model_get_iter_from_string(Handle, out iter, path_string);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_iter_has_child(IntPtr raw, ref Gtk.TreeIter iter);

		public bool IterHasChild(Gtk.TreeIter iter) {
			bool raw_ret = gtk_tree_model_iter_has_child(Handle, ref iter);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_rows_reordered(IntPtr raw, IntPtr path, ref Gtk.TreeIter iter, out int[] new_order);

		public void EmitRowsReordered(Gtk.TreePath path, Gtk.TreeIter iter, int[] new_order) {
			gtk_tree_model_rows_reordered(Handle, path.Handle, ref iter, out new_order);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_iter_nth_child(IntPtr raw, out Gtk.TreeIter iter, ref Gtk.TreeIter parent, int n);

		public bool IterNthChild(out Gtk.TreeIter iter, Gtk.TreeIter parent, int n) {
			bool raw_ret = gtk_tree_model_iter_nth_child(Handle, out iter, ref parent, n);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern int gtk_tree_model_get_n_columns(IntPtr raw);

		public int NColumns { 
			get {
				int raw_ret = gtk_tree_model_get_n_columns(Handle);
				int ret = raw_ret;
				return ret;
			}
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_get_iter_first(IntPtr raw, out Gtk.TreeIter iter);

		public bool GetIterFirst(out Gtk.TreeIter iter) {
			bool raw_ret = gtk_tree_model_get_iter_first(Handle, out iter);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_get_value(IntPtr raw, ref Gtk.TreeIter iter, int column, ref GLib.Value value);

		public void GetValue(Gtk.TreeIter iter, int column, ref GLib.Value value) {
			gtk_tree_model_get_value(Handle, ref iter, column, ref value);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_row_has_child_toggled(IntPtr raw, IntPtr path, ref Gtk.TreeIter iter);

		public void EmitRowHasChildToggled(Gtk.TreePath path, Gtk.TreeIter iter) {
			gtk_tree_model_row_has_child_toggled(Handle, path.Handle, ref iter);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_foreach(IntPtr raw, GtkSharp.TreeModelForeachFuncNative func, IntPtr user_data);

		public void Foreach(Gtk.TreeModelForeachFunc func) {
			GtkSharp.TreeModelForeachFuncWrapper func_wrapper = null;
			func_wrapper = new GtkSharp.TreeModelForeachFuncWrapper (func, this);
			gtk_tree_model_foreach(Handle, func_wrapper.NativeDelegate, IntPtr.Zero);
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern void gtk_tree_model_row_changed(IntPtr raw, IntPtr path, ref Gtk.TreeIter iter);

		public void EmitRowChanged(Gtk.TreePath path, Gtk.TreeIter iter) {
			gtk_tree_model_row_changed(Handle, path.Handle, ref iter);
		}

		delegate void RowsReorderedSignalDelegate (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, out int arg3, IntPtr gch);

		static void RowsReorderedSignalCallback (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, out int arg3, IntPtr gch)
		{
			GLib.Signal sig = ((GCHandle) gch).Target as GLib.Signal;
			if (sig == null)
				throw new Exception("Unknown signal GC handle received " + gch);

			Gtk.RowsReorderedArgs args = new Gtk.RowsReorderedArgs ();
			args.Args = new object[3];
			if (arg1 == IntPtr.Zero)
				args.Args[0] = null;
			else {
				args.Args[0] = new Gtk.TreePath(arg1);
			}
			args.Args[1] = arg2;
			Gtk.RowsReorderedHandler handler = (Gtk.RowsReorderedHandler) sig.Handler;
			handler (GLib.Object.GetObject (arg0), args);
			arg3 = ((int)args.Args[2]);

		}

		delegate void RowsReorderedVMDelegate (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter, out int new_order);

		static RowsReorderedVMDelegate RowsReorderedVMCallback;

		static void rowsreordered_cb (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter, out int new_order)
		{
			HandleModel obj = GLib.Object.GetObject (tree_model, false) as HandleModel;
			obj.OnRowsReordered (new Gtk.TreePath(path), iter, out new_order);
		}

		private static void OverrideRowsReordered (GLib.GType gtype)
		{
			if (RowsReorderedVMCallback == null)
				RowsReorderedVMCallback = new RowsReorderedVMDelegate (rowsreordered_cb);
			OverrideVirtualMethod (gtype, "rows_reordered", RowsReorderedVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(HandleModel), ConnectionMethod="OverrideRowsReordered")]
		protected virtual void OnRowsReordered (Gtk.TreePath path, Gtk.TreeIter iter, out int new_order)
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (4);
			GLib.Value[] vals = new GLib.Value [4];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			vals [1] = new GLib.Value (path);
			inst_and_params.Append (vals [1]);
			vals [2] = new GLib.Value (iter);
			inst_and_params.Append (vals [2]);
			vals [3] = GLib.Value.Empty;
			inst_and_params.Append (vals [3]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
			new_order = (int) vals [3];

		}

		[GLib.Signal("rows_reordered")]
		public event Gtk.RowsReorderedHandler RowsReordered {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "rows_reordered", new RowsReorderedSignalDelegate(RowsReorderedSignalCallback));
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "rows_reordered", new RowsReorderedSignalDelegate(RowsReorderedSignalCallback));
				sig.RemoveDelegate (value);
			}
		}

		delegate void RowChangedSignalDelegate (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, IntPtr gch);

		static void RowChangedSignalCallback (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, IntPtr gch)
		{
			GLib.Signal sig = ((GCHandle) gch).Target as GLib.Signal;
			if (sig == null)
				throw new Exception("Unknown signal GC handle received " + gch);

			Gtk.RowChangedArgs args = new Gtk.RowChangedArgs ();
			args.Args = new object[2];
			if (arg1 == IntPtr.Zero)
				args.Args[0] = null;
			else {
				args.Args[0] = new Gtk.TreePath(arg1);
			}
			args.Args[1] = arg2;
			Gtk.RowChangedHandler handler = (Gtk.RowChangedHandler) sig.Handler;
			handler (GLib.Object.GetObject (arg0), args);

		}

		delegate void RowChangedVMDelegate (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter);

		static RowChangedVMDelegate RowChangedVMCallback;

		static void rowchanged_cb (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter)
		{
			HandleModel obj = GLib.Object.GetObject (tree_model, false) as HandleModel;
			obj.OnRowChanged (new Gtk.TreePath(path), iter);
		}

		private static void OverrideRowChanged (GLib.GType gtype)
		{
			if (RowChangedVMCallback == null)
				RowChangedVMCallback = new RowChangedVMDelegate (rowchanged_cb);
			OverrideVirtualMethod (gtype, "row_changed", RowChangedVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(HandleModel), ConnectionMethod="OverrideRowChanged")]
		protected virtual void OnRowChanged (Gtk.TreePath path, Gtk.TreeIter iter)
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (3);
			GLib.Value[] vals = new GLib.Value [3];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			vals [1] = new GLib.Value (path);
			inst_and_params.Append (vals [1]);
			vals [2] = new GLib.Value (iter);
			inst_and_params.Append (vals [2]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
		}

		[GLib.Signal("row_changed")]
		public event Gtk.RowChangedHandler RowChanged {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_changed", new RowChangedSignalDelegate(RowChangedSignalCallback));
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_changed", new RowChangedSignalDelegate(RowChangedSignalCallback));
				sig.RemoveDelegate (value);
			}
		}

			delegate void RowDeletedSignalDelegate (IntPtr arg0, IntPtr arg1, IntPtr gch);

		static void RowDeletedSignalCallback (IntPtr arg0, IntPtr arg1, IntPtr gch)
		{
			GLib.Signal sig = ((GCHandle) gch).Target as GLib.Signal;
			if (sig == null)
				throw new Exception("Unknown signal GC handle received " + gch);

			Gtk.RowDeletedArgs args = new Gtk.RowDeletedArgs ();
			args.Args = new object[1];
			if (arg1 == IntPtr.Zero)
				args.Args[0] = null;
			else {
				args.Args[0] = new Gtk.TreePath(arg1);
			}
			Gtk.RowDeletedHandler handler = (Gtk.RowDeletedHandler) sig.Handler;
			handler (GLib.Object.GetObject (arg0), args);

		}

		delegate void RowDeletedVMDelegate (IntPtr tree_model, IntPtr path);

		static RowDeletedVMDelegate RowDeletedVMCallback;

		static void rowdeleted_cb (IntPtr tree_model, IntPtr path)
		{
			HandleModel obj = GLib.Object.GetObject (tree_model, false) as HandleModel;
			obj.OnRowDeleted (new Gtk.TreePath(path));
		}

		private static void OverrideRowDeleted (GLib.GType gtype)
		{
			if (RowDeletedVMCallback == null)
				RowDeletedVMCallback = new RowDeletedVMDelegate (rowdeleted_cb);
			OverrideVirtualMethod (gtype, "row_deleted", RowDeletedVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(HandleModel), ConnectionMethod="OverrideRowDeleted")]
		protected virtual void OnRowDeleted (Gtk.TreePath path)
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (2);
			GLib.Value[] vals = new GLib.Value [2];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			vals [1] = new GLib.Value (path);
			inst_and_params.Append (vals [1]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
		}

		[GLib.Signal("row_deleted")]
		public event Gtk.RowDeletedHandler RowDeleted {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_deleted", new RowDeletedSignalDelegate(RowDeletedSignalCallback));
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_deleted", new RowDeletedSignalDelegate(RowDeletedSignalCallback));
				sig.RemoveDelegate (value);
			}
		}

		delegate void RowInsertedSignalDelegate (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, IntPtr gch);

		static void RowInsertedSignalCallback (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, IntPtr gch)
		{
			GLib.Signal sig = ((GCHandle) gch).Target as GLib.Signal;
			if (sig == null)
				throw new Exception("Unknown signal GC handle received " + gch);

			Gtk.RowInsertedArgs args = new Gtk.RowInsertedArgs ();
			args.Args = new object[2];
			if (arg1 == IntPtr.Zero)
				args.Args[0] = null;
			else {
				args.Args[0] = new Gtk.TreePath(arg1);
			}
			args.Args[1] = arg2;
			Gtk.RowInsertedHandler handler = (Gtk.RowInsertedHandler) sig.Handler;
			handler (GLib.Object.GetObject (arg0), args);

		}

		delegate void RowInsertedVMDelegate (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter);

		static RowInsertedVMDelegate RowInsertedVMCallback;

		static void rowinserted_cb (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter)
		{
			HandleModel obj = GLib.Object.GetObject (tree_model, false) as HandleModel;
			obj.OnRowInserted (new Gtk.TreePath(path), iter);
		}

		private static void OverrideRowInserted (GLib.GType gtype)
		{
			if (RowInsertedVMCallback == null)
				RowInsertedVMCallback = new RowInsertedVMDelegate (rowinserted_cb);
			OverrideVirtualMethod (gtype, "row_inserted", RowInsertedVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(HandleModel), ConnectionMethod="OverrideRowInserted")]
		protected virtual void OnRowInserted (Gtk.TreePath path, Gtk.TreeIter iter)
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (3);
			GLib.Value[] vals = new GLib.Value [3];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			vals [1] = new GLib.Value (path);
			inst_and_params.Append (vals [1]);
			vals [2] = new GLib.Value (iter);
			inst_and_params.Append (vals [2]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
		}

		[GLib.Signal("row_inserted")]
		public event Gtk.RowInsertedHandler RowInserted {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_inserted", new RowInsertedSignalDelegate(RowInsertedSignalCallback));
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_inserted", new RowInsertedSignalDelegate(RowInsertedSignalCallback));
				sig.RemoveDelegate (value);
			}
		}

		delegate void RowHasChildToggledSignalDelegate (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, IntPtr gch);

		static void RowHasChildToggledSignalCallback (IntPtr arg0, IntPtr arg1, ref Gtk.TreeIter arg2, IntPtr gch)
		{
			GLib.Signal sig = ((GCHandle) gch).Target as GLib.Signal;
			if (sig == null)
				throw new Exception("Unknown signal GC handle received " + gch);

			Gtk.RowHasChildToggledArgs args = new Gtk.RowHasChildToggledArgs ();
			args.Args = new object[2];
			if (arg1 == IntPtr.Zero)
				args.Args[0] = null;
			else {
				args.Args[0] = new Gtk.TreePath(arg1);
			}
			args.Args[1] = arg2;
			Gtk.RowHasChildToggledHandler handler = (Gtk.RowHasChildToggledHandler) sig.Handler;
			handler (GLib.Object.GetObject (arg0), args);

		}

		delegate void RowHasChildToggledVMDelegate (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter);

		static RowHasChildToggledVMDelegate RowHasChildToggledVMCallback;

		static void rowhaschildtoggled_cb (IntPtr tree_model, IntPtr path, ref Gtk.TreeIter iter)
		{
			HandleModel obj = GLib.Object.GetObject (tree_model, false) as HandleModel;
			obj.OnRowHasChildToggled (new Gtk.TreePath(path), iter);
		}

		private static void OverrideRowHasChildToggled (GLib.GType gtype)
		{
			if (RowHasChildToggledVMCallback == null)
				RowHasChildToggledVMCallback = new RowHasChildToggledVMDelegate (rowhaschildtoggled_cb);
			OverrideVirtualMethod (gtype, "row_has_child_toggled", RowHasChildToggledVMCallback);
		}

		[GLib.DefaultSignalHandler(Type=typeof(HandleModel), ConnectionMethod="OverrideRowHasChildToggled")]
		protected virtual void OnRowHasChildToggled (Gtk.TreePath path, Gtk.TreeIter iter)
		{
			GLib.Value ret = GLib.Value.Empty;
			GLib.ValueArray inst_and_params = new GLib.ValueArray (3);
			GLib.Value[] vals = new GLib.Value [3];
			vals [0] = new GLib.Value (this);
			inst_and_params.Append (vals [0]);
			vals [1] = new GLib.Value (path);
			inst_and_params.Append (vals [1]);
			vals [2] = new GLib.Value (iter);
			inst_and_params.Append (vals [2]);
			g_signal_chain_from_overridden (inst_and_params.ArrayPtr, ref ret);
		}

		[GLib.Signal("row_has_child_toggled")]
		public event Gtk.RowHasChildToggledHandler RowHasChildToggled {
			add {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_has_child_toggled", new RowHasChildToggledSignalDelegate(RowHasChildToggledSignalCallback));
				sig.AddDelegate (value);
			}
			remove {
				GLib.Signal sig = GLib.Signal.Lookup (this, "row_has_child_toggled", new RowHasChildToggledSignalDelegate(RowHasChildToggledSignalCallback));
				sig.RemoveDelegate (value);
			}
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_drag_source_drag_data_delete(IntPtr raw, IntPtr path);

		public bool DragDataDelete(Gtk.TreePath path) {
			bool raw_ret = gtk_tree_drag_source_drag_data_delete(Handle, path.Handle);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_drag_source_row_draggable(IntPtr raw, IntPtr path);

		public bool RowDraggable(Gtk.TreePath path) {
			bool raw_ret = gtk_tree_drag_source_row_draggable(Handle, path.Handle);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_drag_source_drag_data_get(IntPtr raw, IntPtr path, IntPtr selection_data);

		public bool DragDataGet(Gtk.TreePath path, Gtk.SelectionData selection_data) {
			bool raw_ret = gtk_tree_drag_source_drag_data_get(Handle, path.Handle, selection_data.Handle);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_drag_dest_drag_data_received(IntPtr raw, IntPtr dest, IntPtr selection_data);

		public bool DragDataReceived(Gtk.TreePath dest, Gtk.SelectionData selection_data) {
			bool raw_ret = gtk_tree_drag_dest_drag_data_received(Handle, dest.Handle, selection_data.Handle);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_drag_dest_row_drop_possible(IntPtr raw, IntPtr dest_path, IntPtr selection_data);

		public bool RowDropPossible(Gtk.TreePath dest_path, Gtk.SelectionData selection_data) {
			bool raw_ret = gtk_tree_drag_dest_row_drop_possible(Handle, dest_path.Handle, selection_data.Handle);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_iter_children (IntPtr raw, out Gtk.TreeIter iter, IntPtr parent);
		public bool IterChildren (out Gtk.TreeIter iter) {
			bool raw_ret = gtk_tree_model_iter_children (Handle, out iter, IntPtr.Zero);
			bool ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern int gtk_tree_model_iter_n_children (IntPtr raw, IntPtr iter);
		public int IterNChildren () {
			int raw_ret = gtk_tree_model_iter_n_children (Handle, IntPtr.Zero);
			int ret = raw_ret;
			return ret;
		}

		[DllImport("libgtk-2.0-0.dll")]
		static extern bool gtk_tree_model_iter_nth_child (IntPtr raw, out Gtk.TreeIter iter, IntPtr parent, int n);
		public bool IterNthChild (out Gtk.TreeIter iter, int n) {
			bool raw_ret = gtk_tree_model_iter_nth_child (Handle, out iter, IntPtr.Zero, n);
			bool ret = raw_ret;
			return ret;
		}

		public void SetValue (Gtk.TreeIter iter, int column, bool value) {
			SetValue (iter, column, new GLib.Value (value));
		}

		public void SetValue (Gtk.TreeIter iter, int column, double value) {
			SetValue (iter, column, new GLib.Value (value));
		}

		public void SetValue (Gtk.TreeIter iter, int column, int value) {
			SetValue (iter, column, new GLib.Value (value));
		}

		public void SetValue (Gtk.TreeIter iter, int column, string value) {
			SetValue (iter, column, new GLib.Value (value));
		}

		public void SetValue (Gtk.TreeIter iter, int column, float value) {
			SetValue (iter, column, new GLib.Value (value));
		}

		public void SetValue (Gtk.TreeIter iter, int column, uint value) {
			SetValue (iter, column, new GLib.Value (value));
		}
		
		public void SetValue (Gtk.TreeIter iter, int column, object value) {
			GLib.Value val = new GLib.Value (value);
			SetValue (iter, column, val);
			val.Dispose ();
		}

		public object GetValue(Gtk.TreeIter iter, int column) {
			GLib.Value val = GLib.Value.Empty;
			GetValue (iter, column, ref val);
			object ret = val.Val;
			val.Dispose ();
			return ret;
		}
	}
}

namespace GtkSharp
{
	internal delegate bool TreeModelForeachFuncNative(IntPtr model, IntPtr path, ref Gtk.TreeIter iter, IntPtr data);

	internal class TreeModelForeachFuncWrapper : GLib.DelegateWrapper {

		public bool NativeCallback (IntPtr model, IntPtr path, ref Gtk.TreeIter iter, IntPtr data)
		{
			Gtk.TreeModel _arg0 = (Gtk.TreeModel) GLib.Object.GetObject(model);
			Gtk.TreePath _arg1 = new Gtk.TreePath(path);
			Gtk.TreeIter _arg2 = iter;
			return (bool) _managed ( _arg0,  _arg1,  _arg2);
		}

		internal TreeModelForeachFuncNative NativeDelegate;
		protected Gtk.TreeModelForeachFunc _managed;

		public TreeModelForeachFuncWrapper (Gtk.TreeModelForeachFunc managed, object o) : base (o)
		{
			NativeDelegate = new TreeModelForeachFuncNative (NativeCallback);
			_managed = managed;
		}
	}
}
