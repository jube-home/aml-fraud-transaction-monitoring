// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static bool IsTimeEqual(this DateTime time, DateTime timeToCompare)
        {
            return time.TimeOfDay == timeToCompare.TimeOfDay;
        }
    }
}
