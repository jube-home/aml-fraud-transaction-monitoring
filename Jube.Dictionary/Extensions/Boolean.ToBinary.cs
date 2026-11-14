// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Converts the boolean to a binary representation.")]
        public static byte ToBinary(this bool @this)
        {
            return Convert.ToByte(@this);
        }
    }
}
