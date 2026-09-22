// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string? @this)
        {
            public string? Truncate(int maxLength)
            {
                const string suffix = "...";

                if (@this == null || @this.Length <= maxLength)
                {
                    return @this;
                }

                var strLength = maxLength - suffix.Length;
                return @this.Substring(0, strLength) + suffix;
            }

            public string? Truncate(int maxLength, string suffix)
            {
                if (@this == null || @this.Length <= maxLength)
                {
                    return @this;
                }

                var strLength = maxLength - suffix.Length;
                return @this.Substring(0, strLength) + suffix;
            }
        }
    }
}
