// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        [Description("Checks if two dates have the same date part.")]
        public static bool IsDateEqual(this DateTime date, [Description("The date to compare.")] DateTime dateToCompare)
        {
            return date.Date == dateToCompare.Date;
        }
    }
}
