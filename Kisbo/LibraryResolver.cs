using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Kisbo
{
    internal static class LibraryResolver
    {
        public static void Init(Type resourceType)
        {
            RootNamespace = resourceType.Namespace;
            if (RootNamespace.Contains("."))
                RootNamespace = RootNamespace.Substring(0, RootNamespace.IndexOf('.'));

            var bytesType = typeof(byte[]);
            ResourceMethods = resourceType
                              .GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty)
                              .Where(e => e.PropertyType == bytesType)
                              .ToArray();
            ResourcesNames = ResourceMethods.Select(e => e.Name.Replace('_', '.')).ToArray();

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static string           RootNamespace;
        private static string[]         ResourcesNames;
        private static PropertyInfo[]   ResourceMethods;
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name.Replace('-', '.');
            if (name.StartsWith(RootNamespace, StringComparison.OrdinalIgnoreCase)) return null;
            
            for (int i = 0; i < ResourcesNames.Length; ++i)
            {
                if (ResourcesNames[i].Contains(name))
                {
                    byte[] buff;

                    using (var comp   = new MemoryStream((byte[])ResourceMethods[i].GetValue(null, null)))
                    using (var uncomp = new MemoryStream(4096))
                    {
                        new GZipStream(comp, CompressionMode.Decompress).CopyTo(uncomp);
                        buff = uncomp.ToArray();
                    }

                    return Assembly.Load(buff);
                }
            }

            return null;
        }
    }
}
