namespace HCGStudio.TheLauncherLib.GameFile
{
    public class JvmOption
    {
        public ulong MinMemoryInMegaBytes { get; set; } = 128;
        public ulong MaxMemoryInMegaBytes { get; set; } = 4096;
        public string GarbageCollectionType { get; set; } = JvmGcOption.G1Gc;
    }
}