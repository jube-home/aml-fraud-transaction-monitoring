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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Jube.Engine.Sanctions
{
    public sealed record ApprovedSanctionExemption(int Id, int TenantRegistryId, string NameKey,
        int SanctionEntrySourceId, string SanctionEntryReference, DateTime? ExpiryDate);

    public sealed class SanctionExemptionSet
    {
        public static readonly SanctionExemptionSet Shared = new();

        private volatile ConcurrentDictionary<string, DateTime?> exemptions = new();
        private volatile Dictionary<int, string> fingerprints = new();

        public static string NameKey(string screenedName)
        {
            return string.Join(' ', SanctionTextFolding.Tokens(screenedName ?? string.Empty)
                .Order(StringComparer.Ordinal));
        }

        private static string Key(int tenantRegistryId, string nameKey, int sourceId, string reference)
        {
            return $"{tenantRegistryId}|{sourceId}|{reference}|{nameKey}";
        }

        public void Replace(IEnumerable<ApprovedSanctionExemption> approved)
        {
            var next = new ConcurrentDictionary<string, DateTime?>();
            var byTenant = new Dictionary<int, List<string>>();

            foreach (var exemption in approved)
            {
                next[Key(exemption.TenantRegistryId, exemption.NameKey, exemption.SanctionEntrySourceId,
                    exemption.SanctionEntryReference)] = exemption.ExpiryDate;

                if (!byTenant.TryGetValue(exemption.TenantRegistryId, out var list))
                {
                    list = [];
                    byTenant[exemption.TenantRegistryId] = list;
                }

                list.Add($"{exemption.Id}:{exemption.ExpiryDate:O}");
            }

            fingerprints = byTenant.ToDictionary(p => p.Key, p => Hash(p.Value));
            exemptions = next;
        }

        public void Clear()
        {
            Replace([]);
        }

        public bool IsExempt(int tenantRegistryId, string screenedName, int sourceId, string reference, DateTime now)
        {
            if (exemptions.IsEmpty)
            {
                return false;
            }

            return exemptions.TryGetValue(Key(tenantRegistryId, NameKey(screenedName), sourceId, reference),
                       out var expiry) && (expiry is null || expiry.Value > now);
        }

        public string Fingerprint(int tenantRegistryId)
        {
            return fingerprints.TryGetValue(tenantRegistryId, out var value) ? value : "0";
        }

        private static string Hash(List<string> parts)
        {
            parts.Sort(StringComparer.Ordinal);

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts))))[..12]
                .ToLowerInvariant();
        }
    }
}
