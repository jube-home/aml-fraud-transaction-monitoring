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
        [Description("Converts an OLE color to a Color.")]
        public static Color FromOle(this Int32 oleColor)
        {
            return ColorTranslator.FromOle(oleColor);
        }
#endif
    }
}
