// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using DateTime = DateTime;

    public static partial class Extensions
    {
        [Description("Calculates the age from the given date.")]
        public static int Age(this DateTime @this)
        {
            if (DateTime.Today.Month < @this.Month ||
                (DateTime.Today.Month == @this.Month &&
                 DateTime.Today.Day < @this.Day))
            {
                return DateTime.Today.Year - @this.Year - 1;
            }

            return DateTime.Today.Year - @this.Year;
        }
    }
}
