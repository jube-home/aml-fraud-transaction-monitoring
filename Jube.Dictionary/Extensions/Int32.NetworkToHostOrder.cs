// Adapted from Z.ExtensionMethods (https://github.com/zzzprojects/Z.ExtensionMethods), MIT License.
// See NOTICE.md for full attribution.
using System.Net;

namespace Jube.Dictionary.Extensions
{
    using Int32 = int;
    using IPAddress = IPAddress;

    public static partial class Extensions
    {
        [Description("Converts from network byte order to host byte order.")]
        public static Int32 NetworkToHostOrder(this Int32 network)
        {
            return IPAddress.NetworkToHostOrder(network);
        }
    }
}
