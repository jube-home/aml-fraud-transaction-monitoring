// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Removes numbers from the string.")]
        public static string RemoveNumber(this string @this)
        {
            return new string(@this.ToCharArray().Where(x => !char.IsNumber(x)).ToArray());
        }
    }
}
