// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    using Boolean = bool;
    using DateTime = DateTime;
#pragma warning disable CS0618
    using TimeZone = TimeZone;

    public static partial class Extensions
    {
        public static Boolean IsDaylightSavingTime(this DateTime time, DaylightTime daylightTimes)
        {
            return TimeZone.IsDaylightSavingTime(time, daylightTimes);
        }
    }
#pragma warning restore CS0618
}
