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
using Jube.Data.Repository;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code.Waf
{
    public sealed class WafRegistryRefreshService(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        WafRegistry registry) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!IsEnabled())
            {
                return;
            }

            await RefreshAsync(stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(ComputeWindow(
                dynamicEnvironment.AppSettings("WafRefreshInterval"), Setting("WafRefreshIntervalValue", 30)));
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await RefreshAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.Error($"Waf: A refresh of the signature and exception registry has failed as {ex}.");
                }
            }
        }

        private async Task RefreshAsync(CancellationToken token)
        {
            var maxTimeout = Setting("WafMaxRegexTimeoutMilliseconds", 100);

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            var signatures = await new WafSignatureRepository(dbContext).GetActiveAsync(token).ConfigureAwait(false);
            var exceptions = await new WafExceptionRepository(dbContext).GetActiveAsync(token).ConfigureAwait(false);

            var ruleSet = WafRuleSetCompiler.Compile(signatures, exceptions, maxTimeout, log);
            registry.Update(ruleSet);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Waf: Registry refreshed with {ruleSet.Signatures.Count} signatures and {ruleSet.Exceptions.Count} exceptions.");
            }
        }

        private bool IsEnabled()
        {
            return "True".Equals(dynamicEnvironment.AppSettings("WafEnabled"), StringComparison.OrdinalIgnoreCase);
        }

        private int Setting(string key, int fallback)
        {
            return int.TryParse(dynamicEnvironment.AppSettings(key), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : fallback;
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
    }
}