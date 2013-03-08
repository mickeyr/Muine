ASSEMBLY_NAME = muine-plugin
ASSEMBLY = $(ASSEMBLY_NAME).dll
TARGET = $(ASSEMBLY)

CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

PLUGIN_LIB_CSFILES =			\
	$(srcdir)/Plugin.cs		\
	$(srcdir)/IPlayer.cs		\
	$(srcdir)/ISong.cs		\
	$(srcdir)/AssemblyInfo.cs

muine.snk: $(top_srcdir)/muine.snk
	cp $(top_srcdir)/muine.snk .

$(ASSEMBLY): $(PLUGIN_LIB_CSFILES) muine.snk
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(PLUGIN_LIB_CSFILES) $(GTKSHARP_LIBS)

all: $(ASSEMBLY)

EXTRA_DIST = $(PLUGIN_LIB_CSFILES)

CLEANFILES = $(ASSEMBLY) muine.snk

muinelibdir = $(pkglibdir)
muinelib_DATA = $(TARGET)
