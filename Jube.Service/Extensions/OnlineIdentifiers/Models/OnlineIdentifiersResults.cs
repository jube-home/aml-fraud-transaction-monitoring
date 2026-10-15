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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Extensions.OnlineIdentifiers.Models
{
    public sealed record EmailAddressDetails(
        bool IsValid,
        string LocalPart,
        string Domain,
        string TopLevelDomain,
        string AliasNormalised);

    public sealed record PhoneNumberDetails(string Digits, bool IsValidE164);

    public sealed record UrlDetails(bool IsValid, string Scheme, string Host, string TopLevelDomain);

    public sealed record IpAddressDetails(
        bool IsValid,
        string Canonical,
        string Family,
        bool IsPrivate,
        bool IsLoopback,
        bool IsReserved,
        bool IsPublic,
        long IPv4Number);

    public sealed record IpRangeDetails(string FirstAddress, string LastAddress, double AddressCount);
}