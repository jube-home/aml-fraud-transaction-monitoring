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
            public DateTime ConvertTime(TimeZoneInfo destinationTimeZone)
            {
                return TimeZoneInfo.ConvertTime(dateTime, destinationTimeZone);
            }

            public DateTime ConvertTime(TimeZoneInfo sourceTimeZone,
                TimeZoneInfo destinationTimeZone)
            {
                return TimeZoneInfo.ConvertTime(dateTime, sourceTimeZone, destinationTimeZone);
            }
        }
    }
}
