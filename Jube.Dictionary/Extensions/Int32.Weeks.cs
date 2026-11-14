// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Returns a TimeSpan representing the number of weeks.")]
        public static TimeSpan Weeks(this Int32 @this)
        {
            return TimeSpan.FromDays(@this * 7);
        }
    }
}
