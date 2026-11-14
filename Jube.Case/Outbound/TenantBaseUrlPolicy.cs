/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Net;

namespace Jube.Case.Outbound
{
    public static class TenantBaseUrlPolicy
    {
        public const string AllowInsecureTargetsKey = "HttpAdaptationAllowInsecureTargets";

        public static string? Check(string? url, bool allowInsecureTargets)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                return PolicyCode.NotAbsolute;
            }

            if (uri.Scheme is not ("http" or "https"))
            {
                return PolicyCode.SchemeNotAllowed;
            }

            if (uri.Scheme == "http" && !allowInsecureTargets)
            {
                return PolicyCode.SchemeNotAllowed;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                return PolicyCode.UserInfoNotAllowed;
            }

            if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                return PolicyCode.NotAbsolute;
            }

            if (url.Any(c => char.IsControl(c) || c == '\\'))
            {
                return PolicyCode.NotAbsolute;
            }

            var host = uri.Host.Trim('[', ']').ToLowerInvariant().TrimEnd('.');

            if (host.Length == 0)
            {
                return PolicyCode.HostBlocked;
            }

            if (allowInsecureTargets)
            {
                return null;
            }

            if (HostSpelling.TryParseAddress(host, out var address))
            {
                return IpClassifier.IsBlocked(address) ? PolicyCode.HostBlocked : null;
            }

            return host == "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal)
                ? PolicyCode.HostBlocked
                : null;
        }

        public static string? CheckResolved(IReadOnlyCollection<IPAddress> addresses, bool allowInsecureTargets,
            out IReadOnlyList<IPAddress> permitted)
        {
            permitted = allowInsecureTargets
                ? [.. addresses]
                : [.. addresses.Where(w => !IpClassifier.IsBlocked(w))];

            if (addresses.Count == 0)
            {
                return PolicyCode.HostNotResolved;
            }

            return permitted.Count == 0 ? PolicyCode.ResolvesToBlockedAddress : null;
        }
    }
}
