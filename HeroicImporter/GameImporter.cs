namespace HeroicImporter;

using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Data.SQLite;

public class GameImporter(string connectionString, string provider = "mysql")
{
    private readonly string _provider = provider.ToLower();

    public async Task ImportGames(string jsonContent)
    {
        var records = ExtractGameRecords(jsonContent);

        using var connection = CreateConnection();
        await connection.OpenAsync();

        foreach (var record in records)
        {
            if (!await RecordExists(connection, record))
            {
                await InsertRecord(connection, record);
            }
        }
    }

    private DbConnection CreateConnection()
    {
        return _provider switch
        {
            "mysql" => new MySqlConnection(connectionString),
            "sqlite" => new SQLiteConnection(connectionString),
            _ => throw new NotSupportedException($"Provider '{_provider}' is not supported.")
        };
    }

    private string Param(string name) =>_provider == "sqlite" ? $":{name}" : $"@{name}";

    private void AddParameter(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = Param(name);
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private async Task<bool> RecordExists(DbConnection connection, GameRecord record)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM games WHERE title = {Param("title")} AND runner = {Param("runner")}";
        AddParameter(cmd, "title", record.Title);
        AddParameter(cmd, "runner", record.Runner);

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    private async Task InsertRecord(DbConnection connection, GameRecord record)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
                INSERT INTO games 
                (title, runner, can_run_offline, is_linux_native, install_size, 
                 description, short_description, store_url, genres, release_date, date_added)
                VALUES
                ({Param("title")}, {Param("runner")}, {Param("canRunOffline")}, {Param("isLinuxNative")}, {Param("installSize")},
                 {Param("description")}, {Param("shortDescription")}, {Param("storeUrl")}, {Param("genres")}, {Param("releaseDate")}, {Param("dateAdded")})";

        AddParameter(cmd, "title", record.Title);
        AddParameter(cmd, "runner", record.Runner);
        AddParameter(cmd, "canRunOffline", record.CanRunOffline);
        AddParameter(cmd, "isLinuxNative", record.IsLinuxNative);
        AddParameter(cmd, "installSize", record.InstallSize);
        AddParameter(cmd, "description", record.Description ?? (object)DBNull.Value);
        AddParameter(cmd, "shortDescription", record.ShortDescription ?? (object)DBNull.Value);
        AddParameter(cmd, "storeUrl", record.StoreUrl ?? (object)DBNull.Value);
        AddParameter(cmd, "genres", string.Join(",", record.Genres ?? []));
        AddParameter(cmd, "releaseDate", record.ReleaseDate.HasValue ? record.ReleaseDate.Value : (object)DBNull.Value);
        AddParameter(cmd, "dateAdded", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    private static List<GameRecord> ExtractGameRecords(string jsonContent)
    {
        var records = new List<GameRecord>();
        var jsonObject = JObject.Parse(jsonContent);

        // Process GOG games
        var gogGames = jsonObject["games"]?.ToList() ?? [];
        foreach (var game in gogGames)
        {
            records.Add(ParseGameRecord(game));
        }

        // Process Epic and Amazon games
        var libraryGames = jsonObject["library"]?.ToList() ?? [];
        foreach (var game in libraryGames)
        {
            records.Add(ParseGameRecord(game));
        }

        return records;
    }

    private static GameRecord ParseGameRecord(JToken game)
    {
        var record = new GameRecord
        {
            Title = game["title"]?.ToString() ?? string.Empty,
            Runner = game["runner"]?.ToString() ?? string.Empty,
            CanRunOffline = game["canRunOffline"]?.Value<bool>() ?? false,
            IsLinuxNative = game["is_linux_native"]?.Value<bool>() ?? false,
            Description = game["extra"]?["about"]?["description"]?.ToString(),
            ShortDescription = game["extra"]?["about"]?["shortDescription"]?.ToString(),
            StoreUrl = game["store_url"]?.ToString() ?? game["extra"]?["storeUrl"]?.ToString(),
            Genres = game["extra"]?["genres"]?.ToObject<List<string>>() ?? [],
        };

        // Parse install size
        var installSizeStr = game["install"]?["install_size"]?.ToString();
        if (!string.IsNullOrEmpty(installSizeStr))
        {
            installSizeStr = installSizeStr.Replace(" GiB", "");
            _ = double.TryParse(installSizeStr, out double size);
            record.InstallSize = size;
        }

        // Parse release date
        var releaseDateStr = game["extra"]?["releaseDate"]?.ToString();
        if (!string.IsNullOrEmpty(releaseDateStr))
        {
            _ = DateTime.TryParse(releaseDateStr, out DateTime releaseDate);
            record.ReleaseDate = releaseDate;
        }

        return record;
    }
}