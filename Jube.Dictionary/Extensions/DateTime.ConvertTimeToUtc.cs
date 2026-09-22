// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;
    using TimeZoneInfo = TimeZoneInfo;

    public static partial class Extensions
    {
        extension(DateTime dateTime)
        {
            public DateTime ConvertTimeToUtc()
            {
                return TimeZoneInfo.ConvertTimeToUtc(dateTime);
            }

            public DateTime ConvertTimeToUtc(TimeZoneInfo sourceTimeZone)
            {
                return TimeZoneInfo.ConvertTimeToUtc(dateTime, sourceTimeZone);
            }
        }
    }
}
