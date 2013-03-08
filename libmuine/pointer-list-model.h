/* -*- mode: C; c-file-style: "gnu" -*- */
/*
 * Copyright (C) 2003 Richard Hult <richard@imendio.com>
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

#ifndef __POINTER_LIST_MODEL_H__
#define __POINTER_LIST_MODEL_H__

#include <gtk/gtk.h>
#include <glib.h>

#define TYPE_POINTER_LIST_MODEL		 (pointer_list_model_get_type ())
#define POINTER_LIST_MODEL(obj)		 (G_TYPE_CHECK_INSTANCE_CAST ((obj), TYPE_POINTER_LIST_MODEL, PointerListModel))
#define POINTER_LIST_MODEL_CLASS(klass)	 (G_TYPE_CHECK_CLASS_CAST ((klass), TYPE_POINTER_LIST_MODEL, PointerListModelClass))
#define IS_POINTER_LIST_MODEL(obj)	  (G_TYPE_CHECK_INSTANCE_TYPE ((obj), TYPE_POINTER_LIST_MODEL))
#define IS_POINTER_LIST_MODEL_CLASS(klass)  (G_TYPE_CHECK_CLASS_TYPE ((obj), TYPE_POINTER_LIST_MODEL))
#define POINTER_LIST_MODEL_GET_CLASS(obj)   (G_TYPE_INSTANCE_GET_CLASS ((obj), TYPE_POINTER_LIST_MODEL, PointerListModelClass))

typedef struct _PointerListModel PointerListModel;
typedef struct _PointerListModelClass PointerListModelClass;

struct _PointerListModel {
  GObject          parent_instance;
  
  int              stamp;

  GCompareFunc     sort_func;

  GSequenceIter   *current_pointer;

  GSequence       *pointers;
  GHashTable      *reverse_map;
};

struct _PointerListModelClass {
  GObjectClass parent_class;
};


GType         pointer_list_model_get_type       (void);
GtkTreeModel *pointer_list_model_new            (void);
gboolean      pointer_list_model_add            (PointerListModel *model,
					         gpointer          pointer);
gboolean      pointer_list_model_insert         (PointerListModel *model,
					         gpointer          pointer,
						 gpointer          ins,
						 GtkTreeViewDropPosition pos);
void          pointer_list_model_remove         (PointerListModel *model,
					         gpointer          pointer);
void          pointer_list_model_remove_iter    (PointerListModel *model,
					         GtkTreeIter      *iter);
void          pointer_list_model_clear          (PointerListModel *model);
void          pointer_list_model_set_sorting    (PointerListModel *model,
					         GCompareFunc      func);
void          pointer_list_model_sort           (PointerListModel *model,
                                                 GCompareDataFunc  sort_func);
gboolean      pointer_list_model_pointer_get_iter (PointerListModel *model,
					         gpointer          pointer,
					         GtkTreeIter      *iter);
gpointer      pointer_list_model_iter_get_pointer (PointerListModel *model,
					         GtkTreeIter      *iter);
GList *       pointer_list_model_get_pointers   (PointerListModel *model);
gboolean      pointer_list_model_contains       (PointerListModel *model,
						 gpointer          pointer);
void          pointer_list_model_remove_delta   (PointerListModel *model,
					         GList            *pointers);
gpointer      pointer_list_model_get_current    (PointerListModel *model);
gboolean      pointer_list_model_set_current    (PointerListModel *model,
					         gpointer          pointer);
gpointer      pointer_list_model_next           (PointerListModel *model);
gpointer      pointer_list_model_prev           (PointerListModel *model);
gpointer      pointer_list_model_first          (PointerListModel *model);
gpointer      pointer_list_model_last           (PointerListModel *model);
gboolean      pointer_list_model_has_next       (PointerListModel *model);
gboolean      pointer_list_model_has_prev       (PointerListModel *model);
gboolean      pointer_list_model_has_first      (PointerListModel *model);

#endif /* __POINTER_LIST_MODEL_H__ */

