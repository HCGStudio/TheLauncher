using System.Collections.Generic;

namespace HCGStudio.TheLauncherLib.GameFile
{
    public interface IArgProvider
    {
        public IEnumerable<string> EnumerateArg();
    }
}