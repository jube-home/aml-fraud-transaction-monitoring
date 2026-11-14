// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.

using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Checks if the string is a valid email address, including plus-addressed local parts such as ann+tag@example.com.")]
        public static bool IsValidEmail(this string obj)
        {
            return Regex.IsMatch(obj,
                @"^([a-zA-Z0-9_+\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z0-9]{1,30})(\]?)$");
        }
    }
}