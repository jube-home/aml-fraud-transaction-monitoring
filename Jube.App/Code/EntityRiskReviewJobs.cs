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
using Jube.Service.Query.EntityRiskRating;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code
{
    public sealed class EntityRiskReviewJobs(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings("EnableEntityRiskReview"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var wait = long.Parse(dynamicEnvironment.AppSettings("WaitEntityRiskReview"), NumberStyles.Integer,
                CultureInfo.InvariantCulture);

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken).ConfigureAwait(false);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            dynamicEnvironment.AppSettings("ConnectionString"), log);

                        var notified = await EntityRiskReviewJob.RunOnceAsync(dbContext, DateTime.UtcNow,
                            stoppingToken).ConfigureAwait(false);

                        log.Info($"EntityRiskReviewJobs: {notified} notified.");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error("EntityRiskReviewJobs: the cycle failed.", ex);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(wait), stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                log.Info("Graceful Cancellation EntityRiskReviewJobs.");
            }
        }
    }
}
