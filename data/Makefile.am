SUBDIRS =	\
	glade	\
	images	\
	ui

@INTLTOOL_DESKTOP_RULE@

desktop_in_files = muine.desktop.in
desktop_files = $(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

schema_in_files = muine.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

pkgconfig_in_files = muine-plugin.pc.in muine-dbus.pc.in
pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = $(pkgconfig_in_files:.pc.in=.pc)

dbusservice_in_files = org.gnome.Muine.service.in
dbusservicedir = $(datadir)/dbus-1/services
dbusservice_DATA = $(dbusservice_in_files:.service.in=.service)

EXTRA_DIST =			\
	$(desktop_in_files)	\
	$(schema_in_files)	\
	$(pkgconfig_in_files)	\
	$(dbusservice_in_files)

DISTCLEANFILES = 		\
	$(desktop_DATA) 	\
	$(schema_DATA)		\
	$(pkgconfig_DATA)	\
	$(dbusservice_DATA)

if GCONF_SCHEMAS_INSTALL
install-data-local:
	GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) $(GCONFTOOL) --makefile-install-rule $(schema_DATA)
endif
