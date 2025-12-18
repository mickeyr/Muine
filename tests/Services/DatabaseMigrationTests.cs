using Muine.Core.Services;
using Muine.Core.Models;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Muine.Tests.Services;

public class DatabaseMigrationTests
{
    [Fact]
    public async Task Migration_AddsYouTubeColumnsToExistingDatabase()
    {
        // Arrange - Create a database with old schema (without YouTube columns)
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_migration_{Guid.NewGuid()}.db");
        
        try
        {
            // Create old database schema without YouTube columns
            await CreateOldDatabaseSchemaAsync(dbPath);
            
            // Verify columns don't exist in old schema
            var columnsBeforeMigration = await GetTableColumnsAsync(dbPath);
            Assert.DoesNotContain("SourceType", columnsBeforeMigration);
            Assert.DoesNotContain("YouTubeId", columnsBeforeMigration);
            Assert.DoesNotContain("YouTubeUrl", columnsBeforeMigration);
            
            // Act - Initialize database service (should trigger migration)
            using var dbService = new MusicDatabaseService(dbPath);
            await dbService.InitializeAsync();
            
            // Assert - Verify columns were added
            var columnsAfterMigration = await GetTableColumnsAsync(dbPath);
            Assert.Contains("SourceType", columnsAfterMigration);
            Assert.Contains("YouTubeId", columnsAfterMigration);
            Assert.Contains("YouTubeUrl", columnsAfterMigration);
        }
        finally
        {
            // Cleanup
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task Migration_WorksWithNewDatabase()
    {
        // Arrange - Fresh database
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_new_{Guid.NewGuid()}.db");
        
        try
        {
            // Act - Initialize new database (should create table with all columns)
            using var dbService = new MusicDatabaseService(dbPath);
            await dbService.InitializeAsync();
            
            // Assert - Verify all columns exist
            var columns = await GetTableColumnsAsync(dbPath);
            Assert.Contains("SourceType", columns);
            Assert.Contains("YouTubeId", columns);
            Assert.Contains("YouTubeUrl", columns);
        }
        finally
        {
            // Cleanup
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task Migration_AllowsSavingYouTubeSongs()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_youtube_{Guid.NewGuid()}.db");
        
        try
        {
            // Create old database schema
            await CreateOldDatabaseSchemaAsync(dbPath);
            
            // Initialize with migration
            using var dbService = new MusicDatabaseService(dbPath);
            await dbService.InitializeAsync();
            
            // Act - Save a YouTube song
            var youtubeSong = new Song
            {
                Title = "Test YouTube Song",
                Artists = new[] { "Test Artist" },
                Filename = "https://www.youtube.com/watch?v=test",
                Duration = 180,
                SourceType = SongSourceType.YouTube,
                YouTubeId = "test123",
                YouTubeUrl = "https://www.youtube.com/watch?v=test"
            };
            
            var songId = await dbService.SaveSongAsync(youtubeSong);
            
            // Assert - Verify song was saved and can be retrieved
            var retrievedSong = await dbService.GetSongByIdAsync(songId);
            Assert.NotNull(retrievedSong);
            Assert.Equal(SongSourceType.YouTube, retrievedSong.SourceType);
            Assert.Equal("test123", retrievedSong.YouTubeId);
            Assert.Equal("https://www.youtube.com/watch?v=test", retrievedSong.YouTubeUrl);
        }
        finally
        {
            // Cleanup
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static async Task CreateOldDatabaseSchemaAsync(string dbPath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();
        
        // Create table without YouTube columns (old schema)
        var createOldTable = @"
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
                CoverImagePath TEXT
            )";
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = createOldTable;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> GetTableColumnsAsync(string dbPath)
    {
        var columns = new List<string>();
        
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(Songs)";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1)); // Column name is at index 1
        }
        
        return columns;
    }
}
