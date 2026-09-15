// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string ExtractLetter(this string @this)
        {
            return new string(@this.ToCharArray().Where(x => char.IsLetter(x)).ToArray());
        }
    }
}
