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
using System.Globalization;

namespace Muine
{
	public abstract class Item : IComparable
	{
		// Variables
		protected IntPtr handle;

		protected SortKey sort_key   = null;
		protected string  search_key = null;
	
		// Properties
		// Properties :: Abstract
		// Properties :: Abstract :: CoverImage (set; get;)
		public abstract Gdk.Pixbuf CoverImage {
			set;
			get;
		}

		// Properties :: Abstract :: Public (get;)
		public abstract bool Public {
			get;
		}

		// Properties :: Virtual
		// Properties :: Virtual :: Handle (get;)
		public virtual IntPtr Handle {
			get { return handle; }
		}

		// Properties :: SortKey (get;)
		public SortKey SortKey {
			get {
				if (sort_key == null)
					sort_key = GenerateSortKey ();
				
				return sort_key;
			}
		}

		// Properties :: SearchKey (get;)
		public string SearchKey {
			get {
				if (search_key == null)
					search_key = GenerateSearchKey ();

				return search_key;
			}
		}

		// Methods
		// Methods :: Abstract
		public abstract void Deregister ();

		protected abstract SortKey GenerateSortKey ();

		protected abstract string GenerateSearchKey ();


		// Methods :: Public		
		// Methods :: Public :: CompareTo (IComparable)
		public int CompareTo (object o)
		{
			// Check if we're trying to compare to nothing
			if (o == null)
				return 1;
			
			Item other = (Item) o;
					
			return SortKey.Compare (this.SortKey, other.SortKey);
		}
		
		// Methods :: Public :: FitsCriteria
		public bool FitsCriteria (string [] search_bits)
		{
			if (!Public)
				return false;

			int n_matches = 0;
				
			foreach (string search_bit in search_bits) {
				if (SearchKey.IndexOf (search_bit) < 0)
					continue;
					
				n_matches++;
			}

			return (n_matches == search_bits.Length);
		}
	}
}
