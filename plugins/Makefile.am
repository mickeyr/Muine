CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

plugindir = $(libdir)/muine/plugins

if ENABLE_INOTIFY
plugin_DATA = TrayIcon.dll TrayIcon.dll.config InotifyPlugin.dll InotifyPlugin.dll.config
else
plugin_DATA = TrayIcon.dll TrayIcon.dll.config
endif

muinelibdir = $(pkglibdir)
if ENABLE_INOTIFY
muinelib_LTLIBRARIES = libinotifyglue.la
else
muinelib_LTLIBRARIES =
endif

libinotifyglue_la_SOURCES = inotify-glue.c

TRAY_ICON_CSFILES =			\
	$(srcdir)/TrayIcon.cs		\
	$(srcdir)/NotificationArea.cs

TRAY_ICON_EXTRA_CSFILES =		\
	$(top_builddir)/src/Defines.cs	\
	$(top_srcdir)/src/StringUtils.cs

TRAY_ICON_EXTRA_ASSEMBLIES =		\
	$(GTKSHARP_LIBS)		\
	-r:Mono.Posix

TRAY_ICON_RESOURCES =							  \
	-resource:$(srcdir)/TrayIcon.xml,TrayIcon.xml			  \
	-resource:$(srcdir)/muine-tray-playing.png,muine-tray-playing.png \
	-resource:$(srcdir)/muine-tray-paused.png,muine-tray-paused.png

INOTIFY_CSFILES =		\
	$(srcdir)/Inotify.cs		\
	$(srcdir)/InotifyPlugin.cs

INOTIFY_EXTRA_ASSEMBLIES =		\
	$(GTKSHARP_LIBS)		\
	-r:Mono.Posix

ASSEMBLIES = -r:$(top_builddir)/PluginLib/muine-plugin.dll
	
TrayIcon.dll: $(TRAY_ICON_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(TRAY_ICON_CSFILES) $(TRAY_ICON_EXTRA_CSFILES) $(ASSEMBLIES) $(TRAY_ICON_EXTRA_ASSEMBLIES) $(TRAY_ICON_RESOURCES)

InotifyPlugin.dll: $(INOTIFY_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -unsafe -target:library $(LIBFLAGS) -out:$@ $(INOTIFY_CSFILES) $(ASSEMBLIES) $(INOTIFY_EXTRA_ASSEMBLIES)

InotifyPlugin.dll.config: $(srcdir)/InotifyPlugin.dll.config.in
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	< $^ > $@


if ENABLE_INOTIFY
all: TrayIcon.dll InotifyPlugin.dll
else
all: TrayIcon.dll
endif

EXTRA_DIST = $(TRAY_ICON_CSFILES) $(INOTIFY_CSFILES) TrayIcon.xml muine-tray-playing.png muine-tray-paused.png InotifyPlugin.dll.config.in TrayIcon.dll.config

CLEANFILES = TrayIcon.dll InotifyPlugin.dll InotifyPlugin.dll.config
