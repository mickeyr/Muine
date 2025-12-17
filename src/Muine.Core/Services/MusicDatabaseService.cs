using Microsoft.Data.Sqlite;
using Muine.Core.Models;
using System.Text.Json;

namespace Muine.Core.Services;

public class MusicDatabaseService : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public MusicDatabaseService(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();
        await CreateTablesAsync();
    }

    private async Task CreateTablesAsync()
    {
        var createSongsTable = @"
            CREATE TABLE IF NOT EXISTS Songs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Filename TEXT NOT NULL UNIQUE,
                Title TEXT NOT NULL,
                Artists TEXT NOT NULL,
                Performers TEXT NOT NULL,
                Album TEXT,
                TrackNumber INTEGER,
                NAlbumTracks INTEGER,
                DiscNumber INTEGER,
                Year TEXT,
                Duration INTEGER,
                Gain REAL,
                Peak REAL,
                MTime INTEGER,
                CoverImagePath TEXT,
                SourceType INTEGER DEFAULT 0,
                YouTubeId TEXT,
                YouTubeUrl TEXT
            )";

        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_songs_album ON Songs(Album);
            CREATE INDEX IF NOT EXISTS idx_songs_filename ON Songs(Filename);
            CREATE INDEX IF NOT EXISTS idx_songs_sourcetype ON Songs(SourceType);
            CREATE INDEX IF NOT EXISTS idx_songs_youtubeid ON Songs(YouTubeId);
        ";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = createSongsTable;
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = createIndexes;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> SaveSongAsync(Song song)
    {
        const string sql = @"
            INSERT INTO Songs (Filename, Title, Artists, Performers, Album, TrackNumber, 
                NAlbumTracks, DiscNumber, Year, Duration, Gain, Peak, MTime, CoverImagePath,
                SourceType, YouTubeId, YouTubeUrl)
            VALUES (@Filename, @Title, @Artists, @Performers, @Album, @TrackNumber, 
                @NAlbumTracks, @DiscNumber, @Year, @Duration, @Gain, @Peak, @MTime, @CoverImagePath,
                @SourceType, @YouTubeId, @YouTubeUrl)
            ON CONFLICT(Filename) DO UPDATE SET
                Title = @Title, Artists = @Artists, Performers = @Performers, Album = @Album,
                TrackNumber = @TrackNumber, NAlbumTracks = @NAlbumTracks, DiscNumber = @DiscNumber,
                Year = @Year, Duration = @Duration, Gain = @Gain, Peak = @Peak, MTime = @MTime,
                CoverImagePath = @CoverImagePath, SourceType = @SourceType, YouTubeId = @YouTubeId,
                YouTubeUrl = @YouTubeUrl
            RETURNING Id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Filename", song.Filename);
        cmd.Parameters.AddWithValue("@Title", song.Title);
        cmd.Parameters.AddWithValue("@Artists", JsonSerializer.Serialize(song.Artists));
        cmd.Parameters.AddWithValue("@Performers", JsonSerializer.Serialize(song.Performers));
        cmd.Parameters.AddWithValue("@Album", song.Album ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@TrackNumber", song.TrackNumber);
        cmd.Parameters.AddWithValue("@NAlbumTracks", song.NAlbumTracks);
        cmd.Parameters.AddWithValue("@DiscNumber", song.DiscNumber);
        cmd.Parameters.AddWithValue("@Year", song.Year ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Duration", song.Duration);
        cmd.Parameters.AddWithValue("@Gain", song.Gain);
        cmd.Parameters.AddWithValue("@Peak", song.Peak);
        cmd.Parameters.AddWithValue("@MTime", song.MTime);
        cmd.Parameters.AddWithValue("@CoverImagePath", song.CoverImagePath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@SourceType", (int)song.SourceType);
        cmd.Parameters.AddWithValue("@YouTubeId", song.YouTubeId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@YouTubeUrl", song.YouTubeUrl ?? (object)DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<List<Song>> GetAllSongsAsync()
    {
        const string sql = "SELECT * FROM Songs ORDER BY Artists, Album, DiscNumber, TrackNumber";
        var songs = new List<Song>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            songs.Add(ReadSong(reader));
        }

        return songs;
    }

    public async Task<Song?> GetSongByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Songs WHERE Id = @Id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadSong(reader);
        }

        return null;
    }

    public async Task<Song?> GetSongByFilenameAsync(string filename)
    {
        const string sql = "SELECT * FROM Songs WHERE Filename = @Filename";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Filename", filename);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadSong(reader);
        }

        return null;
    }

    public async Task<Dictionary<string, List<Album>>> GetSongsGroupedByArtistAndAlbumAsync()
    {
        var songs = await GetAllSongsAsync();
        var result = new Dictionary<string, List<Album>>();

        // Group songs by artist
        var artistGroups = songs
            .SelectMany(song => song.Artists.Select(artist => new { Artist = artist, Song = song }))
            .GroupBy(x => x.Artist);

        foreach (var artistGroup in artistGroups)
        {
            var artist = string.IsNullOrEmpty(artistGroup.Key) ? "Unknown Artist" : artistGroup.Key;
            
            // Group songs by album name AND folder (not AlbumKey) to combine multi-disc albums
            var albumGroups = artistGroup
                .GroupBy(x => new { x.Song.Album, x.Song.Folder });

            var albums = new List<Album>();
            foreach (var albumGroup in albumGroups)
            {
                var albumSongs = albumGroup.Select(x => x.Song).ToList();
                var firstSong = albumSongs.First();
                
                var album = new Album
                {
                    Name = string.IsNullOrEmpty(albumGroup.Key.Album) ? "Unknown Album" : albumGroup.Key.Album,
                    Artists = new List<string> { artist },
                    Year = firstSong.Year,
                    Folder = firstSong.Folder,
                    Songs = albumSongs,
                    CoverImagePath = firstSong.CoverImagePath,
                    TotalTracks = albumSongs.Any() ? albumSongs.Max(s => s.NAlbumTracks) : 0
                };
                
                albums.Add(album);
            }

            result[artist] = albums.OrderBy(a => a.Year).ThenBy(a => a.Name).ToList();
        }

        return result;
    }

    public async Task<List<Song>> SearchSongsAsync(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return await GetAllSongsAsync();
        }

        var sql = @"
            SELECT * FROM Songs 
            WHERE Title LIKE @Query 
               OR Artists LIKE @Query 
               OR Album LIKE @Query
               OR Performers LIKE @Query
            ORDER BY Artists, Album, DiscNumber, TrackNumber";

        var songs = new List<Song>();
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Query", $"%{searchQuery}%");

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            songs.Add(ReadSong(reader));
        }

        return songs;
    }

    private static Song ReadSong(SqliteDataReader reader)
    {
        var song = new Song
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Filename = reader.GetString(reader.GetOrdinal("Filename")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Artists = JsonSerializer.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("Artists"))) ?? Array.Empty<string>(),
            Performers = JsonSerializer.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("Performers"))) ?? Array.Empty<string>(),
            Album = reader.IsDBNull(reader.GetOrdinal("Album")) ? string.Empty : reader.GetString(reader.GetOrdinal("Album")),
            TrackNumber = reader.GetInt32(reader.GetOrdinal("TrackNumber")),
            NAlbumTracks = reader.GetInt32(reader.GetOrdinal("NAlbumTracks")),
            DiscNumber = reader.GetInt32(reader.GetOrdinal("DiscNumber")),
            Year = reader.IsDBNull(reader.GetOrdinal("Year")) ? string.Empty : reader.GetString(reader.GetOrdinal("Year")),
            Duration = reader.GetInt32(reader.GetOrdinal("Duration")),
            Gain = reader.GetDouble(reader.GetOrdinal("Gain")),
            Peak = reader.GetDouble(reader.GetOrdinal("Peak")),
            MTime = reader.GetInt32(reader.GetOrdinal("MTime")),
            CoverImagePath = reader.IsDBNull(reader.GetOrdinal("CoverImagePath")) ? null : reader.GetString(reader.GetOrdinal("CoverImagePath"))
        };

        // Read YouTube-specific fields if they exist (for backwards compatibility)
        try
        {
            var sourceTypeOrdinal = reader.GetOrdinal("SourceType");
            if (!reader.IsDBNull(sourceTypeOrdinal))
            {
                song.SourceType = (SongSourceType)reader.GetInt32(sourceTypeOrdinal);
            }
        }
        catch (IndexOutOfRangeException)
        {
            // Column doesn't exist in older database schema
        }

        try
        {
            var youtubeIdOrdinal = reader.GetOrdinal("YouTubeId");
            if (!reader.IsDBNull(youtubeIdOrdinal))
            {
                song.YouTubeId = reader.GetString(youtubeIdOrdinal);
            }
        }
        catch (IndexOutOfRangeException)
        {
            // Column doesn't exist in older database schema
        }

        try
        {
            var youtubeUrlOrdinal = reader.GetOrdinal("YouTubeUrl");
            if (!reader.IsDBNull(youtubeUrlOrdinal))
            {
                song.YouTubeUrl = reader.GetString(youtubeUrlOrdinal);
            }
        }
        catch (IndexOutOfRangeException)
        {
            // Column doesn't exist in older database schema
        }

        return song;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
