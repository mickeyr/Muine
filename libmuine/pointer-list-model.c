/* -*- mode: C; c-file-style: "gnu" -*- */
/*
 * Copyright (C) 2003 Richard Hult <richard@imendio.com>
 * Copyright (C) 2003 Johan Dahlin <johan@gnome.org>
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
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

#include <config.h>

#include <string.h>

#include <gtk/gtk.h>

#include "pointer-list-model.h"
#include "macros.h"

static GtkTreeModelFlags
pointer_list_model_get_flags (GtkTreeModel *UNUSED(tree_model))
{
  return GTK_TREE_MODEL_ITERS_PERSIST | GTK_TREE_MODEL_LIST_ONLY;
}

static int
pointer_list_model_get_n_columns (GtkTreeModel *UNUSED(tree_model))
{
  return 1;
}

static GType
pointer_list_model_get_column_type (GtkTreeModel *UNUSED(tree_model), int index)
{
  switch (index) {
  case 0:
    return G_TYPE_POINTER;
  default:
    return G_TYPE_INVALID;
  }
}

static gboolean
pointer_list_model_get_iter (GtkTreeModel *tree_model,
			  GtkTreeIter  *iter,
			  GtkTreePath  *path)
{
  PointerListModel *model;
  GSequenceIter *ptr;
  int i;

  model = (PointerListModel *) tree_model;
  
  i = gtk_tree_path_get_indices (path)[0];

  if (i >= g_sequence_get_length (model->pointers))
    return FALSE;

  ptr = g_sequence_get_iter_at_pos (model->pointers, i);

  iter->stamp = model->stamp;
  iter->user_data = ptr;

  return TRUE;
}

static GtkTreePath *
pointer_list_model_get_path (GtkTreeModel *tree_model,
			  GtkTreeIter  *iter)
{
  PointerListModel *model;
  GtkTreePath *path;
  
  model = (PointerListModel *) tree_model;

  g_return_val_if_fail (model->stamp == iter->stamp, NULL);

  if (g_sequence_iter_is_end (iter->user_data))
    return NULL;
  
  path = gtk_tree_path_new ();
  gtk_tree_path_append_index (path, g_sequence_iter_get_position (iter->user_data));

  return path;
}

static void
pointer_list_model_get_value (GtkTreeModel *tree_model,
			   GtkTreeIter  *iter,
			   int           column,
			   GValue       *value)
{
  PointerListModel *model;
  gpointer val;
   
  model = (PointerListModel *) tree_model;

  g_return_if_fail (model->stamp == iter->stamp);

  val = g_sequence_get (iter->user_data);
  
  switch (column)
    {
    case 0:
      g_value_init (value, G_TYPE_POINTER);
      g_value_set_pointer (value, val);
      break;

    default:
      g_assert_not_reached ();
      break;
    }
}

static gboolean
pointer_list_model_iter_nth_child (GtkTreeModel *tree_model,
				GtkTreeIter  *iter,
				GtkTreeIter  *parent,
				int           n)
{
  PointerListModel *model;
  GSequenceIter *child;

  if (parent)
    return FALSE;

  model = (PointerListModel *) tree_model;

  if (n >= g_sequence_get_length (model->pointers))
    return FALSE;

  child = g_sequence_get_iter_at_pos (model->pointers, n);

  iter->stamp = model->stamp;
  iter->user_data = child;

  return TRUE;
}

static gboolean
pointer_list_model_iter_next (GtkTreeModel *tree_model,
			   GtkTreeIter  *iter)
{
  PointerListModel *model;
  
  model = (PointerListModel *) tree_model;

  g_return_val_if_fail (model->stamp == iter->stamp, FALSE);

  iter->user_data = g_sequence_iter_next (iter->user_data);

  return !g_sequence_iter_is_end (iter->user_data);
}

static gboolean
pointer_list_model_iter_children (GtkTreeModel *tree_model,
			       GtkTreeIter  *iter,
			       GtkTreeIter  *parent)
{
  PointerListModel *model;
  
  if (parent)
    return FALSE;
  
  model = (PointerListModel *) tree_model;
  
  if (g_sequence_get_length (model->pointers) == 0)
    return FALSE;

  iter->stamp = model->stamp;
  iter->user_data = g_sequence_get_begin_iter (model->pointers);
  
  return TRUE;
}

static int
pointer_list_model_iter_n_children (GtkTreeModel *tree_model,
				 GtkTreeIter  *iter)
{
  PointerListModel *model;
  
  model = (PointerListModel *) tree_model;
  
  if (iter == NULL)
    return g_sequence_get_length (model->pointers);

  g_return_val_if_fail (model->stamp == iter->stamp, -1);

  return 0;
}

static gboolean
pointer_list_model_iter_parent (GtkTreeModel *UNUSED(tree_model),
			     GtkTreeIter  *UNUSED(iter),
			     GtkTreeIter  *UNUSED(child))
{
  return FALSE;
}

static gboolean
pointer_list_model_iter_has_child (GtkTreeModel *UNUSED(tree_model),
				GtkTreeIter  *UNUSED(iter))
{
  return FALSE;
}

static gboolean
pointer_list_model_row_draggable (GtkTreeDragSource *drag_source,
                                GtkTreePath       *UNUSED(path))
{
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (drag_source), FALSE);

  return TRUE;
}
  
static gboolean
pointer_list_model_drag_data_delete (GtkTreeDragSource *UNUSED(drag_source),
                                   GtkTreePath       *UNUSED(path))
{
  return FALSE;
}

static gboolean
pointer_list_model_drag_data_get (GtkTreeDragSource *drag_source,
                                GtkTreePath       *path,
                                GtkSelectionData  *selection_data)
{
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (drag_source), FALSE);

  /* Note that we don't need to handle the GTK_TREE_MODEL_ROW
   * target, because the default handler does it for us, but
   * we do anyway for the convenience of someone maybe overriding the
   * default handler.
   */

  if (gtk_tree_set_row_drag_data (selection_data,
				  GTK_TREE_MODEL (drag_source),
				  path))
    {
      return TRUE;
    }

  return FALSE;
}

static gboolean
pointer_list_model_drag_data_received (GtkTreeDragDest   *UNUSED(drag_dest),
                                       GtkTreePath       *UNUSED(dest),
                                       GtkSelectionData  *UNUSED(selection_data))
{
  return FALSE;
}

static gboolean
pointer_list_model_row_drop_possible (GtkTreeDragDest  *drag_dest,
                                    GtkTreePath      *dest_path,
				    GtkSelectionData *selection_data)
{
  gint *indices;
  GtkTreeModel *src_model = NULL;
  GtkTreePath *src_path = NULL;
  gboolean retval = FALSE;
  PointerListModel *model;

  g_return_val_if_fail (IS_POINTER_LIST_MODEL (drag_dest), FALSE);

  model = POINTER_LIST_MODEL (drag_dest);

  /* don't accept drops if the list has been sorted */
  if (model->sort_func != NULL)
    return FALSE;

  if (!gtk_tree_get_row_drag_data (selection_data,
				   &src_model,
				   &src_path))
    goto out;

  if (src_model != GTK_TREE_MODEL (drag_dest))
    goto out;

  if (gtk_tree_path_get_depth (dest_path) != 1)
    goto out;

  /* can drop before any existing node, or before one past any existing. */

  indices = gtk_tree_path_get_indices (dest_path);

  if (indices[0] <= g_sequence_get_length (model->pointers))
    retval = TRUE;

 out:
  if (src_path)
    gtk_tree_path_free (src_path);
  
  return retval;
}

static void
pointer_list_model_tree_model_init (GtkTreeModelIface *iface)
{
  iface->get_flags = pointer_list_model_get_flags;
  iface->get_n_columns = pointer_list_model_get_n_columns;
  iface->get_column_type = pointer_list_model_get_column_type;
  iface->get_iter = pointer_list_model_get_iter;
  iface->get_path = pointer_list_model_get_path;
  iface->get_value = pointer_list_model_get_value;
  iface->iter_nth_child = pointer_list_model_iter_nth_child;
  iface->iter_next = pointer_list_model_iter_next;
  iface->iter_has_child = pointer_list_model_iter_has_child;
  iface->iter_n_children = pointer_list_model_iter_n_children;
  iface->iter_children = pointer_list_model_iter_children;
  iface->iter_parent = pointer_list_model_iter_parent;
}

static void
pointer_list_model_drag_source_init (GtkTreeDragSourceIface *iface)
{
  iface->row_draggable = pointer_list_model_row_draggable;
  iface->drag_data_delete = pointer_list_model_drag_data_delete;
  iface->drag_data_get = pointer_list_model_drag_data_get;
}

static void
pointer_list_model_drag_dest_init (GtkTreeDragDestIface *iface)
{
  iface->drag_data_received = pointer_list_model_drag_data_received;
  iface->row_drop_possible = pointer_list_model_row_drop_possible;
}

static void
pointer_list_model_class_init (PointerListModelClass *UNUSED(klass))
{
}

static void
pointer_list_model_init (PointerListModel *model)
{
  model->pointers = g_sequence_new (NULL);
  model->reverse_map = g_hash_table_new (NULL, NULL);
  model->stamp = g_random_int ();
  model->sort_func = NULL;

  model->current_pointer = NULL;
}

GType
pointer_list_model_get_type (void)
{
  static GType type = 0;

  if (!type)
    {
      static const GTypeInfo object_info = {
	sizeof (PointerListModelClass),
	NULL,		/* base_init */
	NULL,		/* base_finalize */
	(GClassInitFunc) pointer_list_model_class_init,
	NULL,		/* class_finalize */
	NULL,		/* class_data */
	sizeof (PointerListModel),
	0,              /* n_preallocs */
	(GInstanceInitFunc) pointer_list_model_init,
	NULL
      };
	    
      static const GInterfaceInfo tree_model_info = {
	(GInterfaceInitFunc) pointer_list_model_tree_model_init,
	NULL,
	NULL
      };

      static const GInterfaceInfo drag_source_info =
      {
	(GInterfaceInitFunc) pointer_list_model_drag_source_init,
	NULL,
	NULL
      };

      static const GInterfaceInfo drag_dest_info =
      {
	(GInterfaceInitFunc) pointer_list_model_drag_dest_init,
	NULL,
	NULL
      };
	    
      type = g_type_register_static (G_TYPE_OBJECT,
				     "PointerListModel",
				     &object_info, 0);
	    
      g_type_add_interface_static (type,
				   GTK_TYPE_TREE_MODEL,
				   &tree_model_info);
      g_type_add_interface_static (type,
				   GTK_TYPE_TREE_DRAG_SOURCE,
				   &drag_source_info);
      g_type_add_interface_static (type,
				   GTK_TYPE_TREE_DRAG_DEST,
				   &drag_dest_info);
    }
  
  return type;
}

GtkTreeModel *
pointer_list_model_new (void)
{
  return g_object_new (TYPE_POINTER_LIST_MODEL, NULL);
}

gboolean
pointer_list_model_add (PointerListModel *model, gpointer pointer)
{
  GtkTreeIter iter;
  GtkTreePath *path;
  GSequenceIter *new_ptr;

  if (g_hash_table_lookup (model->reverse_map, pointer))
    return FALSE;
  
  if (model->sort_func != NULL)
    new_ptr = g_sequence_insert_sorted (model->pointers, pointer,
  				        (GCompareDataFunc) model->sort_func,
				        NULL);
  else
    new_ptr = g_sequence_append (model->pointers, pointer);
  
  g_hash_table_insert (model->reverse_map, pointer, new_ptr);
	
  iter.stamp = model->stamp;
  iter.user_data = new_ptr;
  
  path = gtk_tree_model_get_path (GTK_TREE_MODEL (model), &iter);
  gtk_tree_model_row_inserted (GTK_TREE_MODEL (model), path, &iter);
  gtk_tree_path_free (path);

  return TRUE;
}

gboolean
pointer_list_model_insert (PointerListModel *model, gpointer pointer,
			   gpointer ins, GtkTreeViewDropPosition pos)
{
  GtkTreeIter iter;
  GtkTreePath *path;
  GSequenceIter *new_ptr, *before_ptr;
  gboolean is_end;

  if (g_hash_table_lookup (model->reverse_map, pointer))
    return FALSE;

  before_ptr = g_hash_table_lookup (model->reverse_map, ins);
  g_assert (before_ptr != NULL);

  is_end = g_sequence_iter_is_end (g_sequence_iter_next (before_ptr));

  new_ptr = g_sequence_append (model->pointers, pointer);

  switch (pos) {
    case GTK_TREE_VIEW_DROP_BEFORE:
    case GTK_TREE_VIEW_DROP_INTO_OR_BEFORE:
      break;
    case GTK_TREE_VIEW_DROP_AFTER:
    case GTK_TREE_VIEW_DROP_INTO_OR_AFTER:
      if (!is_end)
        before_ptr = g_sequence_iter_next (before_ptr);
      else
        before_ptr = NULL;

      break;
  }

  if (before_ptr != NULL)
    g_sequence_move (new_ptr, before_ptr);

  g_hash_table_insert (model->reverse_map, pointer, new_ptr);

  iter.stamp = model->stamp;
  iter.user_data = new_ptr;

  path = gtk_tree_model_get_path (GTK_TREE_MODEL (model), &iter);
  gtk_tree_model_row_inserted (GTK_TREE_MODEL (model), path, &iter);
  gtk_tree_path_free (path);

  return TRUE;
}

void
pointer_list_model_remove_iter (PointerListModel *model, GtkTreeIter *iter)
{
  GSequenceIter *ptr;
  GtkTreePath *path;
  
  path = gtk_tree_model_get_path (GTK_TREE_MODEL (model), iter);
  ptr = iter->user_data;

  if (ptr == model->current_pointer)
    model->current_pointer = NULL;

  g_hash_table_remove (model->reverse_map, g_sequence_get (ptr));
  g_sequence_remove (ptr);
  
  model->stamp++;

  gtk_tree_model_row_deleted (GTK_TREE_MODEL (model), path);
  gtk_tree_path_free (path);
}

void
pointer_list_model_remove (PointerListModel *model, gpointer pointer)
{
  GtkTreeIter iter;
  
  if (pointer_list_model_pointer_get_iter (model, pointer, &iter))
    pointer_list_model_remove_iter (model, &iter);
}

void
pointer_list_model_clear (PointerListModel *model)
{
  GtkTreeIter iter;
  
  g_return_if_fail (model != NULL);

  model->current_pointer = NULL;
  
  while (g_sequence_get_length (model->pointers) > 0)
    {
      iter.stamp = model->stamp;
      iter.user_data = g_sequence_get_begin_iter (model->pointers);
      pointer_list_model_remove_iter (model, &iter);
    }
}

void
pointer_list_model_sort (PointerListModel *model, GCompareDataFunc sort_func)
{
  GSequence *pointers;
  GSequenceIter **old_order;
  GtkTreePath *path;
  int *new_order;
  int length;
  int i;

  pointers = model->pointers;
  length = g_sequence_get_length (pointers);

  if (length <= 1)
    return;
  
  /* Generate old order of GSequenceIters. */
  old_order = g_new (GSequenceIter *, length);
  for (i = 0; i < length; ++i)
    old_order[i] = g_sequence_get_iter_at_pos (pointers, i);

  g_sequence_sort (pointers, sort_func, NULL);

  /* Generate new order. */
  new_order = g_new (int, length);
  for (i = 0; i < length; ++i)
    new_order[i] = g_sequence_iter_get_position (old_order[i]);

  path = gtk_tree_path_new ();
  
  gtk_tree_model_rows_reordered (GTK_TREE_MODEL (model), path, NULL, new_order);
  
  gtk_tree_path_free (path);
  g_free (old_order);
  g_free (new_order);
}

void
pointer_list_model_set_sorting (PointerListModel  *model,
			        GCompareFunc sort_func)
{
  if (sort_func == model->sort_func)
    return;

  model->sort_func = sort_func;

  pointer_list_model_sort (model, (GCompareDataFunc) sort_func);
}

gboolean
pointer_list_model_pointer_get_iter (PointerListModel *model,
			       gpointer pointer,
			       GtkTreeIter   *iter)
{
  GSequenceIter *ptr;

  ptr = g_hash_table_lookup (model->reverse_map, pointer);
  if (!ptr)
    return FALSE;
  
  if (iter != NULL)
    {
      iter->stamp = model->stamp;
      iter->user_data = ptr;
    }
  
  return TRUE;
}

gpointer
pointer_list_model_iter_get_pointer (PointerListModel *model, GtkTreeIter *iter)
{
  g_return_val_if_fail (model->stamp == iter->stamp, NULL);
  
  return g_sequence_get (iter->user_data);
}

GList *
pointer_list_model_get_pointers (PointerListModel *model)
{
  GList *list = NULL;
  GSequenceIter *ptr;

  ptr = g_sequence_get_begin_iter (model->pointers);
  while (!g_sequence_iter_is_end (ptr))
    {
      list = g_list_prepend (list, g_sequence_get (ptr));
      ptr = g_sequence_iter_next (ptr);
    }

  return g_list_reverse (list);
}

gboolean
pointer_list_model_contains (PointerListModel *model,
			     gpointer          pointer)
{
  return (g_hash_table_lookup (model->reverse_map, pointer) != NULL);
}

static void
remove_ptr (PointerListModel *model, GSequenceIter *ptr)
{
  GtkTreePath *path;
  
  path = gtk_tree_path_new ();
  gtk_tree_path_append_index (path, g_sequence_iter_get_position (ptr));

  if (ptr == model->current_pointer)
    model->current_pointer = NULL;
  
  g_hash_table_remove (model->reverse_map, g_sequence_get (ptr));
  
  g_sequence_remove (ptr);

  model->stamp++;
  
  gtk_tree_model_row_deleted (GTK_TREE_MODEL (model), path);
  gtk_tree_path_free (path);
}

void
pointer_list_model_remove_delta (PointerListModel *model, GList *pointers)
{
  GHashTable *hash;
  GList *l, *remove = NULL;
  gpointer pointer;
  GSequenceIter *ptr;
  
  if (g_sequence_get_length (model->pointers) == 0)
    return;

  if (!pointers)
    {
      pointer_list_model_clear (model);
      return;
    }

  hash = g_hash_table_new (NULL, NULL);
 
  for (l = pointers; l; l = l->next)
    g_hash_table_insert (hash, l->data, GINT_TO_POINTER (TRUE));
 
  ptr = g_sequence_get_begin_iter (model->pointers);
  while (!g_sequence_iter_is_end (ptr))
    {
      pointer = g_sequence_get (ptr);
      if (!g_hash_table_lookup (hash, pointer))
	remove = g_list_prepend (remove, ptr);
      
      ptr = g_sequence_iter_next (ptr);
    }

  for (l = remove; l; l = l->next)
    remove_ptr (model, l->data);

  g_list_free (remove);
  g_hash_table_destroy (hash);
}

gpointer
pointer_list_model_get_current (PointerListModel *model)
{
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), NULL);

  if (g_sequence_get_length (model->pointers) == 0)
    return NULL;

  if (model->current_pointer == NULL)
    return NULL;
  
  return g_sequence_get (model->current_pointer);
}

static void
row_changed (PointerListModel *model, GSequenceIter *ptr)
{
  GtkTreeIter iter;
  GtkTreePath *path;

  if (ptr == NULL)
	  return;

  iter.stamp = model->stamp;
  iter.user_data = ptr;

  path = pointer_list_model_get_path ((GtkTreeModel *) model, &iter);

  gtk_tree_model_row_changed ((GtkTreeModel *) model, path, &iter);

  gtk_tree_path_free (path);
}

gboolean
pointer_list_model_set_current (PointerListModel *model, gpointer pointer)
{
  GSequenceIter *ptr;
  int len;
  
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), FALSE);

  row_changed (model, model->current_pointer);

  if (!pointer)
    {
      model->current_pointer = NULL;
      return TRUE;
    }
  
  len = g_sequence_get_length (model->pointers);
  
  if (len == 0)
    return FALSE;
  
  ptr = g_hash_table_lookup (model->reverse_map, pointer);
  if (!ptr)
    return FALSE;

  model->current_pointer = ptr;

  row_changed (model, model->current_pointer);

  return TRUE;
}

gpointer
pointer_list_model_next (PointerListModel *model)
{
  GSequenceIter *ptr;
  
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), NULL);

  ptr = g_sequence_iter_next (model->current_pointer);
  if (g_sequence_iter_is_end (ptr))
    return NULL;

  if (ptr)
    {
      row_changed (model, model->current_pointer);
      model->current_pointer = ptr;
      row_changed (model, model->current_pointer);
    }

  return g_sequence_get (model->current_pointer);
}

gpointer
pointer_list_model_prev (PointerListModel *model)
{
  GSequenceIter *ptr;
  
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), NULL);

  if (!pointer_list_model_has_prev (model))
    return NULL;

  ptr = g_sequence_iter_prev (model->current_pointer);
  if (ptr)
    {
      row_changed (model, model->current_pointer);
      model->current_pointer = ptr;
      row_changed (model, model->current_pointer);
    }

  return g_sequence_get (model->current_pointer);
}

gpointer
pointer_list_model_first (PointerListModel *model)
{
  GSequenceIter *ptr;
  
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), NULL);

  if (g_sequence_get_length (model->pointers) == 0)
    return NULL;
  
  ptr = g_sequence_get_begin_iter (model->pointers);
  if (ptr)
    {
      row_changed (model, model->current_pointer);
      model->current_pointer = ptr;
      row_changed (model, model->current_pointer);
    }

  return g_sequence_get (model->current_pointer);
}

gpointer
pointer_list_model_last (PointerListModel *model)
{
  GSequenceIter *ptr;
  
  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), NULL);

  if (g_sequence_get_length (model->pointers) == 0)
    return NULL;
  
  ptr = g_sequence_get_end_iter (model->pointers);
  if (ptr)
    ptr = g_sequence_iter_prev (ptr);
  if (ptr)
    {
      row_changed (model, model->current_pointer);
      model->current_pointer = ptr;
      row_changed (model, model->current_pointer);
    }

  return g_sequence_get (model->current_pointer);
}

gboolean
pointer_list_model_has_prev (PointerListModel *model)
{
  int len;

  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), FALSE);

  len = g_sequence_get_length (model->pointers);

  if (len == 0)
    return FALSE;

  if (!model->current_pointer)
    return FALSE;

  return (g_sequence_iter_get_position (model->current_pointer) > 0);
}

gboolean
pointer_list_model_has_next (PointerListModel *model)
{
  int len;
  GSequenceIter *ptr;

  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), FALSE);

  len = g_sequence_get_length (model->pointers);

  if (len == 0)
    return FALSE;

  if (!model->current_pointer)
    return FALSE;

  ptr = g_sequence_iter_next (model->current_pointer);

  return !g_sequence_iter_is_end (ptr);
}

gboolean
pointer_list_model_has_first (PointerListModel *model)
{
  int len;

  g_return_val_if_fail (IS_POINTER_LIST_MODEL (model), FALSE);

  len = g_sequence_get_length (model->pointers);

  return (len > 0);
}
