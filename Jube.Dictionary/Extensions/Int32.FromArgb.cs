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
        public static Color FromArgb(this Int32 argb)
        {
            return Color.FromArgb(argb);
        }

        public static Color FromArgb(this Int32 argb, Int32 red, Int32 green, Int32 blue)
        {
            return Color.FromArgb(argb, red, green, blue);
        }

        public static Color FromArgb(this Int32 argb, Color baseColor)
        {
            return Color.FromArgb(argb, baseColor);
        }

        public static Color FromArgb(this Int32 argb, Int32 green, Int32 blue)
        {
            return Color.FromArgb(argb, green, blue);
        }
#endif
    }
}
