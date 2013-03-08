if ENABLE_MONODOC
ASSEMBLIES = \
	$(top_builddir)/PluginLib/muine-plugin.dll \
	$(top_builddir)/DBusLib/muine-dbus.dll

ASSEMBLIES_BUILD = $(foreach asm,$(ASSEMBLIES),$(addprefix -assembly:,$(asm)))

docdir = $(libdir)/monodoc/sources
doc_DATA = \
	muine-docs.zip \
	muine-docs.tree \
	muine-docs.source
else
endif

muine-docs.zip: muine-docs.tree

muine-docs.tree: $(srcdir)/en/*/*.xml $(srcdir)/en/*.xml
	$(MDASSEMBLER) --ecma $(srcdir)/en -o muine-docs

update: $(ASSEMBLIES)
	$(MONODOCER) $(ASSEMBLIES_BUILD) -path:en -updateto:en -delete

DISTCLEANFILES = muine-docs.zip muine-docs.tree

EXTRA_DIST = muine-docs.source

dist-hook:
	mkdir -p $(distdir)/en
	cp $(srcdir)/en/*.xml $(distdir)/en/
	mkdir -p $(distdir)/en/Muine.DBusLib
	cp $(srcdir)/en/Muine.DBusLib/*.xml $(distdir)/en/Muine.DBusLib/
	mkdir -p $(distdir)/en/Muine.PluginLib
	cp $(srcdir)/en/Muine.PluginLib/*.xml $(distdir)/en/Muine.PluginLib/
