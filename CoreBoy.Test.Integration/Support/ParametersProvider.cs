using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreBoy.Test.Integration.Support
{
    public static class ParametersProvider
    {
        private static List<string> EXCLUDES = new List<string>
        {
            "-mgb.gb",
            "-sgb.gb",
            "-sgb2.gb",
            "-S.gb",
            "-A.gb"
        };

        public static object[][] getParameters(string dirName)
        {
            return getParameters(dirName, EXCLUDES, SearchOption.TopDirectoryOnly);
        }

        public static object[][] getParameters(string dirName, SearchOption searchOption)
        {
            return getParameters(dirName, EXCLUDES, SearchOption.AllDirectories);
        }

        public static object[][] getParameters(string dirName, List<string> excludes, SearchOption? searchOption)
        {
            searchOption ??= SearchOption.AllDirectories;

            var root = "C:\\Users\\David Whitney\\OneDrive\\Desktop\\coffee-gb-netcore\\CoreBoy.Test.Integration\\roms";
            var dir = Path.Combine(root, dirName);
            var paths = Directory.EnumerateFiles(dir, "*.gb", searchOption.Value).ToList();
            paths.RemoveAll(path => excludes.Any(path.Contains));

            var returnVal = paths.Select(path => new List<object> { path }.ToArray()).ToArray();

            return returnVal;
        }

    }
}