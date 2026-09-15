// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Drawing;

namespace Jube.Dictionary.Extensions
{
    using Color = Color;
    using ColorTranslator = ColorTranslator;
    using Int32 = int;

    public static partial class Extensions
    {
#if !NETSTANDARD
        public static Color FromWin32(this Int32 win32Color)
        {
            return ColorTranslator.FromWin32(win32Color);
        }
#endif
    }
}
