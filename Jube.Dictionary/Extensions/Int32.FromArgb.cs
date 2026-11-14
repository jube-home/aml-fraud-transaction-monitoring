// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Drawing;

namespace Jube.Dictionary.Extensions
{
    using Color = Color;
    using Int32 = int;

    public static partial class Extensions
    {
#if !NETSTANDARD
        extension(Int32 argb)
        {
            [Description("Creates a Color from a 32-bit ARGB value.")]
            public Color FromArgb()
            {
                return Color.FromArgb(argb);
            }

            [Description("Creates a Color from ARGB components.")]
            public Color FromArgb([Description("The red component (0-255).")] Int32 red,
                [Description("The green component (0-255).")] Int32 green,
                [Description("The blue component (0-255).")] Int32 blue)
            {
                return Color.FromArgb(argb, red, green, blue);
            }

            [Description("Creates a Color with a new alpha value.")]
            public Color FromArgb([Description("The Color from which to create the new Color.")] Color baseColor)
            {
                return Color.FromArgb(argb, baseColor);
            }

            [Description("Creates a Color from RGB components (alpha implicit).")]
            public Color FromArgb([Description("The green component (0-255).")] Int32 green,
                [Description("The blue component (0-255).")] Int32 blue)
            {
                return Color.FromArgb(argb, green, blue);
            }
        }

#endif
    }
}
