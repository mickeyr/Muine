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

using Mono.Unix;

using TagLib;
using TagLib.Id3v2;
using TagLib.Mpeg4;

namespace Muine
{
	public class Metadata
	{
		// Strings
		private static readonly string string_error_load =
			Catalog.GetString ("Failed to load metadata: {0}");

		private TagLib.File file = null;
		private Gdk.Pixbuf album_art = null;
		private double peak = 0.0, gain = 0.0;
		private bool peak_set = false, gain_set = false;

		// Constructor
		public Metadata (string filename)
		{
			file = TagLib.File.Create (filename);
			if (file == null || file.Tag == null || file.Properties.MediaTypes != TagLib.MediaTypes.Audio) {
				throw new System.Exception (System.String.Format (string_error_load, filename));
			}
		}

		// Properties
		// Properties :: Title (get;)

		public string Title {
			get { return file.Tag.Title != null ? file.Tag.Title : ""; }
		}

		// Properties :: Artists (get;)
		//	FIXME: Refactor Artists and Performers properties

		public string [] Artists {
			get { return file.Tag.Artists; }
		}

		// Properties :: Performers (get;)
		//	FIXME: Refactor Artists and Performers properties

		public string [] Performers {
			get { return file.Tag.Performers; }
		}

		// Properties :: Album (get;)

		public string Album {
			get { return file.Tag.Album != null ? file.Tag.Album : ""; }
		}

		// Properties :: AlbumArt (get;)
		public Gdk.Pixbuf AlbumArt {
			get {
				if (album_art == null) {
					TagLib.Id3v2.Tag id3v2_tag = null;
					try {
						id3v2_tag = (TagLib.Id3v2.Tag) file.GetTag (TagTypes.Id3v2);
					} catch {}

					if (id3v2_tag != null) {
						// Try to get a cover image first.
						foreach (AttachedPictureFrame f in id3v2_tag.GetFrames ("APIC")) {
							if (f.Type == PictureType.FrontCover) {
								album_art = GetPixbuf (f.Data);
								if (album_art != null)
									return album_art;
							}
						}

						// Take any image we can get.
						foreach (AttachedPictureFrame f in id3v2_tag.GetFrames ("APIC")) {
							album_art = GetPixbuf (f.Data);

							if (album_art != null)
								return album_art;
						}
					}
					TagLib.Mpeg4.AppleTag apple_tag = null;
					try {
						apple_tag = (TagLib.Mpeg4.AppleTag) file.GetTag (TagTypes.Apple);
					} catch {}

					if (apple_tag != null) {
						foreach (AppleDataBox b in apple_tag.DataBoxes ("covr")) {
							if (b.Flags == (uint) AppleDataBox.FlagType.ContainsJpegData ||
							    b.Flags == (uint) AppleDataBox.FlagType.ContainsPngData) {
								album_art = GetPixbuf (b.Data);

								if (album_art != null)
									return album_art;
							}
						}
					}
				}
				return album_art;
			}
		}

		// Properties :: TrackNumber (get;)
		
		public int TrackNumber {
			get { return (int) file.Tag.Track; }
		}

		// Properties :: TotalTracks (get;)
		
		public int TotalTracks {
			get { return (int) file.Tag.TrackCount; }
		}

		// Properties :: DiscNumber (get;)

		public int DiscNumber {
			get { return (int) file.Tag.Disc; }
		}

		// Properties :: Year (get;)

		public string Year {
			get { return file.Tag.Year.ToString ();}
		}

		// Properties :: Duration (get;)

		public int Duration {
			get { return (int) file.Properties.Duration.TotalSeconds; }
		}

		// Properties :: MimeType (get;)

		public string MimeType {
			get { return file.MimeType != null ? file.MimeType : ""; }
		}

		// Properties :: MTime (get;)

		public int MTime {
			get {
				Mono.Unix.Native.Stat buf;
				Mono.Unix.Native.Syscall.stat (file.Name, out buf);
				return (int) buf.st_mtime;
			}
		}

		// Properties :: Gain (get;)

		public double Gain {
			get {
				if (!gain_set)
				{
					TagLib.Id3v2.Tag id3v2_tag = null;
					gain_set = true;
					TagLib.Ogg.XiphComment xiph_comment = (TagLib.Ogg.XiphComment) file.GetTag (TagTypes.Xiph);

					try {
						id3v2_tag = (TagLib.Id3v2.Tag) file.GetTag (TagTypes.Id3v2);
					} catch {}

					if (id3v2_tag != null) {
						foreach (RelativeVolumeFrame f in id3v2_tag.GetFrames ("RVA2")) {
							gain = f.GetVolumeAdjustment (ChannelType.MasterVolume);
							return gain;
						}
					}

					if (xiph_comment != null) {
						string [] names = {"replaygain_track_gain", "replaygain_album_gain", "rg_audiophile", "rg_radio"};
						foreach (string name in names) {
							string [] l = xiph_comment.GetField (name);
							if (l != null && l.Length != 0)
								foreach (string s in l)
									try {
										gain = System.Double.Parse (s);
										return gain;
									} catch {}
						}
					}
				}

				return gain;
			}
		}

		// Properties :: Peak (get;)
		
		public double Peak {
			get {
				if (!peak_set)
				{
					peak_set = true;
					TagLib.Id3v2.Tag id3v2_tag = null;

					TagLib.Ogg.XiphComment xiph_comment = (TagLib.Ogg.XiphComment) file.GetTag (TagTypes.Xiph);

					try {
						id3v2_tag = (TagLib.Id3v2.Tag) file.GetTag (TagTypes.Id3v2);
					} catch {}

					if (id3v2_tag != null) {
						foreach (RelativeVolumeFrame f in id3v2_tag.GetFrames ("RVA2")) {
							peak = f.GetPeakVolume (ChannelType.MasterVolume);
							return peak;
						}
					}

					if (xiph_comment != null) {
						string [] names = {"replaygain_track_peak", "replaygain_album_peak", "rg_peak"};
						foreach (string name in names) {
							string [] l = xiph_comment.GetField (name);
							if (l != null && l.Length != 0)
								foreach (string s in l)
									try {
										peak = System.Double.Parse (s);
										return peak;
									} catch {}
						}
					}
				}

				return peak;
			}
		}

		private Gdk.Pixbuf GetPixbuf (ByteVector data)
		{
			bool bail = false;
			Gdk.Pixbuf output;
			Gdk.PixbufLoader loader = new Gdk.PixbufLoader ();

			try {
				if (!loader.Write (data.Data))
					bail = true;
			} catch {bail = true;}

			try {
				if (!loader.Close ())
					bail = true;
			} catch {bail = true;}

			output = (!bail) ? loader.Pixbuf : null;

			return output;
		}
	}
}
