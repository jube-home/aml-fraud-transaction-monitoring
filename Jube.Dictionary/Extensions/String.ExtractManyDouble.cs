// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Extracts all doubles from the string, reading . as the decimal point whatever the server culture.")]
        public static double[] ExtractManyDouble(this string @this)
        {
            return Regex.Matches(@this, @"[-]?\d+(\.\d+)?")
                .Select(x => Convert.ToDouble(x.Value, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();
        }
    }
}