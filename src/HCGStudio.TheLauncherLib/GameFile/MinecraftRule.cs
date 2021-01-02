using System;
using System.Collections.Generic;

namespace HCGStudio.TheLauncherLib.GameFile
{
    public class MinecraftRule
    {
        public bool Allow { get; init; }
        public string System { get; init; } = string.Empty;

        public bool? IsAllowed()
        {
            switch (System)
            {
                case "osx":
                    if (OperatingSystem.IsMacOS())
                        return Allow;
                    break;
                case "windows":
                    if (OperatingSystem.IsWindows())
                        return Allow;
                    break;
                case "":
                    return Allow;
            }

            return null;
        }
    }

    public static class MinecraftRuleExtenstion
    {
        public static bool IsAllowed(this IEnumerable<MinecraftRule> rules)
        {
            bool? result = null;
            foreach (var rule in rules)
            {
                var allow = rule.IsAllowed();
                result ??= allow;
                if (allow != null)
                    result |= (bool) allow;
            }

            return result ?? false;
        }
    }
}