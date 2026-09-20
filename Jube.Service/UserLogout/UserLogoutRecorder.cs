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

using System.Collections.Concurrent;
using Jube.Data.Context;
using Jube.Data.Repository;
using log4net;
using UserLogoutPoco = Jube.Data.Poco.UserLogout;

namespace Jube.Service.UserLogout
{
    public static class UserLogoutRecorder
    {
        private const int MaxLength = 512;
        private const int MaxThrottledAddresses = 10_000;
        private static readonly TimeSpan insertTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan noSessionInterval = TimeSpan.FromMinutes(1);
        private static readonly ConcurrentDictionary<string, DateTime> lastNoSessionRow = new();

        public static async Task RecordAsync(Func<DbContext> dbContextFactory, ILog log, UserLogoutEntry entry,
            TimeProvider? timeProvider = null)
        {
            try
            {
                if (!Allowed(entry, timeProvider))
                {
                    return;
                }

                await using var dbContext = dbContextFactory();
                await InsertAsync(dbContext, entry, timeProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Warn(log, entry, ex);
            }
        }

        public static async Task RecordAsync(DbContext dbContext, ILog log, UserLogoutEntry entry,
            TimeProvider? timeProvider = null)
        {
            try
            {
                if (!Allowed(entry, timeProvider))
                {
                    return;
                }

                await InsertAsync(dbContext, entry, timeProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Warn(log, entry, ex);
            }
        }

        private static async Task InsertAsync(DbContext dbContext, UserLogoutEntry entry, TimeProvider? timeProvider)
        {
            using var timeout = new CancellationTokenSource(insertTimeout);
            int? tenantRegistryId = null;
            if (!string.IsNullOrEmpty(entry.UserName))
            {
                tenantRegistryId = await UserInTenantRepository
                    .GetTenantRegistryIdAsync(dbContext, entry.UserName, timeout.Token).ConfigureAwait(false);
            }

            await new UserLogoutRepository(dbContext).InsertAsync(new UserLogoutPoco
            {
                CreatedUser = Clip(entry.UserName),
                TenantRegistryId = tenantRegistryId,
                CreatedDate = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime,
                ReasonId = entry.ReasonId,
                OutcomeId = entry.OutcomeId,
                RemoteIp = Clip(entry.RemoteIp),
                LocalIp = Clip(entry.LocalIp),
                UserAgent = Clip(entry.UserAgent),
                SessionStartDate = entry.SessionStartUtc,
                CutByUser = Clip(entry.CutByUser),
                Message = Clip(entry.Message)
            }, timeout.Token).ConfigureAwait(false);
        }

        private static bool Allowed(UserLogoutEntry entry, TimeProvider? timeProvider)
        {
            if (entry.OutcomeId != UserLogoutOutcome.NoSession)
            {
                return true;
            }

            var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
            var key = entry.RemoteIp ?? "-";
            if (lastNoSessionRow.Count >= MaxThrottledAddresses)
            {
                foreach (var stale in lastNoSessionRow)
                {
                    if (now - stale.Value >= noSessionInterval)
                    {
                        lastNoSessionRow.TryRemove(stale.Key, out _);
                    }
                }

                if (lastNoSessionRow.Count >= MaxThrottledAddresses)
                {
                    return false;
                }
            }

            var allowed = false;
            lastNoSessionRow.AddOrUpdate(key, _ =>
            {
                allowed = true;
                return now;
            }, (_, last) =>
            {
                if (now - last < noSessionInterval)
                {
                    return last;
                }

                allowed = true;
                return now;
            });

            return allowed;
        }

        private static string? Clip(string? value)
        {
            if (value == null)
            {
                return null;
            }

            var clean = string.Create(value.Length, value, static (span, v) =>
            {
                for (var i = 0; i < v.Length; i++)
                {
                    span[i] = char.IsControl(v[i]) ? '?' : v[i];
                }
            });

            return clean.Length > MaxLength ? clean[..MaxLength] : clean;
        }

        private static void Warn(ILog log, UserLogoutEntry entry, Exception ex)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn(
                    $"UserLogout: the audit row for reason={entry.ReasonId} outcome={entry.OutcomeId} was not written.",
                    ex);
            }
        }
    }
}