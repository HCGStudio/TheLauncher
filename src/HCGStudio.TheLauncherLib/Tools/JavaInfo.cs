namespace HCGStudio.TheLauncherLib.Tools
{
    public record JavaInfo
    {
        public string Location { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string Distribution { get; init; } = string.Empty;
        public bool IsOpen { get; init; }
    }
}