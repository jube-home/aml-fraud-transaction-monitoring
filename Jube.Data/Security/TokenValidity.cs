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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Security.Models;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Security
{
    public static class TokenValidity
    {
        public const string IssuedMillisecondsClaim = "jube_iat_ms";

        public static async Task RevokeUserBeforeAsync(DbContext dbContext, string userName, DateTimeOffset cutoff,
            CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return;
            }

            var cutoffUtc = DateTime.SpecifyKind(cutoff.UtcDateTime, DateTimeKind.Unspecified);

            await dbContext.ExecuteAsync(
                "UPDATE \"UserRegistry\" SET \"TokensValidFrom\" = GREATEST(COALESCE(\"TokensValidFrom\", @cutoff), @cutoff) " +
                "WHERE \"Name\" = @name",
                new DataParameter("cutoff", cutoffUtc, DataType.DateTime2),
                new DataParameter("name", userName, DataType.NVarChar)).ConfigureAwait(false);
        }

        public static async Task<long?> GetTokensValidFromMillisecondsAsync(DbContext dbContext, string userName)
        {
            return (await GetSessionStateAsync(dbContext, userName).ConfigureAwait(false)).ValidFromMilliseconds;
        }

        public static async Task<(long? ValidFromMilliseconds, bool Blocked)> GetSessionStateAsync(
            DbContext dbContext, string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return (null, false);
            }

            var rows = await dbContext.QueryToListAsync<SessionStateRow>(
                "SELECT max(\"TokensValidFrom\") AS \"ValidFrom\", " +
                "min(CASE WHEN COALESCE(\"PasswordLocked\", 0) = 1 OR COALESCE(\"Active\", 0) <> 1 " +
                "OR COALESCE(\"Deleted\", 0) = 1 THEN 1 ELSE 0 END) AS \"Blocked\" " +
                "FROM \"UserRegistry\" WHERE \"Name\" = @name",
                new DataParameter("name", userName, DataType.NVarChar)).ConfigureAwait(false);

            var row = rows.Count > 0 ? rows[0] : null;
            long? validFrom = row?.ValidFrom.HasValue == true
                ? new DateTimeOffset(DateTime.SpecifyKind(row.ValidFrom.Value, DateTimeKind.Utc))
                    .ToUnixTimeMilliseconds()
                : null;

            return (validFrom, row?.Blocked == 1);
        }

        public static async Task<bool> IsRevokedAsync(DbContext dbContext, string userName, string issuedMilliseconds,
            DateTime? issuedAtUtc)
        {
            var (validFrom, blocked) = await GetSessionStateAsync(dbContext, userName).ConfigureAwait(false);
            if (blocked)
            {
                return true;
            }

            if (!validFrom.HasValue)
            {
                return false;
            }

            if (!long.TryParse(issuedMilliseconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out var issued))
            {
                issued = issuedAtUtc.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(issuedAtUtc.Value, DateTimeKind.Utc))
                        .ToUnixTimeMilliseconds()
                    : 0;
            }

            return issued < validFrom.Value;
        }
    }
}