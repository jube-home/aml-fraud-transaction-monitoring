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
using System.Linq;
using System.Threading.Tasks;
using Accord.Neuro;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using Newtonsoft.Json;

namespace Jube.Test.Exhaustive
{
    internal sealed class PromotedModelSeed
    {
        private readonly DbContext db;
        private int instanceId;
        private int trialId;

        private PromotedModelSeed(DbContext db)
        {
            this.db = db;
        }

        internal Guid InstanceGuid { get; private set; }

        internal static async Task<PromotedModelSeed> InsertAsync(DbContext db, ModelScaffold scaffold)
        {
            var seed = new PromotedModelSeed(db) { InstanceGuid = Guid.NewGuid() };

            seed.instanceId = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstance
            {
                Name = "SeededInstance" + seed.InstanceGuid.ToString("N")[..8],
                Guid = seed.InstanceGuid,
                EntityAnalysisModelId = scaffold.ModelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                StatusId = 15,
                ResponsePayload = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = scaffold.UserName,
                Version = 1
            });

            await EntityApprovalSeed.ApproveAsync(db, EntityApprovalKind.ExhaustiveSearchInstance, seed.instanceId);

            seed.trialId = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = seed.instanceId,
                CreatedDate = DateTime.UtcNow
            });

            for (var i = 0; i < 2; i++)
            {
                var variableId = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
                {
                    ExhaustiveSearchInstanceId = seed.instanceId,
                    Name = "Payload.Field" + i,
                    ProcessingTypeId = 1,
                    Mean = 0.25,
                    StandardDeviation = 1,
                    Maximum = 10,
                    Minimum = -10,
                    NormalisationTypeId = 0,
                    VariableSequence = i
                });

                await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceVariableId = variableId,
                    ExhaustiveSearchInstanceTrialInstanceId = seed.trialId,
                    VariableSequence = i
                });
            }

            var network = new ActivationNetwork(new SigmoidFunction(), 2, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];
            neuron.Weights[0] = 1;
            neuron.Weights[1] = 2;

            await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = seed.trialId,
                Json = JsonConvert.SerializeObject(network,
                    new JsonSerializationHelper().TopologyNetworkJsonSerializerSettings),
                Active = 1,
                Score = 0.9,
                CreatedDate = DateTime.UtcNow
            });

            return seed;
        }

        internal async Task DeleteAsync()
        {
            await EntityApprovalSeed.RemoveForEntityAsync(db, EntityApprovalKind.ExhaustiveSearchInstance,
                instanceId);
            await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == trialId).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == trialId).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == instanceId).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceTrialInstance>().Where(w => w.Id == trialId).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstance>().Where(w => w.Id == instanceId).DeleteAsync();
        }
    }
}