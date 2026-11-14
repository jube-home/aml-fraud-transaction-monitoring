// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using Double = double;
    using TimeSpan = TimeSpan;

    public static partial class Extensions
    {
        [Description("Returns a TimeSpan representing the number of hours.")]
        public static TimeSpan FromHours(this Double value)
        {
            return TimeSpan.FromHours(value);
        }
    }
}
