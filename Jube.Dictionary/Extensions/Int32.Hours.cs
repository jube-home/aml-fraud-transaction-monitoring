// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Returns a TimeSpan representing the number of hours.")]
        public static TimeSpan Hours(this Int32 @this)
        {
            return TimeSpan.FromHours(@this);
        }
    }
}
