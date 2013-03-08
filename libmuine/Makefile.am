INCLUDES =							\
	-I$(top_srcdir)						\
	-DG_LOG_DOMAIN=\"libmuine\"				\
	-DGNOMELOCALEDIR=\""$(datadir)/locale"\"		\
	$(MUINE_CFLAGS)						\
	$(WARN_CFLAGS)						\
	-DG_DISABLE_DEPRECATED					\
	-DGTK_DISABLE_DEPRECATED				\
	-DGDK_DISABLE_DEPRECATED				\
	-DGDK_PIXBUF_DISABLE_DEPRECATED				\
	-DGNOME_DISABLE_DEPRECATED

muinelibdir = $(pkglibdir)
muinelib_LTLIBRARIES = libmuine.la

libmuine_la_SOURCES =			\
	macros.h			\
	player.h			\
	player-gst.c		\
	pointer-list-model.c		\
	pointer-list-model.h		\
        rb-cell-renderer-pixbuf.c       \
        rb-cell-renderer-pixbuf.h       \
	db.c				\
	db.h				\
	mm-keys.c			\
	mm-keys.h

libmuine_la_LIBADD = $(MUINE_LIBS) $(GDBM_LIBS)
