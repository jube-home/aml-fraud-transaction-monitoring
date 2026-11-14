// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using TimeSpan = TimeSpan;

    public static partial class Extensions
    {
        [Description("Returns a TimeSpan representing the number of days.")]
        public static TimeSpan FromDays(this Double value)
        {
            return TimeSpan.FromDays(value);
        }
    }
}
