// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        extension(String format)
        {
            public String Format(object arg0)
            {
                return string.Format(format, arg0);
            }

            public String Format(object arg0, object arg1)
            {
                return string.Format(format, arg0, arg1);
            }

            public String Format(object arg0, object arg1, object arg2)
            {
                return string.Format(format, arg0, arg1, arg2);
            }

            public String Format(object[] args)
            {
                return string.Format(format, args);
            }
        }
    }
}
