namespace HeroicImporter;

using Newtonsoft.Json;

public class Program
{
    static async Task Main()
    {
        var config = await LoadConfigAsync();

        // Allow DB path override for SQLite via environment variable (e.g. SQLITE_DB_PATH)
        string connectionString = config.ConnectionString;
        if (config.DatabaseProvider.ToLower() == "sqlite")
        {
            var envPath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                // Replace Data Source in connection string
                var builder = new System.Data.SQLite.SQLiteConnectionStringBuilder(connectionString)
                {
                    DataSource = envPath
                };
                connectionString = builder.ToString();
                Console.WriteLine($"[INFO] Using SQLite DB file at: {envPath}");
            }
        }

        string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        foreach (var file in Directory.GetFiles(Path.Combine(appDataRoaming, config.AppDataLibraryPath), "*_library.json"))
        {
            Console.WriteLine($"Processing file: {file}");
            string jsonContent = await File.ReadAllTextAsync(file);
            var importer = new GameImporter(connectionString, config.DatabaseProvider);
            await importer.ImportGames(jsonContent);
        }
    }

    private static string? FindConfigInParentDirs(string fileName)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }
        
        return null;
    }

    private static async Task<HeroicConfig> LoadConfigAsync()
    {
        string fileName = "heroicImporter_config.json";
        string? configPath = FindConfigInParentDirs(fileName)
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Could not find {fileName} in parent directories or executable directory.");
        }

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonConvert.DeserializeObject<HeroicConfig>(json) ?? throw new InvalidOperationException("Failed load configuration file.");
        return config;
    }
}