// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace Jube.Dictionary.Extensions
{
    using HttpUtility = HttpUtility;
    using String = string;

    public static partial class Extensions
    {
        public static NameValueCollection ParseQueryString(this String query)
        {
            return HttpUtility.ParseQueryString(query);
        }

        public static NameValueCollection ParseQueryString(this String query, Encoding encoding)
        {
            return HttpUtility.ParseQueryString(query, encoding);
        }
    }
}
