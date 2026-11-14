// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        [Description("Returns the next day at the same time.")]
        public static DateTime Tomorrow(this DateTime @this)
        {
            return @this.AddDays(1);
        }
    }
}
