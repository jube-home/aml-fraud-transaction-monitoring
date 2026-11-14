// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using BitConverter = BitConverter;
    using Int32 = int;

    public static partial class Extensions
    {
        [Description("Returns the value as a byte array.")]
        public static byte[] GetBytes(this Int32 value)
        {
            return BitConverter.GetBytes(value);
        }
    }
}
