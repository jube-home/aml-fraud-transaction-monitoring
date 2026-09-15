// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static TimeSpan ToEpochTimeSpan(this DateTime @this)
        {
            return @this.Subtract(new DateTime(1970, 1, 1));
        }
    }
}
