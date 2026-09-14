// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startDayOfWeek = DayOfWeek.Sunday)
        {
            var end = dt;
            var endDayOfWeek = startDayOfWeek - 1;
            if (endDayOfWeek < 0)
            {
                endDayOfWeek = DayOfWeek.Saturday;
            }

            if (end.DayOfWeek != endDayOfWeek)
            {
                end = endDayOfWeek < end.DayOfWeek
                    ? end.AddDays(7 - (end.DayOfWeek - endDayOfWeek))
                    : end.AddDays(endDayOfWeek - end.DayOfWeek);
            }

            return new DateTime(end.Year, end.Month, end.Day, 23, 59, 59, 999);
        }
    }
}
