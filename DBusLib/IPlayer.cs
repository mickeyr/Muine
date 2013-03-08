
using NDesk.DBus;

namespace Muine.DBusLib
{
	[Interface ("org.gnome.Muine.Player")]
	public interface IPlayer
	{
		bool GetPlaying ();
		void SetPlaying (bool playing);
		bool HasNext ();
		void Next ();
		bool HasPrevious ();
		void Previous ();
		string GetCurrentSong ();
		bool GetWindowVisible ();
		void SetWindowVisible (bool visible, uint time);
		int GetVolume ();
		void SetVolume (int volume);
		int GetPosition ();
		void SetPosition (int pos);
		void PlayAlbum (uint time);
		void PlaySong (uint time);
		void OpenPlaylist (string uri);
		void PlayFile (string uri);
		void QueueFile (string uri);
		// XXX: Should be uri
		bool WriteAlbumCoverToFile (string file);
		byte [] GetAlbumCover ();
		void Quit ();
		event SongChangedHandler SongChanged;
		event StateChangedHandler StateChanged;
	}

	public delegate void SongChangedHandler (string song_data);
	public delegate void StateChangedHandler (bool playing);
}
