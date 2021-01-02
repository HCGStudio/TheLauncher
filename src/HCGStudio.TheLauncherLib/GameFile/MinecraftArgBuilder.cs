using System.Collections.Generic;

namespace HCGStudio.TheLauncherLib.GameFile
{
    public class MinecraftArgBuilder
    {
        private readonly List<string> _args = new();

        public MinecraftArgBuilder Append(IArgProvider provider)
        {
            _args.AddRange(provider.EnumerateArg());
            return this;
        }

        public string[] Build()
        {
            return _args.ToArray();
        }
    }
}