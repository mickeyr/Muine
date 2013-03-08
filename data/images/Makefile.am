noinst_DATA =					\
	muine-about.png				\
	muine-playing.png			\
	muine-paused.png			\
	muine-nothing.png			\
	muine-default-cover.png			\
	muine-cover-downloading.png

install-data-local: install-icons
uninstall-local: uninstall-icons

gtk_update_icon_cache = gtk-update-icon-cache -f -t $(datadir)/icons/hicolor

icons = \
	muine-16.png \
	muine-22.png \
	muine-24.png \
	muine-32.png \
	muine-48.png \
	muine-scalable.svg

install-icons:
	@echo "Installing application icons..."
	for icon in $(icons); do \
		SIZE=`echo $$icon | cut -d- -f2 | cut -d. -f1`; \
		EXTENSION=`echo $$icon | cut -d. -f2`; \
		if test "$$SIZE" = "scalable"; then \
			SIZE="scalable"; \
		else \
			SIZE="$${SIZE}x$${SIZE}"; \
		fi; \
		mkdir -p $(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps; \
		$(INSTALL_DATA) $(srcdir)/$$icon $(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/muine.$$EXTENSION; \
	done
	@-if test -z "$(DESTDIR)"; then \
		echo "Updating GTK icon cache."; \
		$(gtk_update_icon_cache); \
	else \
		echo "*** Icon cache not updated.  After install, run this:"; \
		echo "***   $(gtk_update_icon_cache)"; \
	fi

uninstall-icons:
	@echo "Uninstalling application icons..."
	for icon in $(icons); do \
		SIZE=`echo $$icon | cut -d- -f2 | cut -d. -f1`; \
		EXTENSION=`echo $$icon | cut -d. -f2`; \
		if test "$$SIZE" = "scalable"; then \
			SIZE="scalable"; \
		else \
			SIZE="$${SIZE}x$${SIZE}"; \
		fi; \
		rm -f $(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/muine.$$EXTENSION; \
	done

EXTRA_DIST = \
	$(noinst_DATA) \
	muine-16.png \
	muine-16.svg \
	muine-22.png \
	muine-24.png \
	muine-24.svg \
	muine-32.png \
	muine-32.svg \
	muine-48.png \
	muine-scalable.svg
