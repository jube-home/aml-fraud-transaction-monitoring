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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Jube.Case.Outbound
{
    public sealed class OutboundHostPolicy
    {
        public const string AllowedHostsKey = "JUBE_HOOK_ALLOWED_HOSTS";
        public const string AllowedCidrsKey = "JUBE_HOOK_ALLOWED_CIDRS";

        private static readonly Regex TokenPattern = new(@"\[@.*?@\]", RegexOptions.Compiled);

        private static readonly ConditionalWeakTable<DynamicEnvironment.DynamicEnvironment, OutboundHostPolicy> cache =
            new();

        private readonly IReadOnlyList<string> allowedHosts;
        private readonly IReadOnlyList<IpRange> allowedRanges;

        public OutboundHostPolicy(IEnumerable<string>? allowedHosts = null, IEnumerable<string>? allowedCidrs = null)
        {
            this.allowedHosts =
            [
                .. (allowedHosts ?? []).Select(s => Normalise(s)).Where(w => w.Length > 0).Distinct()
            ];

            var ranges = new List<IpRange>();
            var invalid = new List<string>();

            foreach (var entry in allowedCidrs ?? [])
            {
                if (IpRange.TryParse(entry, out var range))
                {
                    ranges.Add(range);
                }
                else if (!string.IsNullOrWhiteSpace(entry))
                {
                    invalid.Add(entry);
                }
            }

            allowedRanges = ranges;
            InvalidAllowedCidrs = invalid;
        }

        public static OutboundHostPolicy Default { get; } = new();

        public IReadOnlyList<string> InvalidAllowedCidrs { get; }

        public string Fingerprint =>
            string.Join(",", allowedHosts) + "|" + string.Join(",", allowedRanges.Select(s => s.GetHashCode()));

        public static OutboundHostPolicy From(DynamicEnvironment.DynamicEnvironment environment)
        {
            return cache.GetValue(environment, e => new OutboundHostPolicy(Split(e.AppSettings(AllowedHostsKey)),
                Split(e.AppSettings(AllowedCidrsKey))));
        }

        public string? CheckUri(Uri uri)
        {
            if (uri.Scheme is not ("http" or "https"))
            {
                return PolicyCode.SchemeNotAllowed;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                return PolicyCode.UserInfoNotAllowed;
            }

            var host = Normalise(uri.Host);

            if (host.Length == 0)
            {
                return PolicyCode.HostBlocked;
            }

            if (HostSpelling.TryParseAddress(host, out var address))
            {
                return IsAddressPermitted(address, host) ? null : PolicyCode.HostBlocked;
            }

            if (IsHostAllowed(host))
            {
                return null;
            }

            return host == "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal)
                ? PolicyCode.HostBlocked
                : null;
        }

        public string? CheckTemplate(string? template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return PolicyCode.NotAbsolute;
            }

            var (url, refusal) = WebhookTarget.Resolve(template, t => TokenPattern.Replace(t, "x"));

            return url == null ? refusal : CheckUri(new Uri(url));
        }

        public bool IsAddressPermitted(IPAddress address, string host)
        {
            if (!IpClassifier.IsBlocked(address))
            {
                return true;
            }

            return IsHostAllowed(Normalise(host)) || allowedRanges.Any(a => a.Contains(address));
        }

        public string? CheckResolved(string host, IReadOnlyCollection<IPAddress> addresses,
            out IReadOnlyList<IPAddress> permitted)
        {
            permitted = [.. addresses.Where(w => IsAddressPermitted(w, host))];

            if (addresses.Count == 0)
            {
                return PolicyCode.HostNotResolved;
            }

            return permitted.Count == 0 ? PolicyCode.ResolvesToBlockedAddress : null;
        }

        private bool IsHostAllowed(string host)
        {
            return allowedHosts.Any(a => a == host || (a.StartsWith("*.", StringComparison.Ordinal)
                                                        && host.EndsWith(a[1..], StringComparison.Ordinal)));
        }

        private static string Normalise(string? host)
        {
            var text = (host ?? string.Empty).Trim().Trim('[', ']').ToLowerInvariant();

            return text.EndsWith('.') ? text[..^1] : text;
        }

        private static IEnumerable<string> Split(string? value)
        {
            return (value ?? string.Empty).Split([',', ';', ' ', '\n', '\r', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
