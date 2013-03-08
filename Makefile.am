NULL =

SUBDIRS = 		\
	data		\
	libmuine	\
	PluginLib	\
	DBusLib		\
	src		\
	plugins		\
	doc		\
	po		\
	$(NULL)

EXTRA_DIST =			\
	TODO			\
	HACKING			\
	PLUGINS			\
	MAINTAINERS		\
	muine.spec.in		\
	muine.spec		\
	muine.snk		\
	$(NULL)

DISTCLEANFILES = 		\
	$(NULL)

DISTCHECK_CONFIGURE_FLAGS = \
	--disable-schemas-install

ChangeLog:
	@if test -f $(top_srcdir)/.git/HEAD; then \
		git log --stat > $@; \
	fi

dist: ChangeLog

.PHONY: ChangeLog

# vim: set ts=8 sw=8 noet:
