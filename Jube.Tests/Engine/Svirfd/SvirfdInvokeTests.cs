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

namespace Jube.Test.Engine.Svirfd
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SvirfdInvokeTests(DatabaseFixture database, SvirfdInvokeFixture fixture)
        : IClassFixture<SvirfdInvokeFixture>
    {
        [Fact]
        public async Task TheEntryIsLookedUpOnEveryInvocationAndSurfacedInTheResponseAndTheArchiveAsync()
        {
            await using var model = await fixture.CreateIsolatedAsync();
            await using var dbContext = database.GetDbContext();

            await dbContext.EntityAnalysisModelSvirfd.Where(s => s.EntityAnalysisModelId == model.ModelId).DeleteAsync();
            await ModelSync.SyncAsync(fixture.Engine, model);

            var without = await fixture.Api.InvokeAsync(model, Payloads.Example()
                .With("TxnId", $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}")
                .With("IP", "37.30.56.54"));

            without.Status.Should().Be(200);
            without.Find("Svirfd").Should().BeNull("a model without a Svirfd entry has no Svirfd block");

            var ids = await ConfigureAsync(dbContext, model, true, true);

            try
            {
                await ModelSync.SyncAsync(fixture.Engine, model);

                var invalid = await fixture.Api.InvokeAsync(model, Payloads.Example()
                    .With("TxnId", $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}")
                    .With("IP", "123.456.789.200"));

                invalid.Status.Should().Be(200, "an address that is not an address is a data fact, not a failure");
                invalid.Find("Svirfd", "IP")!.Value.ValueKind.Should().Be(JsonValueKind.Null);

                var response = await fixture.Api.InvokeAsync(model, Payloads.Example()
                    .With("TxnId", $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}")
                    .With("IP", "37.30.56.54"));

                response.Status.Should().Be(200);
                response.Find("Svirfd", "IP", "ip")!.Value.GetString().Should().Be("37.30.56.54");
                response.Find("Svirfd", "IP", "asn", "asn")!.Value.GetString().Should().Be("AS12912");
                response.Find("Svirfd", "IP", "privacy", "tor")!.Value.GetBoolean().Should().BeFalse();
                response.Find("Svirfd", "IP", "narrative", "headline")!.Value.GetString().Should().Contain("T-Mobile");

                var archive = await WaitForArchiveAsync(dbContext,
                    Guid.Parse(response.String("EntityAnalysisModelInstanceEntryGuid")!));

                archive.GetProperty("IP").GetProperty("asn").GetProperty("name").GetString()
                    .Should().Be("T-Mobile Polska S.A.");
                archive.GetProperty("IP").GetProperty("location").GetProperty("country").GetString().Should().Be("PL");
                archive.GetProperty("IP").GetProperty("narrative").GetProperty("headline").GetString()
                    .Should().Contain("T-Mobile");
            }
            finally
            {
                await CleanAsync(dbContext, ids);
            }
        }

        [Fact]
        public async Task ATenantWithRemoteLookupsSwitchedOffStillGetsLocalLookupsAsync()
        {
            await using var model = await fixture.CreateIsolatedAsync();
            await using var dbContext = database.GetDbContext();

            var ids = await ConfigureAsync(dbContext, model, true, true);

            await dbContext.TenantRegistry.Where(t => t.Id == model.TenantRegistryId)
                .Set(t => t.SvirfdRemoteLookupsEnabled, (byte?)0).UpdateAsync();

            try
            {
                await ModelSync.SyncAsync(fixture.Engine, model);

                var response = await fixture.Api.InvokeAsync(model, Payloads.Example()
                    .With("TxnId", $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}")
                    .With("IP", "37.30.56.54"));

                response.Status.Should().Be(200);
                response.Find("Svirfd", "IP", "asn", "asn")!.Value.GetString().Should().Be("AS12912");
            }
            finally
            {
                await dbContext.TenantRegistry.Where(t => t.Id == model.TenantRegistryId)
                    .Set(t => t.SvirfdRemoteLookupsEnabled, (byte?)1).UpdateAsync();

                await CleanAsync(dbContext, ids);
            }
        }

        private static async Task<List<int>> ConfigureAsync(DbContext dbContext, ModelScaffold model,
            bool responsePayload, bool reportTable)
        {
            await dbContext.EntityAnalysisModelSvirfd.Where(s => s.EntityAnalysisModelId == model.ModelId).DeleteAsync();

            var ipType = await dbContext.EntityAnalysisModelRequestXpath
                .Where(x => x.EntityAnalysisModelId == model.ModelId && x.Name == "IP")
                .Select(x => x.DataTypeId).FirstOrDefaultAsync();

            ipType.Should().Be(8, "the scaffold copies the demo model, whose IP field is data type IP");

            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelSvirfd
            {
                EntityAnalysisModelId = model.ModelId,
                Name = "IP",
                RequestXPathName = "IP",
                ResponsePayload = responsePayload ? (byte)1 : (byte)0,
                ReportTable = reportTable ? (byte)1 : (byte)0,
                Narrative = 1,
                Active = 1,
                Locked = 0,
                Version = 1,
                Guid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
                Deleted = 0
            });

            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelSvirfd, id, 1,
                model.TenantRegistryId);

            return [id];
        }

        private static async Task CleanAsync(DbContext dbContext, List<int> ids)
        {
            await dbContext.EntityApproval.Where(a => a.EntityId != null && ids.Contains(a.EntityId.Value)
                                                      && a.EntityApprovalKindId ==
                                                      (int)EntityApprovalKind.EntityAnalysisModelSvirfd).DeleteAsync();
            await dbContext.EntityAnalysisModelSvirfd.Where(r => ids.Contains(r.Id)).DeleteAsync();
        }

        private static async Task<JsonElement> WaitForArchiveAsync(DbContext dbContext, Guid entry)
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

            return JsonDocument.Parse(json!).RootElement.GetProperty("svirfd");
        }
    }
}
