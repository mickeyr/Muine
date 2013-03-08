/*
 * Copyright (C) 2004 Jorn Baayen <jorn.baayen@gmail.com>
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

using System;

using Gdk;

namespace Muine
{
	public static class KeyUtils
	{
		// Methods
		// Methods :: Public
		// Methods :: Public :: HaveModifier
		//	If we have modifiers, and either Ctrl, Mod1 (Alt), or any
		//	of Mod3 to Mod5 (Mod2 is num-lock...) are pressed, we
		//	let Gtk+ handle the key
		public static bool HaveModifier (Gdk.EventKey e) {
			return (e.State != 0 && 
				(((e.State & Gdk.ModifierType.ControlMask) != 0) ||
			         ((e.State & Gdk.ModifierType.Mod1Mask   ) != 0) ||
			         ((e.State & Gdk.ModifierType.Mod3Mask   ) != 0) ||
			         ((e.State & Gdk.ModifierType.Mod4Mask   ) != 0) ||
			         ((e.State & Gdk.ModifierType.Mod5Mask   ) != 0)));
		}

		// Methods :: Public :: IsModifier
		public static bool IsModifier (Gdk.EventKey e) {
			switch (e.Key) {
			case Key.Shift_L:
			case Key.Shift_R:
			case Key.Caps_Lock:
			case Key.Shift_Lock:
			case Key.Control_L:
			case Key.Control_R:
			case Key.Meta_L:
			case Key.Meta_R:
			case Key.Alt_L:
			case Key.Alt_R:
			case Key.Super_L:
			case Key.Super_R:
			case Key.Hyper_L:
			case Key.Hyper_R:
			case Key.Mode_switch:
			case Key.ISO_Level3_Shift:
				return true;

			default:
				return false;
			}
		}
	}
}
