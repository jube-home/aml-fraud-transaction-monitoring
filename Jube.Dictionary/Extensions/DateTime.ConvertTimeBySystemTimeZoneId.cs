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
            [Description("Converts the time using time zone identifiers.")]
            public DateTime ConvertTimeBySystemTimeZoneId([Description("The identifier of the destination time zone.")] String destinationTimeZoneId)
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, destinationTimeZoneId);
            }

            [Description("Converts the time between time zones using identifiers.")]
            public DateTime ConvertTimeBySystemTimeZoneId([Description("The identifier of the source time zone.")] String sourceTimeZoneId,
                [Description("The identifier of the destination time zone.")] String destinationTimeZoneId)
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, sourceTimeZoneId, destinationTimeZoneId);
            }
        }
    }
}
