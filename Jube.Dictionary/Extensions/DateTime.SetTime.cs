// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        extension(DateTime current)
        {
            public DateTime SetTime(int hour)
            {
                return current.SetTime(hour, 0, 0, 0);
            }

            public DateTime SetTime(int hour, int minute)
            {
                return current.SetTime(hour, minute, 0, 0);
            }

            public DateTime SetTime(int hour, int minute, int second)
            {
                return current.SetTime(hour, minute, second, 0);
            }

            public DateTime SetTime(int hour, int minute, int second, int millisecond)
            {
                return new DateTime(current.Year, current.Month, current.Day, hour, minute, second, millisecond);
            }
        }
    }
}
