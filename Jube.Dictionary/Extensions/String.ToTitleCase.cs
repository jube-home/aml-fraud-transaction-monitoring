// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string @this)
        {
            [Description("Converts the string to title case.")]
            public string ToTitleCase()
            {
                return new CultureInfo("en-US").TextInfo.ToTitleCase(@this);
            }

            [Description("Converts to title case using specified culture.")]
            public string ToTitleCase([Description("Information describing the culture.")] CultureInfo cultureInfo)
            {
                return cultureInfo.TextInfo.ToTitleCase(@this);
            }
        }
    }
}
