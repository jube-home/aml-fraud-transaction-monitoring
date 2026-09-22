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
            public Color FromArgb()
            {
                return Color.FromArgb(argb);
            }

            public Color FromArgb(Int32 red, Int32 green, Int32 blue)
            {
                return Color.FromArgb(argb, red, green, blue);
            }

            public Color FromArgb(Color baseColor)
            {
                return Color.FromArgb(argb, baseColor);
            }

            public Color FromArgb(Int32 green, Int32 blue)
            {
                return Color.FromArgb(argb, green, blue);
            }
        }

#endif
    }
}
