/*
 * Copyright (C) 2005 Tamara Roberson <tamara.roberson@gmail.com>
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

namespace Muine
{
	public class AddWindowEntry : Gtk.Entry
	{
		// Variables
		private string [] search_bits = new string [0];

		// Constructor
		/// <summary>
		/// 	Creates a new <see cref="AddWindowEntry">.
		/// </summary>
		/// <remarks>
		/// 	This should only be called by <see cref="AddWindow" />.
		/// </remarks>
		public AddWindowEntry () : base ()
		{
			ActivatesDefault = true;

			Changed += OnChanged;
		}

		// Properties
		// Properties :: SearchBits (get;)
		/// <summary>
		/// 	The "search bits", or words, entered in the entry.
		/// </summary>
		/// <returns>
		///	An array of <see cref="String">strings</see>.
		/// </returns>
		public string [] SearchBits {
			get { return search_bits; }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Clear
		/// <summary>
		/// 	Reset the entry.
		/// </summary>
		public void Clear ()
		{
			base.Text = String.Empty;
		}

		// Handlers
		// Handlers :: OnChanged
		/// <summary>
		/// 	Handler called when the entry is changed.
		/// </summary>
		/// <remarks>
		/// 	Updates the <see cref="SearchBits" />.
		/// </remarks>
		private void OnChanged (object o, EventArgs args)
		{
			search_bits = base.Text.ToLower ().Split (' ');
		}
	}
}
