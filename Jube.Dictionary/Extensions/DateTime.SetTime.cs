// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        extension(DateTime current)
        {
            [Description("Sets the time of the date (hour only).")]
            public DateTime SetTime([Description("The hour.")] int hour)
            {
                return current.SetTime(hour, 0, 0, 0);
            }

            [Description("Sets the time of the date (hour and minute).")]
            public DateTime SetTime([Description("The hour.")] int hour, [Description("The minute.")] int minute)
            {
                return current.SetTime(hour, minute, 0, 0);
            }

            [Description("Sets the time of the date (hour, minute, second).")]
            public DateTime SetTime([Description("The hour.")] int hour,
                [Description("The minute.")] int minute,
                [Description("The second.")] int second)
            {
                return current.SetTime(hour, minute, second, 0);
            }

            [Description("Sets the time of the date (hour, minute, second, millisecond).")]
            public DateTime SetTime([Description("The hour.")] int hour,
                [Description("The minute.")] int minute,
                [Description("The second.")] int second,
                [Description("The millisecond.")] int millisecond)
            {
                return new DateTime(current.Year, current.Month, current.Day, hour, minute, second, millisecond);
            }
        }
    }
}
