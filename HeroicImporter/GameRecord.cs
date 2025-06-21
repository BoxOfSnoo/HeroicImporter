namespace HeroicImporter;

public class GameRecord
{
    public required string Title { get; set; }
    public required string Runner { get; set; }
    public bool CanRunOffline { get; set; }
    public bool IsLinuxNative { get; set; }
    public double InstallSize { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? StoreUrl { get; set; }
    public List<string>? Genres { get; set; }
    public DateTime? ReleaseDate { get; set; }
}
