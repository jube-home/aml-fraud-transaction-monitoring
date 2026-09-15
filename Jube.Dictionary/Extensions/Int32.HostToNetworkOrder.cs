// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Net;

namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using IPAddress = IPAddress;

    public static partial class Extensions
    {
        public static Int32 HostToNetworkOrder(this Int32 host)
        {
            return IPAddress.HostToNetworkOrder(host);
        }
    }
}
