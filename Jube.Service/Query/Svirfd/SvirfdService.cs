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

using System.ComponentModel;
using System.Net;
using Jube.Data.Context;
using Jube.Data.Query.GetTenantSvirfdRemoteLookupsQuery;
using Jube.Data.Repository;
using Jube.Dto.Query.Svirfd;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.Svirfd;
using Jube.Service.Security;
using Jube.Svirfd.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;
using Svirfd;

namespace Jube.Service.Query.Svirfd
{
    public sealed class SvirfdService
    {
        private static readonly int[] permissions = [72];
        private readonly ISvirfdLookup lookup;
        private readonly PermissionValidation permissionValidation;
        private readonly IStringLocalizer strings;
        private readonly bool remoteLookupsEnabled;

        private SvirfdService(ISvirfdLookup lookup, PermissionValidation permissionValidation,
            IStringLocalizer strings, bool remoteLookupsEnabled)
        {
            this.remoteLookupsEnabled = remoteLookupsEnabled;
            this.lookup = lookup;
            this.permissionValidation = permissionValidation;
            this.strings = strings;
        }

        public static IReadOnlyList<int> ReadPermissions => permissions;

        public static async Task<SvirfdService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            ISvirfdLookup lookup, IStringLocalizerFactory stringLocalizerFactory, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(SvirfdResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ForbiddenException(strings[SvirfdResources.NotAuthenticated], permissions);
            }

            if (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                    .ConfigureAwait(false) is null)
            {
                throw new ForbiddenException(strings[SvirfdResources.NotAuthenticated], permissions);
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            var tenantRemoteLookupsEnabled = await new GetTenantSvirfdRemoteLookupsQuery(dbContext)
                .ExecuteAsync(userName, token).ConfigureAwait(false);

            return new SvirfdService(lookup, validation, strings, tenantRemoteLookupsEnabled);
        }

        [Description("Looks up one IPv4 or IPv6 address for IP intelligence from svirfd.com: geolocation with the evidence and confidence behind any city, ASN and BGP routing with RPKI, VPN, Tor and relay lists, cloud and connection type, observed traceroutes, ping density and a plain-English narrative. Fails when Svirfd is disabled. Credit svirfd.com wherever the result is shown. Read-only.")]
        [ServiceOperation("SvirfdLookup", OperationKind.Read, Idempotent = true)]
        public async Task<SvirfdLookupDto> LookupAsync(
            [Description("The IPv4 or IPv6 address to look up.")] string? ip, CancellationToken token = default)
        {
            EnsurePermitted();

            if (!lookup.Enabled)
            {
                throw new NotFoundException(strings[SvirfdResources.Disabled]);
            }

            if (!IPAddress.TryParse(ip, out _))
            {
                throw new InvalidAddressException(strings[SvirfdResources.NotAnIpAddress]);
            }

            if (!lookup.Ready)
            {
                throw new IndexLoadingException(strings[SvirfdResources.IndexLoading]);
            }

            if (!remoteLookupsEnabled && lookup.Mode == SvirfdMode.Remote)
            {
                throw new NotFoundException(strings[SvirfdResources.RemoteLookupsDisabledForTenant]);
            }

            var record = await lookup.LookupFreshAsync(ip, remoteLookupsEnabled, token).ConfigureAwait(false);

            return record is null
                ? throw new NotFoundException(strings[SvirfdResources.NoRecord])
                : SvirfdLookupMapper.ToDto(record, lookup);
        }

        [Description("Returns the state of the Svirfd IP intelligence client: whether it is enabled and ready, the mode in use and the credit that must be shown wherever its data is shown. Read-only.")]
        [ServiceOperation("SvirfdStatus", OperationKind.Read, Idempotent = true)]
        public SvirfdStatusDto Status()
        {
            EnsurePermitted();

            return SvirfdLookupMapper.ToStatus(lookup, remoteLookupsEnabled);
        }

        private void EnsurePermitted()
        {
            if (!permissionValidation.Validate(permissions))
            {
                throw new ForbiddenException(strings[SvirfdResources.PermissionDenied], permissions);
            }
        }
    }
}
