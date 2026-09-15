// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        public static String Format(this String format, object arg0)
        {
            return string.Format(format, arg0);
        }

        public static String Format(this String format, object arg0, object arg1)
        {
            return string.Format(format, arg0, arg1);
        }

        public static String Format(this String format, object arg0, object arg1, object arg2)
        {
            return string.Format(format, arg0, arg1, arg2);
        }

        public static String Format(this String format, object[] args)
        {
            return string.Format(format, args);
        }
    }
}
