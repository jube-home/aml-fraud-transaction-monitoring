// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        extension(String format)
        {
            [Description("Replaces format items in a string.")]
            public String Format([Description("The object to format.")] object arg0)
            {
                return string.Format(format, arg0);
            }

            [Description("Replaces format items with two objects.")]
            public String Format([Description("The first object to format.")] object arg0,
                [Description("The second object to format.")] object arg1)
            {
                return string.Format(format, arg0, arg1);
            }

            [Description("Replaces format items with three objects.")]
            public String Format([Description("The first object to format.")] object arg0,
                [Description("The second object to format.")] object arg1,
                [Description("The third object to format.")] object arg2)
            {
                return string.Format(format, arg0, arg1, arg2);
            }

            [Description("Replaces format items with an array of objects.")]
            public String Format([Description("An object array that contains zero or more objects to format.")] object[] args)
            {
                return string.Format(format, args);
            }
        }
    }
}
