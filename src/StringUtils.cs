/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jorn.baayen@gmail.com>
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
using System.Collections;

using Mono.Unix;

namespace Muine
{
	public static class StringUtils
	{
		// Strings
		private static readonly string string_unknown =
			Catalog.GetString ("Unknown");

		private static readonly string string_many =
			Catalog.GetString ("{0} and others");

		private static readonly string string_several =
			Catalog.GetString ("{0} and {1}");
		
		// Methods
		// Methods :: Public
		// Methods :: Public :: SecondsToString		
		public static string SecondsToString (long time)
		{
			long h, m, s;

			h = (time / 3600);
			m = ((time % 3600) / 60);
			s = ((time % 3600) % 60);

			if (h > 0)
				return String.Format ("{0}:{1}:{2}", h, m.ToString ("d2"), 
					s.ToString ("d2"));
			
			return String.Format ("{0}:{1}", m, s.ToString ("d2"));
		}

		// Methods :: Public :: JoinHumanReadable
		//	TODO: Make I18N (don't hardcode English commas)
		public static string JoinHumanReadable (string [] strings)
		{
			return JoinHumanReadable (strings, -1);
		}

		public static string JoinHumanReadable (string [] strings, int max)
		{
			if (strings.Length == 0)
				return string_unknown;
			
			if (strings.Length == 1)
				return strings [0];
			
			if (strings.Length > max && max > 1)
				return String.Format (string_many, 
					String.Join (", ", strings, 0, max));

			return String.Format (string_several, 
				String.Join (", ", strings, 0, strings.Length - 1),
				strings [strings.Length - 1]);
		}

		// Methods :: Public :: PrefixToSuffix
		public static string PrefixToSuffix (string str, string prefix)
		{
			string str_no_prefix = str.Remove (0, prefix.Length + 1);
			return String.Format ("{0} {1}", str_no_prefix, prefix);
		}

		// Methods :: Public :: SearchKey
		//	TODO: Rename this to a verb.
		public static string SearchKey (string key)
		{
			string lower = key.ToLower ();

			bool different = false;
			string stripped = String.Empty;

			foreach (char c in lower) {
				if (Char.IsLetterOrDigit (c) || Char.IsWhiteSpace (c) ||
				    Char.IsSurrogate (c)) {
					stripped += c;
					continue;
				}

				different = true;
			}

			// Both, so that "R.E.M." will yield only "R.E.M.", but "rem"
			// both "remix and "R.E.M.".
			if (different)
				return String.Format ("{0} {1}", stripped, lower);

			return stripped;
		}

		// Methods :: Public :: EscapeForPango
		public static string EscapeForPango (string s)
		{
			s = s.Replace ("&", "&amp;");
			s = s.Replace ("<", "&lt;");

			return s;
		}
	}
}
