namespace HeroicImporter;

using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

public class GameImporter(string connectionString)
{
    public async Task ImportGames(string jsonContent)
    {
        var records = ExtractGameRecords(jsonContent);

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var record in records)
        {
            if (!await RecordExists(connection, record))
            {
                await InsertRecord(connection, record);
            }
        }
    }

    private static async Task<bool> RecordExists(MySqlConnection connection, GameRecord record)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM games WHERE title = @title AND runner = @runner";
        cmd.Parameters.AddWithValue("@title", record.Title);
        cmd.Parameters.AddWithValue("@runner", record.Runner);

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    private static async Task InsertRecord(MySqlConnection connection, GameRecord record)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
                INSERT INTO games 
                (title, runner, can_run_offline, is_linux_native, install_size, 
                 description, short_description, store_url, genres, release_date, date_added)
                VALUES
                (@title, @runner, @canRunOffline, @isLinuxNative, @installSize,
                 @description, @shortDescription, @storeUrl, @genres, @releaseDate, @dateAdded)";

        cmd.Parameters.AddWithValue("@title", record.Title);
        cmd.Parameters.AddWithValue("@runner", record.Runner);
        cmd.Parameters.AddWithValue("@canRunOffline", record.CanRunOffline);
        cmd.Parameters.AddWithValue("@isLinuxNative", record.IsLinuxNative);
        cmd.Parameters.AddWithValue("@installSize", record.InstallSize);
        cmd.Parameters.AddWithValue("@description", record.Description != null ? record.Description : DBNull.Value);
        cmd.Parameters.AddWithValue("@shortDescription", record.ShortDescription != null ? record.ShortDescription : DBNull.Value);
        cmd.Parameters.AddWithValue("@storeUrl", record.StoreUrl != null ? record.StoreUrl : DBNull.Value);
        cmd.Parameters.AddWithValue("@genres", string.Join(",", record.Genres ?? []));
        cmd.Parameters.AddWithValue("@releaseDate", record.ReleaseDate.HasValue ? record.ReleaseDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@dateAdded", DateTime.UtcNow);

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