// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Globalization;
using System.Text;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string RemoveDiacritics(this string @this)
        {
            var normalizedString = @this.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var t in normalizedString)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(t);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(t);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
