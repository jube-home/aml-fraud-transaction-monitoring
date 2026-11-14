// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(string? @this)
        {
            [Description("Truncates the string to a maximum length.")]
            public string? Truncate([Description("The maximum length.")] int maxLength)
            {
                const string suffix = "...";

                if (@this == null || @this.Length <= maxLength)
                {
                    return @this;
                }

                var strLength = maxLength - suffix.Length;

                return @this.Substring(0, strLength) + suffix;
            }

            [Description("Truncates with a suffix.")]
            public string? Truncate([Description("The maximum length.")] int maxLength,
                [Description("The suffix to append when truncated.")] string suffix)
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