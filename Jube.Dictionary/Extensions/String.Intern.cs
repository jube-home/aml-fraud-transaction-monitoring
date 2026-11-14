// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
namespace Jube.Dictionary.Extensions
{
    using String = string;

    public static partial class Extensions
    {
        [Description("Retrieves the system's reference to the string.")]
        public static String Intern(this String str)
        {
            return string.Intern(str);
        }
    }
}
