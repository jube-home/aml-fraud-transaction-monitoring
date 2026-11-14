// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        [Description("Converts the date to an epoch time span.")]
        public static TimeSpan ToEpochTimeSpan(this DateTime @this)
        {
            return @this.Subtract(new DateTime(1970, 1, 1));
        }
    }
}
