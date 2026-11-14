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
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphRecallInvokeTests(DatabaseFixture database, GraphRecallInvokeFixture fixture)
        : IClassFixture<GraphRecallInvokeFixture>
    {
        private const string AccountKey = "AccountId";

        [Fact]
        public async Task TheResponseCarriesTheResponsePayloadEntityAndTheArchiveCarriesTheReportTableEntityAsync()
        {
            await using var model = await fixture.CreateIsolatedAsync();
            await using var dbContext = database.GetDbContext();
            var tenantRegistryId = model.TenantRegistryId;
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var account = $"{DatabaseFixture.Prefix}A{suffix}";
            var beneficiary = $"{DatabaseFixture.Prefix}B{suffix}";
            var registry = Guid.NewGuid();
            var recallIds = new List<int>();

            try
            {
                var now = DateTime.UtcNow;
                await dbContext.InsertAsync(new GraphRegistry
                {
                    Guid = registry,
                    TenantRegistryId = tenantRegistryId,
                    Name = $"{DatabaseFixture.Prefix}Recall{suffix}",
                    Active = 1,
                    Locked = 0,
                    ContextKindId = 1,
                    DefaultLayoutId = 1,
                    DefaultHops = 2,
                    DefaultWindowDays = 30,
                    RenderCap = 150,
                    HubPolicyId = 1,
                    CreatedDate = now,
                    CreatedUser = DatabaseFixture.Prefix,
                    Version = 1,
                    Deleted = 0
                });

                await dbContext.InsertAsync(Node(tenantRegistryId, account, 2, 0, 1, now));
                await dbContext.InsertAsync(Node(tenantRegistryId, beneficiary, 1, 1, 1, now));

                recallIds.Add(await InsertRecallAsync(dbContext, model.ModelId, "Account", AccountKey, null,
                    registry, true));
                recallIds.Add(await InsertRecallAsync(dbContext, model.ModelId, "Beneficiary", "ToAccountId",
                    AccountKey, registry, false));

                foreach (var id in recallIds)
                {
                    await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall,
                        id, 1, tenantRegistryId);
                }

                await ModelSync.SyncAsync(fixture.Engine, model);

                var response = await InvokeUntilKnownAsync(model, account, beneficiary);

                response.Status.Should().Be(200);
                response.Find("Graph", "Account", "Known")!.Value.GetBoolean().Should().BeTrue();
                response.Find("Graph", "Account", "Value")!.Value.GetString().Should().Be(account);
                response.Find("Graph", "Account", "DistanceToConfirmed")!.Value.GetInt32().Should().Be(2);
                response.Find("Graph", "Account", "ConfirmedWithin2")!.Value.GetInt32().Should().Be(1);
                response.Find("Graph", "Beneficiary").Should().BeNull(
                    "an entity without Response Payload stays out of the response");

                var archive = await WaitForArchiveGraphAsync(dbContext,
                    Guid.Parse(response.String("EntityAnalysisModelInstanceEntryGuid")!));

                archive.GetProperty("Beneficiary").GetProperty("known").GetBoolean().Should().BeTrue();
                archive.GetProperty("Beneficiary").GetProperty("value").GetString().Should().Be(beneficiary);
                archive.GetProperty("Beneficiary").GetProperty("distanceToConfirmed").GetInt32().Should().Be(1);
                archive.TryGetProperty("Account", out _).Should().BeFalse(
                    "an entity without Report Table stays out of the archive");
            }
            finally
            {
                await dbContext.EntityApproval.Where(a => a.EntityId != null && recallIds.Contains(a.EntityId.Value)
                                                          && a.EntityApprovalKindId ==
                                                          (int)EntityApprovalKind.EntityAnalysisModelGraphRecall)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModelGraphRecall.Where(r => recallIds.Contains(r.Id)).DeleteAsync();
                await dbContext.GraphNode.Where(n => n.TenantRegistryId == tenantRegistryId).DeleteAsync();
                await dbContext.GraphRegistry.Where(r => r.Guid == registry).DeleteAsync();
            }
        }

        private static GraphNode Node(int tenantRegistryId, string value, short distance, int within1, int within2,
            DateTime seen)
        {
            return new GraphNode
            {
                TenantRegistryId = tenantRegistryId,
                Key = AccountKey,
                Value = value,
                Degree = 3,
                DegreeMoney = 2,
                DegreeIdentity = 1,
                DistanceToConfirmed = distance,
                ConfirmedWithin1 = within1,
                ConfirmedWithin2 = within2,
                ConfirmedWithin3 = within2,
                FirstSeen = seen,
                LastSeen = seen
            };
        }

        private static Task<int> InsertRecallAsync(DbContext dbContext, int modelId, string name, string field,
            string? nodeKey, Guid registry, bool responsePayload)
        {
            return dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelGraphRecall
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                RequestXPathName = field,
                NodeKey = nodeKey,
                GraphRegistryGuid = registry,
                ResponsePayload = responsePayload ? (byte)1 : (byte)0,
                ReportTable = responsePayload ? (byte)0 : (byte)1,
                Active = 1,
                Locked = 0,
                Version = 1,
                Guid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
                Deleted = 0
            });
        }

        private async Task<InvokeResponse> InvokeUntilKnownAsync(ModelScaffold model, string account,
            string beneficiary)
        {
            var deadline = DateTime.UtcNow.AddSeconds(60);
            InvokeResponse response;

            do
            {
                response = await fixture.Api.InvokeAsync(model, Payloads.Example()
                    .With("TxnId", $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}")
                    .With(AccountKey, account)
                    .With("ToAccountId", beneficiary));

                if (response.Find("Graph", "Account", "Known") is { ValueKind: JsonValueKind.True })
                {
                    return response;
                }

                await Task.Delay(1000);
            } while (DateTime.UtcNow < deadline);

            return response;
        }

        private static async Task<JsonElement> WaitForArchiveGraphAsync(DbContext dbContext, Guid entry)
        {
            var deadline = DateTime.UtcNow.AddSeconds(60);
            string? json = null;

            while (DateTime.UtcNow < deadline)
            {
                json = await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == entry)
                    .Select(a => a.Json).FirstOrDefaultAsync();

                if (json != null)
                {
                    break;
                }

                await Task.Delay(500);
            }

            json.Should().NotBeNull("the archiver writes the invocation within the wait");

            return JsonDocument.Parse(json!).RootElement.GetProperty("graph");
        }
    }
}
