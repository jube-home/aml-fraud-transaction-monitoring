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
            [Description("Converts the time to a different time zone.")]
            public DateTime ConvertTime([Description("The time zone to convert to.")] TimeZoneInfo destinationTimeZone)
            {
                return TimeZoneInfo.ConvertTime(dateTime, destinationTimeZone);
            }

            [Description("Converts the time from one time zone to another.")]
            public DateTime ConvertTime([Description("The time zone of the dateTime.")] TimeZoneInfo sourceTimeZone,
                [Description("The time zone to convert to.")] TimeZoneInfo destinationTimeZone)
            {
                return TimeZoneInfo.ConvertTime(dateTime, sourceTimeZone, destinationTimeZone);
            }
        }
    }
}
