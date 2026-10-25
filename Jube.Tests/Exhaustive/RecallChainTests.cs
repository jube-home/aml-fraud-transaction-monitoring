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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.Exhaustive;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using log4net;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RecallChainTests(DatabaseFixture fx, ModelScaffoldFixture engineFixture)
        : IClassFixture<ModelScaffoldFixture>
    {
        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private static Training NewTraining()
        {
            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["ExhaustiveTrialsLimit"] = "3",
                ["ExhaustiveTopologySinceImprovementLimit"] = "2",
                ["ExhaustiveTopologyExplorationEpochs"] = "2",
                ["ExhaustiveTopologyFinalisationEpochs"] = "2",
                ["ExhaustiveActivationFunctionExplorationEpochs"] = "3",
                ["ExhaustiveSimulationsCount"] = "50"
            });

            return new Training(LogManager.GetLogger(typeof(RecallChainTests)), environment,
                new JsonSerializationHelper())
            {
                seeded = new Random(1),
                pollDelay = TimeSpan.FromMilliseconds(20)
            };
        }

        [Fact]
        public async Task ATrainedAndPromotedModelIsSyncedAndRecalledThroughBothPathsAsync()
        {
            await using var scaffold = await engineFixture.CreateIsolatedAsync();
            await using var db = fx.GetDbContext();

            var instance = await new ExhaustiveSearchInstanceRepository(db, scaffold.UserName).InsertAsync(
                new ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}En{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = scaffold.ModelId, Active = 1, Locked = 0, Deleted = 0, StatusId = 0,
                    Anomaly = 1, AnomalyProbability = 0.5, Filter = 0, ResponsePayload = 1, DataSourceId = 1,
                    ChallengerModelBaseProbability = 0
                });

            await EntityApprovalSeed.ApproveAsync(db, EntityApprovalKind.ExhaustiveSearchInstance, instance.Id);

            var dto = new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = instance.Id, EntityAnalysisModelId = scaffold.ModelId,
                TenantRegistryId = scaffold.TenantRegistryId,
                Anomaly = true, AnomalyProbability = 0.5, DataSourceId = 1, ChallengerModelBaseProbability = 0
            };

            Assert.True(await NewTraining().RunOneInstanceAsync(dto, db, CancellationToken.None));
            await ModelSync.SyncAsync(engineFixture.Engine, scaffold);

            var loaded = engineFixture.Engine.FindActiveModel(scaffold.ModelGuid)!.Snapshot.ExhaustiveModels
                .Single(w => w.Guid == instance.Guid);
            Assert.NotNull(loaded.TopologyNetwork);
            Assert.NotEmpty(loaded.NetworkVariablesInOrder);
            Assert.Equal(loaded.NetworkVariablesInOrder.Count, loaded.TopologyNetwork.InputsCount);

            var scores = new List<double>();

            foreach (var multiple in new[] { -3.0, 0.0, 1.0, 3.0, 10.0 })
            {
                var body = new JObject();

                foreach (var v in loaded.NetworkVariablesInOrder)
                {
                    body[v.Name] = v.Mean + multiple * v.Sd;
                }

                var response = await engineFixture.Api.SendAsync("POST",
                    "/api/Invoke/ExhaustiveSearchInstance/" + loaded.Guid, scaffold.UserName,
                    Encoding.UTF8.GetBytes(body.ToString()), "application/json");

                Assert.Equal(200, response.Status);
                var score = response.Json.GetProperty("score").GetDouble();
                Assert.InRange(score, 0, 1);
                scores.Add(score);
            }

            Assert.Contains(scores, s => s > 0 && s < 1);
            Assert.True(scores.Distinct().Count() > 1, "Recall is constant: " + string.Join(",", scores));

            var invoked = await engineFixture.Api.InvokeAsync(scaffold, Payloads.Example().With("TxnId", "rc-1"));
            Assert.Equal(200, invoked.Status);
            var pipelineScore = invoked.Find("ExhaustiveAdaptation", loaded.Name);
            Assert.NotNull(pipelineScore);
            Assert.InRange(pipelineScore!.Value.GetDouble(), 1e-9, 1);
        }
    }
}