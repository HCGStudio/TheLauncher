using System;
using System.Collections.Generic;

namespace HCGStudio.TheLauncherLib.GameFile
{
    public class JvmArgProvider : IArgProvider
    {
        private readonly List<string> _args = new();

        public JvmArgProvider(Action<JvmOption>? configure = null)
        {
            var option = new JvmOption();
            configure?.Invoke(option);
            _args.Add($"-Xmx{option.MaxMemoryInMegaBytes}m");
            _args.Add($"-Xmn{option.MinMemoryInMegaBytes}m");
            _args.Add($"-XX:+Use{option.GarbageCollectionType}");
        }

        public IEnumerable<string> EnumerateArg()
        {
            return _args;
        }

        public void ManuallyAdd(string arg)
        {
            _args.Add(arg);
        }
    }
}