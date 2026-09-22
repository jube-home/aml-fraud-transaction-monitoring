// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            public void SaveAs(string fileName, bool append = false)
            {
                using TextWriter tw = new StreamWriter(fileName, append);
                tw.Write(@this);
            }

            public void SaveAs(FileInfo file, bool append = false)
            {
                using TextWriter tw = new StreamWriter(file.FullName, append);
                tw.Write(@this);
            }
        }
    }
}
