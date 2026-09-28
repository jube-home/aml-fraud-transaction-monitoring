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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code.Waf
{
    public sealed class WafAttackFlushService(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings("WafEnabled"), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var intervalValue = int.TryParse(dynamicEnvironment.AppSettings("WafFlushIntervalValue"),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : 10;

            using var timer = new PeriodicTimer(
                ComputeWindow(dynamicEnvironment.AppSettings("WafFlushInterval"), intervalValue));
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await FlushAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.Error($"Waf: A flush of captured attacks has failed as {ex}.");
                }
            }

            await FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private static TimeSpan ComputeWindow(string interval, int intervalValue)
        {
            return interval switch
            {
                "s" => TimeSpan.FromSeconds(intervalValue),
                "n" => TimeSpan.FromMinutes(intervalValue),
                "h" => TimeSpan.FromHours(intervalValue),
                _ => TimeSpan.FromDays(intervalValue)
            };
        }

        private async Task FlushAsync(CancellationToken token)
        {
            var drained = WafAttackCapture.DrainAll();
            if (drained.Count == 0)
            {
                return;
            }

            var instance = Dns.GetHostName();
            var models = drained.Select(record => new WafAttack
            {
                CreatedDate = record.CreatedDate,
                Transport = record.Transport,
                Route = record.Route,
                Method = record.Method,
                RemoteIp = record.RemoteIp,
                UserName = record.UserName,
                WafSignatureId = record.SignatureId,
                SignatureName = record.SignatureName,
                Category = record.Category,
                MatchedField = record.MatchedField,
                MatchedValue = record.MatchedValue,
                Action = record.Action,
                CorrelationId = record.CorrelationId,
                Instance = instance
            }).ToList();

            try
            {
                await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log);

                await new WafAttackRepository(dbContext).BulkCopyAsync(models, token).ConfigureAwait(false);
            }
            catch
            {
                foreach (var record in drained)
                {
                    WafAttackCapture.Enqueue(record);
                }

                throw;
            }

            if (log.IsDebugEnabled)
            {
                log.Debug($"Waf: Bulk inserted {models.Count} captured attacks.");
            }
        }
    }
}