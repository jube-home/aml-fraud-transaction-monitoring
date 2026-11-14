// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;
    using Double = double;

    public static partial class Extensions
    {
        [Description("Converts an OLE Automation date to a DateTime.")]
        public static DateTime FromOADate(this Double d)
        {
            return DateTime.FromOADate(d);
        }
    }
}