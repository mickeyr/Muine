/* -*- mode: C; c-file-style: "gnu" -*- */
/*
 * Copyright (C) 2003 Richard Hult <richard@imendio.com>
 * Copyright (C) 2003 Johan Dahlin <jdahlin@gnome.org>
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
#include <math.h>
#include <gst/gst.h>

#include <glib/gi18n.h>

#include "macros.h"
#include "player.h"

static void player_class_init (PlayerClass     *klass);
static void player_init       (Player          *player);
static void player_finalize   (GObject         *object);
static gboolean bus_message_cb (GstBus *bus,
				GstMessage *message,
				gpointer data);
static gboolean tick_timeout  (Player          *player);

enum {
	END_OF_STREAM,
	TICK,
	ERROR,
	LAST_SIGNAL
};

struct _PlayerPriv {
	GstElement *play;

	char       *current_file;

	int	    cur_volume;
	double      volume_scale;

	guint       tick_timeout_id;

	GTimer     *timer;
	long        timer_add;
};

static GObjectClass *parent_class;
static guint signals[LAST_SIGNAL];

GType
player_get_type (void)
{
	static GType type = 0;

	if (!type) {
		static const GTypeInfo info = {
			sizeof (PlayerClass),
				NULL,           /* base_init */
				NULL,           /* base_finalize */
				(GClassInitFunc) player_class_init,
				NULL,           /* class_finalize */
				NULL,           /* class_data */
				sizeof (Player),
				0,
				(GInstanceInitFunc) player_init,
				NULL
			};

			type = g_type_register_static (G_TYPE_OBJECT,
						       "Player",
				                       &info, 0);
	}

	return type;
}

static void
player_class_init (PlayerClass *klass)
{
	GObjectClass *object_class;

	parent_class = g_type_class_peek_parent (klass);
	object_class = (GObjectClass *) klass;

	object_class->finalize = player_finalize;

	signals[END_OF_STREAM] =
		g_signal_new ("end_of_stream",
		              G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      0,
			      NULL, NULL,
			      g_cclosure_marshal_VOID__VOID,
			      G_TYPE_NONE, 0);

	signals[TICK] =
		g_signal_new ("tick",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      0,
			      NULL, NULL,
			      g_cclosure_marshal_VOID__INT,
			      G_TYPE_NONE, 1, G_TYPE_INT);

	signals[ERROR] =
		g_signal_new ("error",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      0,
			      NULL, NULL,
			      g_cclosure_marshal_VOID__STRING,
			      G_TYPE_NONE, 1, G_TYPE_STRING);
}

static void
player_init (Player *UNUSED(player))
{
}

static void
player_construct (Player *player, char **error)
{
	PlayerPriv *priv;
	GstElement *sink;

	gst_init (NULL, NULL);

	priv = g_new0 (PlayerPriv, 1);
	player->priv = priv;

	priv->timer = g_timer_new ();
	g_timer_stop (priv->timer);
	priv->timer_add = 0;

	priv->tick_timeout_id = g_timeout_add (200, (GSourceFunc) tick_timeout, player);

	priv->play = gst_element_factory_make ("playbin", "play");
	if (!priv->play) {
		*error = g_strdup (_("Failed to create a GStreamer play object"));

		return;
	}

	sink = gst_element_factory_make ("gconfaudiosink", "sink");
	if (!sink) {
		*error = g_strdup (_("Could not render default GStreamer audio output sink"));

		return;
	}

	g_object_set (G_OBJECT (priv->play), "audio-sink",
		      sink, NULL);

	gst_bus_add_watch (gst_pipeline_get_bus (GST_PIPELINE (priv->play)),
			   bus_message_cb, player);
}

static void
player_finalize (GObject *object)
{
	Player *player = PLAYER (object);

	player_stop (player);

	g_timer_destroy (player->priv->timer);

	if (player->priv->tick_timeout_id > 0)
		g_source_remove (player->priv->tick_timeout_id);

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_NULL);
	g_object_unref (player->priv->play);

	g_free (player->priv);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		(* G_OBJECT_CLASS (parent_class)->finalize) (object);
}

Player *
player_new (char **error)
{
	Player *player;

	player = g_object_new (TYPE_PLAYER, NULL);

	*error = NULL;

	player_construct (player, error);

	return player;
}

static gboolean
tick_timeout (Player *player)
{
	GstState state;

	gst_element_get_state (player->priv->play, &state, NULL, 0);
	if (state != GST_STATE_PLAYING)
		return TRUE;

	g_signal_emit (player, signals[TICK], 0, player_tell (player));

	return TRUE;
}

static gboolean
bus_message_cb (GstBus *UNUSED(bus),
		GstMessage *message,
		gpointer data)
{
	Player *player = (Player *) data;
	char *debug;
	GError *err;

	switch (GST_MESSAGE_TYPE (message)) {
	case GST_MESSAGE_ERROR:
		gst_message_parse_error (message, &err, &debug);

		/* Stop playing so we don't get repeated error messages. 
		   Might lead to troubles with threads, not sure.
		*/
		player_stop (player);
		
		g_signal_emit (player, signals[ERROR], 0, g_strdup (err->message));
		break;

	case GST_MESSAGE_EOS:
		player->priv->timer_add += floor (g_timer_elapsed (player->priv->timer, NULL) + 0.5);
		g_timer_stop (player->priv->timer);
		g_timer_reset (player->priv->timer);
		
		g_signal_emit (player, signals[END_OF_STREAM], 0);
		break;

	case GST_MESSAGE_STATE_CHANGED:
		/* Do nothing */
		break;

	default:
		break;
	}

	return TRUE;
}

gboolean
player_set_file (Player     *player,
		 const char *file,
		 char      **error)
{
	g_return_val_if_fail (IS_PLAYER (player), FALSE);

	*error = NULL;

	player_stop (player);

	if (!file)
		return FALSE;

	player->priv->current_file = g_filename_to_uri (file, NULL, NULL);
	if (player->priv->current_file == NULL)
	  {
	    *error = g_strdup ("Failed to convert filename to URI.");
	    return FALSE;
	  }

	g_timer_stop (player->priv->timer);
	g_timer_reset (player->priv->timer);
	player->priv->timer_add = 0;

	g_object_set (G_OBJECT (player->priv->play), "uri",
		      player->priv->current_file, NULL);

	return TRUE;
}

void
player_play (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_PLAYING);

	g_timer_start (player->priv->timer);
}

void
player_stop (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	g_free (player->priv->current_file);
	player->priv->current_file = NULL;

	g_timer_stop (player->priv->timer);
	g_timer_reset (player->priv->timer);
	player->priv->timer_add = 0;

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_READY);
}

void
player_pause (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_PAUSED);

	player->priv->timer_add += floor (g_timer_elapsed (player->priv->timer, NULL) + 0.5);
	g_timer_stop (player->priv->timer);
	g_timer_reset (player->priv->timer);
}

static void
update_volume (Player *player)
{
	int real_vol;
	double d;

	real_vol = player->priv->cur_volume * player->priv->volume_scale;

	d = CLAMP (real_vol, 0, 100) / 100.0;

	g_object_set (G_OBJECT (player->priv->play), "volume", d, NULL);
}

void
player_set_volume (Player *player, int volume)
{
	g_return_if_fail (IS_PLAYER (player));
	g_return_if_fail (volume >= 0 && volume <= 100);

	player->priv->cur_volume = volume;

	update_volume (player);
}

int
player_get_volume (Player *player)
{
	g_return_val_if_fail (IS_PLAYER (player), -1);

	return player->priv->cur_volume;
}

void
player_set_replaygain (Player *player, double gain, double peak)
{
	double scale;

	g_return_if_fail (IS_PLAYER (player));

	if (gain == 0) {
		player->priv->volume_scale = 1.0;
		update_volume (player);

		return;
	}

	scale = pow (10., gain / 20);

	/* anti clip */
	if (peak != 0 && (scale * peak) > 1)
		scale = 1.0 / peak;

	/* For security */
	if (scale > 15)
		scale = 15;

	player->priv->volume_scale = scale;
	update_volume (player);
}

void
player_seek (Player *player, int t)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_seek (player->priv->play, 1.0,
			  GST_FORMAT_TIME,
			  GST_SEEK_FLAG_FLUSH,
			  GST_SEEK_TYPE_SET,
			  t * GST_SECOND,
			  0, 0);

	g_timer_reset (player->priv->timer);
	player->priv->timer_add = t;
}

int
player_tell (Player *player)
{
	g_return_val_if_fail (IS_PLAYER (player), -1);

	return (int) floor (g_timer_elapsed (player->priv->timer, NULL) + 0.5) + player->priv->timer_add;
}
