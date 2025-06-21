namespace HeroicImporter;

public class HeroicConfig
{
    public required string AppDataLibraryPath { get; set; }

    public string DatabaseProvider { get; set; } = "mysql";

    public required string ConnectionString { get; set; }
}