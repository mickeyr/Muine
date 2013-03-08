CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

TARGET = muine.exe
WRAPPER = muine
MUINE_LIBS = $(GTKSHARP_LIBS) $(TAGLIBSHARP_LIBS)

MUINE_CSFILES =					\
	$(srcdir)/AddSongWindow.cs		\
	$(srcdir)/AddAlbumWindow.cs		\
	$(srcdir)/Global.cs			\
	$(srcdir)/PlaylistWindow.cs		\
	$(srcdir)/Song.cs			\
	$(srcdir)/Album.cs			\
	$(srcdir)/SongDatabase.cs		\
	$(srcdir)/About.cs			\
	$(srcdir)/Metadata.cs			\
	$(srcdir)/Player.cs			\
	$(srcdir)/FileSelector.cs		\
	$(srcdir)/StringUtils.cs		\
	$(srcdir)/KeyUtils.cs			\
	$(srcdir)/SkipToWindow.cs		\
	$(srcdir)/ProgressWindow.cs		\
	$(srcdir)/ErrorDialog.cs		\
	$(srcdir)/OverwriteDialog.cs		\
	$(srcdir)/VolumeButton.cs		\
	$(srcdir)/HandleView.cs			\
	$(srcdir)/HandleModel.cs		\
	$(srcdir)/StockIcons.cs			\
	$(srcdir)/ColoredCellRendererPixbuf.cs  \
	$(srcdir)/CoverDatabase.cs		\
	$(srcdir)/CoverGetter.cs		\
	$(srcdir)/MusicBrainz.cs		\
	$(srcdir)/GnomeProxy.cs			\
	$(srcdir)/CoverImage.cs			\
	$(srcdir)/MmKeys.cs			\
	$(srcdir)/GnomeMMKeys.cs		\
	$(srcdir)/FileUtils.cs			\
	$(srcdir)/SignalUtils.cs		\
	$(srcdir)/Database.cs			\
	$(srcdir)/DBusService.cs		\
	$(srcdir)/PluginManager.cs		\
	$(srcdir)/AddWindow.cs			\
	$(srcdir)/Config.cs			\
	$(srcdir)/DndUtils.cs			\
	$(srcdir)/Item.cs			\
	$(srcdir)/ThreadBase.cs			\
	$(srcdir)/AddWindowList.cs		\
	$(srcdir)/AddWindowEntry.cs		\
	$(srcdir)/Actions.cs			\
	$(srcdir)/ImportDialog.cs		\
	$(srcdir)/OpenDialog.cs			\
	$(srcdir)/SaveDialog.cs

MUINE_GENERATED_CSFILES =			\
	AmazonSearchService.cs			\
	Defines.cs

if EXTERNAL_DBUS
DBUS_ASSEMBLIES = $(DBUS_LIBS)
else
DBUS_ASSEMBLIES =							\
	-r:$(top_builddir)/deps/dbus-sharp/NDesk.DBus.dll		\
	-r:$(top_builddir)/deps/dbus-sharp-glib/NDesk.DBus.GLib.dll
endif

MUINE_ASSEMBLIES =							\
	-r:$(top_builddir)/PluginLib/muine-plugin.dll			\
	-r:$(top_builddir)/DBusLib/muine-dbus.dll			\
	$(DBUS_ASSEMBLIES)						\
	-r:System.Web.Services						\
	-r:Mono.Posix
	
MUINE_RESOURCES =											\
	-resource:$(top_srcdir)/data/glade/PlaylistWindow.glade,PlaylistWindow.glade			\
	-resource:$(top_srcdir)/data/glade/SkipToWindow.glade,SkipToWindow.glade			\
	-resource:$(top_srcdir)/data/glade/AddWindow.glade,AddWindow.glade				\
	-resource:$(top_srcdir)/data/glade/ProgressWindow.glade,ProgressWindow.glade			\
	-resource:$(top_srcdir)/data/ui/PlaylistWindow.xml,PlaylistWindow.xml				\
	-resource:$(top_srcdir)/data/images/muine-playing.png,muine-playing.png				\
	-resource:$(top_srcdir)/data/images/muine-paused.png,muine-paused.png				\
	-resource:$(top_srcdir)/data/images/muine-nothing.png,muine-nothing.png				\
	-resource:$(top_srcdir)/data/images/muine-default-cover.png,muine-default-cover.png		\
	-resource:$(top_srcdir)/data/images/muine-cover-downloading.png,muine-cover-downloading.png	\
	-resource:$(top_srcdir)/data/images/muine-about.png,muine-about.png				\
	-resource:$(top_srcdir)/data/images/muine-32.png,muine.png						

# FIXME MONO_XMLSERIALIZER_THS=no works around Ximian Bugzilla #58194
AmazonSearchService.cs: AmazonSearchService.wsdl
	MONO_XMLSERIALIZER_THS=no wsdl -namespace:Amazon $(srcdir)/AmazonSearchService.wsdl

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" 		\
	    -e "s|\@pkglibdir\@|$(pkglibdir)|g" 	\
	    -e "s|\@target\@|$(TARGET)|g" 		\
	    -e "s|\@mono\@|$(MONO)|g"			\
	    -e "s|\@mono_flags\@|$(MONO_FLAGS)|g"	\
	    < $^ > $@

$(TARGET).config: $(srcdir)/$(TARGET).config.in
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	    < $^ > $@

Defines.cs: $(srcdir)/Defines.cs.in
	sed -e "s|\@version\@|$(VERSION)|"     \
	    -e "s|\@datadir\@|$(datadir)|"     \
	    -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	    < $^ > $@

$(TARGET): $(MUINE_CSFILES) $(MUINE_GENERATED_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll $(top_builddir)/DBusLib/muine-dbus.dll
	$(CSC) -target:winexe -out:$@ $(MUINE_CSFILES) $(MUINE_GENERATED_CSFILES) $(MUINE_ASSEMBLIES) $(MUINE_RESOURCES) $(MUINE_LIBS)

all: $(TARGET)

muinelibdir = $(pkglibdir)
muinelib_DATA = $(TARGET) $(TARGET).config

bin_SCRIPTS = $(WRAPPER)

EXTRA_DIST =					\
	$(MUINE_CSFILES)			\
	AmazonSearchService.wsdl		\
	$(WRAPPER).in				\
	$(TARGET).config.in			\
	Defines.cs.in

CLEANFILES =					\
	$(MUINE_GENERATED_CSFILES)		\
	$(TARGET)				\
	$(TARGET).config			\
	$(WRAPPER)
