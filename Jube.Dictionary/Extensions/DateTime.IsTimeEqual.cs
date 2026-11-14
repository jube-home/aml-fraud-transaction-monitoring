// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        [Description("Checks if two dates have the same time part.")]
        public static bool IsTimeEqual(this DateTime time, [Description("The time to compare.")] DateTime timeToCompare)
        {
            return time.TimeOfDay == timeToCompare.TimeOfDay;
        }
    }
}
