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
 *
 * Some code stolen from song-db.c from Jamboree.
 */

#include <gdk-pixbuf/gdk-pixdata.h>
#include <glib.h>
#include <gdbm.h>
#include <string.h>
#include <stdlib.h>

#include "db.h"

#define _ALIGN_VALUE(this, boundary) \
  (( ((unsigned long)(this)) + (((unsigned long)(boundary)) -1)) & (~(((unsigned long)(boundary))-1)))
#define _ALIGN_ADDRESS(this, boundary) \
  ((void*)_ALIGN_VALUE(this, boundary))

#define VERSION_KEY "version"

gpointer
db_open (const char *filename,
	 int version,
	 char **error_message_return)
{
	GDBM_FILE db = gdbm_open ((char *) filename, 4096,
				  GDBM_NOLOCK | GDBM_WRITER | GDBM_SYNC,
				  04644, NULL);

	if (db != NULL && db_get_version (db) != version) {
		gdbm_close (db);
		db = NULL;
	}

	if (db == NULL) {
		db = gdbm_open ((char *) filename, 4096,
				GDBM_NOLOCK | GDBM_NEWDB | GDBM_SYNC,
				04644, NULL);

		if (db != NULL)
			db_set_version (db, version);
	}

	if (db == NULL) {
		*error_message_return = gdbm_strerror (gdbm_errno);
	} else {
		*error_message_return = NULL;
	}

	return (gpointer) db;
}

int
db_get_version (gpointer db)
{
	datum key, data;
	int ret;

	memset (&key, 0, sizeof (key));
	key.dptr = VERSION_KEY;
	key.dsize = strlen (key.dptr);

	data = gdbm_fetch ((GDBM_FILE) db, key);
	if (!data.dptr)
		return -1;

	db_unpack_int (data.dptr, &ret);

	free (data.dptr);

	return ret;
}

void
db_set_version (gpointer db,
		int version)
{
	GString *string;
	datum key, data;

	memset (&key, 0, sizeof (key));
	key.dptr = VERSION_KEY;
	key.dsize = strlen (key.dptr);

	string = db_pack_start ();
	db_pack_int (string, version);

	memset (&data, 0, sizeof (data));
	data.dptr = db_pack_end (string, &data.dsize);

	gdbm_store ((GDBM_FILE) db, key, data, GDBM_REPLACE);

	g_free (data.dptr);
}

gboolean
db_exists (gpointer db,
	   const char *key_str)
{
	datum key;

	memset (&key, 0, sizeof (key));
	key.dptr = (gpointer) key_str;
	key.dsize = strlen (key_str);

	return gdbm_exists ((GDBM_FILE) db, key);
}

void
db_delete (gpointer db,
	   const char *key_str)
{
	datum key;

	memset (&key, 0, sizeof (key));
	key.dptr = (gpointer) key_str;
	key.dsize = strlen (key_str);

	gdbm_delete ((GDBM_FILE) db, key);
}

void
db_store (gpointer db,
	  const char *key_str,
	  gboolean overwrite,
	  gpointer data,
	  int data_size)
{
	datum key, datum;

	memset (&key, 0, sizeof (key));
	key.dptr = (gpointer) key_str;
	key.dsize = strlen (key_str);

	memset (&datum, 0, sizeof (datum));
	datum.dptr = data;
	datum.dsize = data_size;

	gdbm_store ((GDBM_FILE) db, key, datum,
		    overwrite ? GDBM_REPLACE : GDBM_INSERT);

	g_free (datum.dptr);
}

void
db_foreach (gpointer db,
	    ForeachDecodeFunc func,
	    gpointer user_data)
{
	datum key, data, next_key;
	char *keystr;

	key = gdbm_firstkey ((GDBM_FILE) db);
	while (key.dptr) {
		if (((char *) key.dptr)[0] == VERSION_KEY[0] && key.dsize == strlen (VERSION_KEY))
			goto done;

		data = gdbm_fetch ((GDBM_FILE) db, key);

		if (data.dptr == NULL)
			goto done;

		keystr = g_strndup (key.dptr, key.dsize);
		if (strcmp (keystr, VERSION_KEY) != 0)
			func ((const char *) keystr, (gpointer) data.dptr, user_data);
		g_free (keystr);

		free (data.dptr);

done:
		next_key = gdbm_nextkey ((GDBM_FILE) db, key);

		free (key.dptr);

		key = next_key;
	}
}

gpointer
db_unpack_string (gpointer p, char **str)
{
	int len;

	p = _ALIGN_ADDRESS (p, 4);

	len = *(int *) p;

	if (str)
		*str = g_malloc (len + 1);

	p = (gpointer) ((unsigned long) p + 4);

	if (str) {
		memcpy (*str, p, len);
		(*str)[len] = 0;
	}

	return (gpointer) ((unsigned long) p + len + 1);
}

gpointer
db_unpack_int (gpointer p, int *val)
{
	p = _ALIGN_ADDRESS (p, 4);

	if (val)
		*val = *(int *) p;

	p= (gpointer) ((unsigned long) p + 4);

	return p;
}

gpointer
db_unpack_bool (gpointer p, gboolean *val)
{
	return db_unpack_int (p, (int *) val);
}

gpointer
db_unpack_double (gpointer p, double *val)
{
	gpointer ret;
	char *str;

	ret = db_unpack_string (p, &str);
	*val = atof (str);
	g_free (str);

	return ret;
}

gpointer
db_unpack_pixbuf (gpointer p, GdkPixbuf **pixbuf)
{
	int len;
	GdkPixdata *pixdata;

	p = _ALIGN_ADDRESS (p, 4);

	len = *(int *) p;

	p = (gpointer) ((unsigned long) p + 4);

	pixdata = g_new0 (GdkPixdata, 1);
	gdk_pixdata_deserialize (pixdata, len, (const guint8 *) p, NULL);

	if (pixbuf)
		*pixbuf = gdk_pixbuf_from_pixdata (pixdata, TRUE, NULL);

	g_free (pixdata);

	return (gpointer) ((unsigned long) p + len + 1);
}

static void
string_align (GString *string, int boundary)
{
	gpointer p;
	int padding;
	int i;

	p = string->str + string->len;

	padding = (unsigned long) _ALIGN_ADDRESS (p, boundary) - (unsigned long) p;

	for (i = 0; i < padding; i++)
		g_string_append_c (string, 0);
}

gpointer
db_pack_start (void)
{
	return (gpointer) g_string_new (NULL);
}

void
db_pack_string (gpointer p, const char *str)
{
	GString *string = (GString *) p;
	int len;

	if (str)
		len = strlen (str);
	else
		len = 0;

	db_pack_int (string, len);

	if (str)
		g_string_append (string, str);

	g_string_append_c (string, 0);
}

void
db_pack_int (gpointer p, int val)
{
	GString *string = (GString *) p;

	string_align (string, 4);

	g_string_append_len (string, (char *) &val, 4);
}

void
db_pack_bool (gpointer p, gboolean val)
{
	db_pack_int (p, (int) val);
}

void
db_pack_double (gpointer p, double val)
{
	char *str;

	str = g_strdup_printf ("%.9f", val);
	db_pack_string (p, str);
	g_free (str);
}

void
db_pack_pixbuf (gpointer p, GdkPixbuf *pixbuf)
{
	GString *string = (GString *) p;
	GdkPixdata *pixdata;
	guint len = 0;
	char *str;

	pixdata = g_new0 (GdkPixdata, 1);
	gdk_pixdata_from_pixbuf (pixdata, pixbuf, FALSE);

	str = (char *) gdk_pixdata_serialize (pixdata, &len);

	db_pack_int (string, len);

	if (str) {
		int i;
		for (i = 0; i < len; i++)
			g_string_append_c (string, str [i]);
		g_free (str);
	}

	g_free (pixdata);

	g_string_append_c (string, 0);
}

gpointer
db_pack_end (gpointer p, int *len)
{
	GString *string = (GString *) p;

	*len = string->len;

	return g_string_free (string, FALSE);
}
