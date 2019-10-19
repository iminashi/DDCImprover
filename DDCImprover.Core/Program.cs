using System;
using System.Reflection;

namespace DDCImprover.Core
{
    public sealed class Program
    {
        private static string version;

        public static string Version
        {
            get
            {
                if (version != null)
                    return version;

                var thisAsm = Assembly.GetExecutingAssembly();
                var ver = thisAsm.GetName().Version;
                version = $"v{ver.Major}.{ver.Minor}.{ver.Build}";

                return version;
            }
        }
    }
}
