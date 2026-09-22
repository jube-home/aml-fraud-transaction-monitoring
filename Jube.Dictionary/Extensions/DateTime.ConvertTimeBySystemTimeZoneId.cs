// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;
    using String = string;
    using TimeZoneInfo = TimeZoneInfo;

    public static partial class Extensions
    {
        extension(DateTime dateTime)
        {
            public DateTime ConvertTimeBySystemTimeZoneId(String destinationTimeZoneId)
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, destinationTimeZoneId);
            }

            public DateTime ConvertTimeBySystemTimeZoneId(String sourceTimeZoneId,
                String destinationTimeZoneId)
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, sourceTimeZoneId, destinationTimeZoneId);
            }
        }
    }
}
