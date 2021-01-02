namespace HCGStudio.TheLauncherLib.GameFile
{
    public record MinecraftLib
    {
        public string Path { get; init; } = string.Empty;
        public string Sha { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
    }
}