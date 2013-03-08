/*
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

#ifndef __DB_H
#define __DB_H

#include <gdk-pixbuf/gdk-pixbuf.h>
#include <glib.h>

typedef void (*ForeachDecodeFunc) (const char *key,
				   gpointer data,
				   gpointer user_data);

gpointer db_open          (const char *filename,
			   int version,
	                   char **error_message_return);
int      db_get_version   (gpointer db);
void     db_set_version   (gpointer db,
			   int version);
gboolean db_exists        (gpointer db,
	                   const char *key_str);
void     db_delete        (gpointer db,
	                   const char *key_str);
void     db_store         (gpointer db,
	                   const char *key_str,
	                   gboolean overwrite,
	                   gpointer data,
	                   int data_size);
void     db_foreach       (gpointer db,
	                   ForeachDecodeFunc func,
	                   gpointer user_data);

gpointer db_unpack_string (gpointer p, char **str);
gpointer db_unpack_int    (gpointer p, int *val);
gpointer db_unpack_bool   (gpointer p, gboolean *val);
gpointer db_unpack_double (gpointer p, double *val);
gpointer db_unpack_pixbuf (gpointer p, GdkPixbuf **pixbuf);

gpointer db_pack_start    (void);
void     db_pack_string   (gpointer p, const char *str);
void     db_pack_int      (gpointer p, int val);
void     db_pack_bool     (gpointer p, gboolean val);
void     db_pack_double   (gpointer p, double val);
void	 db_pack_pixbuf   (gpointer p, GdkPixbuf *pixbuf);
gpointer db_pack_end      (gpointer p, int *len);

#endif /* __DB_H */
