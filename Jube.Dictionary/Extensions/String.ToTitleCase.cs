// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            public string ToTitleCase()
            {
                return new CultureInfo("en-US").TextInfo.ToTitleCase(@this);
            }

            public string ToTitleCase(CultureInfo cultureInfo)
            {
                return cultureInfo.TextInfo.ToTitleCase(@this);
            }
        }
    }
}
