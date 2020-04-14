using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreBoy.Test.Unit.Integration.Support
{
    public static class ParametersProvider
    {
        private static List<String> EXCLUDES = new List<string>
        {
            "-mgb.gb",
            "-sgb.gb",
            "-sgb2.gb",
            "-S.gb",
            "-A.gb"
        };

        public static object[][] getParameters(String dirName)
        {
            return getParameters(dirName, EXCLUDES, 1);
        }

        public static object[][] getParameters(String dirName, int maxDepth)
        {
            return getParameters(dirName, EXCLUDES, maxDepth);
        }

        public static object[][] getParameters(String dirName, List<String> excludes, int maxDepth)
        {
            var root = "C:\\Users\\David Whitney\\OneDrive\\Desktop\\coffee-gb-netcore\\CoreBoy.Test.Unit\\roms";
            var dir = Path.Combine(root, dirName);
            var paths = Directory.EnumerateFiles(dir, "*.gb", SearchOption.AllDirectories).ToList();
            paths.RemoveAll(path => excludes.Any(path.Contains));

            var returnVal = paths.Select(path => new List<object> {path}.ToArray()).ToArray();

            return returnVal;
        }

    }
}