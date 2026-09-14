// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        public static bool IsDateEqual(this DateTime date, DateTime dateToCompare)
        {
            return date.Date == dateToCompare.Date;
        }
    }
}
