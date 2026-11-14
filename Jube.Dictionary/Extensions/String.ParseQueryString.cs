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
            [Description("Parses a query string into a NameValueCollection.")]
            public NameValueCollection ParseQueryString()
            {
                return HttpUtility.ParseQueryString(query);
            }

            [Description("Parses a query string with specified encoding.")]
            public NameValueCollection ParseQueryString([Description("The encoding to use.")] Encoding encoding)
            {
                return HttpUtility.ParseQueryString(query, encoding);
            }
        }
    }
}
