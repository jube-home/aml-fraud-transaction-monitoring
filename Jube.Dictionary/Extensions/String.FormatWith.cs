// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        extension(String @this)
        {
            [Description("Replaces format items with a single object.")]
            public String FormatWith([Description("The argument to format.")] object arg0)
            {
                return string.Format(@this, arg0);
            }

            [Description("Replaces format items with two objects.")]
            public String FormatWith([Description("The first argument to format.")] object arg0,
                [Description("The second argument to format.")] object arg1)
            {
                return string.Format(@this, arg0, arg1);
            }

            [Description("Replaces format items with three objects.")]
            public String FormatWith([Description("The first argument to format.")] object arg0,
                [Description("The second argument to format.")] object arg1,
                [Description("The third argument to format.")] object arg2)
            {
                return string.Format(@this, arg0, arg1, arg2);
            }
        }

        [Description("Replaces format items with an array of objects.")]
        public static string FormatWith(this string @this,
            [Description("An Object array containing zero or more objects to format.")] params object[] values)
        {
            return string.Format(@this, values);
        }
    }
}
