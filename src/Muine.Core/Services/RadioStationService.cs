using Microsoft.Data.Sqlite;
using Muine.Core.Models;
using System.Text.Json;

namespace Muine.Core.Services;

public class RadioStationService : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public RadioStationService(string databasePath)
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
        var createRadioStationsTable = @"
            CREATE TABLE IF NOT EXISTS RadioStations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Url TEXT NOT NULL UNIQUE,
                Genre TEXT,
                Location TEXT,
                Description TEXT,
                Website TEXT,
                Bitrate INTEGER,
                Category TEXT,
                ParentCategory TEXT,
                DateAdded TEXT NOT NULL,
                LastPlayed TEXT
            )";

        var createCategoriesTable = @"
            CREATE TABLE IF NOT EXISTS RadioCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ParentCategory TEXT,
                DisplayOrder INTEGER DEFAULT 0,
                UNIQUE(Name, ParentCategory)
            )";

        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_radiostations_category ON RadioStations(Category);
            CREATE INDEX IF NOT EXISTS idx_radiostations_parentcategory ON RadioStations(ParentCategory);
            CREATE INDEX IF NOT EXISTS idx_radiocategories_parent ON RadioCategories(ParentCategory);
        ";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = createRadioStationsTable;
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = createCategoriesTable;
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = createIndexes;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> SaveStationAsync(RadioStation station)
    {
        const string sql = @"
            INSERT INTO RadioStations (Name, Url, Genre, Location, Description, Website, 
                Bitrate, Category, ParentCategory, DateAdded, LastPlayed)
            VALUES (@Name, @Url, @Genre, @Location, @Description, @Website, 
                @Bitrate, @Category, @ParentCategory, @DateAdded, @LastPlayed)
            ON CONFLICT(Url) DO UPDATE SET
                Name = @Name, Genre = @Genre, Location = @Location, 
                Description = @Description, Website = @Website,
                Bitrate = @Bitrate, Category = @Category, ParentCategory = @ParentCategory
            RETURNING Id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Name", station.Name);
        cmd.Parameters.AddWithValue("@Url", station.Url);
        cmd.Parameters.AddWithValue("@Genre", station.Genre ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Location", station.Location ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", station.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Website", station.Website ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Bitrate", station.Bitrate);
        cmd.Parameters.AddWithValue("@Category", station.Category ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ParentCategory", station.ParentCategory ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DateAdded", station.DateAdded.ToString("o"));
        cmd.Parameters.AddWithValue("@LastPlayed", station.LastPlayed?.ToString("o") ?? (object)DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<List<RadioStation>> GetAllStationsAsync()
    {
        const string sql = "SELECT * FROM RadioStations ORDER BY ParentCategory, Category, Name";
        var stations = new List<RadioStation>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stations.Add(ReadStation(reader));
        }

        return stations;
    }

    public async Task<RadioStation?> GetStationByIdAsync(int id)
    {
        const string sql = "SELECT * FROM RadioStations WHERE Id = @Id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadStation(reader);
        }

        return null;
    }

    public async Task<RadioStation?> GetStationByUrlAsync(string url)
    {
        const string sql = "SELECT * FROM RadioStations WHERE Url = @Url";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Url", url);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadStation(reader);
        }

        return null;
    }

    public async Task<Dictionary<string, List<RadioStation>>> GetStationsGroupedByCategoryAsync()
    {
        var stations = await GetAllStationsAsync();
        var result = new Dictionary<string, List<RadioStation>>();

        foreach (var station in stations)
        {
            var categoryKey = station.FullCategory;
            if (string.IsNullOrEmpty(categoryKey))
            {
                categoryKey = "Uncategorized";
            }

            if (!result.ContainsKey(categoryKey))
            {
                result[categoryKey] = new List<RadioStation>();
            }

            result[categoryKey].Add(station);
        }

        return result;
    }

    public async Task<List<RadioStation>> SearchStationsAsync(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return await GetAllStationsAsync();
        }

        var sql = @"
            SELECT * FROM RadioStations 
            WHERE Name LIKE @Query 
               OR Genre LIKE @Query 
               OR Location LIKE @Query
               OR Description LIKE @Query
            ORDER BY ParentCategory, Category, Name";

        var stations = new List<RadioStation>();
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Query", $"%{searchQuery}%");

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stations.Add(ReadStation(reader));
        }

        return stations;
    }

    public async Task DeleteStationAsync(int id)
    {
        const string sql = "DELETE FROM RadioStations WHERE Id = @Id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateLastPlayedAsync(int id)
    {
        const string sql = "UPDATE RadioStations SET LastPlayed = @LastPlayed WHERE Id = @Id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@LastPlayed", DateTime.UtcNow.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    // Category operations
    public async Task<int> SaveCategoryAsync(RadioCategory category)
    {
        const string sql = @"
            INSERT INTO RadioCategories (Name, ParentCategory, DisplayOrder)
            VALUES (@Name, @ParentCategory, @DisplayOrder)
            ON CONFLICT(Name, ParentCategory) DO UPDATE SET
                DisplayOrder = @DisplayOrder
            RETURNING Id";

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Name", category.Name);
        cmd.Parameters.AddWithValue("@ParentCategory", category.ParentCategory ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<List<RadioCategory>> GetAllCategoriesAsync()
    {
        const string sql = "SELECT * FROM RadioCategories ORDER BY ParentCategory, DisplayOrder, Name";
        var categories = new List<RadioCategory>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(ReadCategory(reader));
        }

        return categories;
    }

    public async Task<List<RadioCategory>> GetRootCategoriesAsync()
    {
        const string sql = "SELECT * FROM RadioCategories WHERE ParentCategory IS NULL OR ParentCategory = '' ORDER BY DisplayOrder, Name";
        var categories = new List<RadioCategory>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(ReadCategory(reader));
        }

        return categories;
    }

    public async Task<List<RadioCategory>> GetSubCategoriesAsync(string parentCategory)
    {
        const string sql = "SELECT * FROM RadioCategories WHERE ParentCategory = @ParentCategory ORDER BY DisplayOrder, Name";
        var categories = new List<RadioCategory>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@ParentCategory", parentCategory);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(ReadCategory(reader));
        }

        return categories;
    }

    private static RadioStation ReadStation(SqliteDataReader reader)
    {
        return new RadioStation
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Url = reader.GetString(reader.GetOrdinal("Url")),
            Genre = reader.IsDBNull(reader.GetOrdinal("Genre")) ? string.Empty : reader.GetString(reader.GetOrdinal("Genre")),
            Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? string.Empty : reader.GetString(reader.GetOrdinal("Location")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
            Website = reader.IsDBNull(reader.GetOrdinal("Website")) ? string.Empty : reader.GetString(reader.GetOrdinal("Website")),
            Bitrate = reader.GetInt32(reader.GetOrdinal("Bitrate")),
            Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? string.Empty : reader.GetString(reader.GetOrdinal("Category")),
            ParentCategory = reader.IsDBNull(reader.GetOrdinal("ParentCategory")) ? string.Empty : reader.GetString(reader.GetOrdinal("ParentCategory")),
            DateAdded = DateTime.ParseExact(reader.GetString(reader.GetOrdinal("DateAdded")), "o", System.Globalization.CultureInfo.InvariantCulture),
            LastPlayed = reader.IsDBNull(reader.GetOrdinal("LastPlayed")) ? null : DateTime.ParseExact(reader.GetString(reader.GetOrdinal("LastPlayed")), "o", System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static RadioCategory ReadCategory(SqliteDataReader reader)
    {
        return new RadioCategory
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            ParentCategory = reader.IsDBNull(reader.GetOrdinal("ParentCategory")) ? string.Empty : reader.GetString(reader.GetOrdinal("ParentCategory")),
            DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
        };
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
