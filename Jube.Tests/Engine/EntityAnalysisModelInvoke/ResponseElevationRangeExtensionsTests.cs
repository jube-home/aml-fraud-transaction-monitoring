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
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ResponseElevationRangeExtensionsTests
    {
        private static Context NewContext(int? reprocessingRuleInstanceId)
        {
            var model = new EntityAnalysisModel { Instance = { TenantRegistryId = 7, Guid = Guid.NewGuid() } };
            model.ConcurrentQueues.PendingNotifications = new ConcurrentQueue<Notification>();
            model.ConcurrentQueues.PendingWebhooks = new ConcurrentQueue<Webhook>();
            var snapshot = new ModelSnapshotBuilder(model.Snapshot).Build();

            snapshot.EntityAnalysisModelResponseElevationRanges.Add(new EntityAnalysisModelResponseElevationRange
            {
                Id = 1,
                Guid = Guid.NewGuid(),
                Name = "Review hold",
                Minimum = 1,
                Maximum = 100,
                Hold = true,
                Decline = true,
                EnableHttpEndpoint = true,
                HttpEndpoint = "http://127.0.0.1/hook",
                HttpEndpointTypeId = 1,
                EnableNotification = true,
                NotificationTypeId = 1,
                NotificationDestination = "someone@example.com",
                NotificationSubject = "Held",
                NotificationBody = "Held"
            });

            return new Context
            {
                EntityAnalysisModel = model,
                Snapshot = snapshot,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    ReferenceDate = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                    EntityAnalysisModelReprocessingRuleInstanceId = reprocessingRuleInstanceId,
                    ResponseElevation = new ResponseElevation { Value = 50 },
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Environment = TestDynamicEnvironment.Create(),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp
            };
        }

        [Fact]
        public async Task AReplayNamesTheElevationButStampsNoHoldAndDispatchesNothingAsync()
        {
            var context = NewContext(4);

            await context.ApplyResponseElevationRangeAsync();

            var payload = context.EntityAnalysisModelInstanceEntryPayload;
            payload.ResponseElevation.Name.Should().Be("Review hold");
            payload.Hold.Should().BeNull();
            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().BeEmpty();
            context.EntityAnalysisModel.ConcurrentQueues.PendingWebhooks.Should().BeEmpty();
        }
    }
}
