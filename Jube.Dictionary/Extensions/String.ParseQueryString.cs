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
        extension(String query)
        {
            public NameValueCollection ParseQueryString()
            {
                return HttpUtility.ParseQueryString(query);
            }

            public NameValueCollection ParseQueryString(Encoding encoding)
            {
                return HttpUtility.ParseQueryString(query, encoding);
            }
        }
    }
}
