using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace HCGStudio.TheLauncherLib.Tools
{
    public static class SymbolicLink
    {
        [SupportedOSPlatform("windows")]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateSymbolicLinkW(string lpSymlinkFileName, 
            string lpTargetFileName, int flags);

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [DllImport("libc")]
        private static extern int symlink(string target, string linkPath);
        public static bool Create(string source, string dest)
        {
            if (OperatingSystem.IsWindows())
                return CreateSymbolicLinkW(dest, source, 0);
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                return symlink(source, dest) == 0;
            throw new NotSupportedException();
        }
    }
}
