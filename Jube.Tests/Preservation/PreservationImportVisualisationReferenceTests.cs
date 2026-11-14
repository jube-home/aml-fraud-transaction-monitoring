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
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PreservationImportVisualisationReferenceTests(DatabaseFixture fx)
    {
        private readonly PreservationHarness harness = new(fx);

        [Fact]
        public async Task ASameTenantImportKeepsEachWorkflowPointingAtItsVisualisationRegistryAsync()
        {
            var tenant = await harness.CreateTenantAsync("ViR");
            var modelId = await harness.CreateModelAsync(tenant, "Vi");
            var registryGuid = Guid.NewGuid();
            var workflowGuid = Guid.NewGuid();

            await using var db = fx.GetDbContext();

            await db.InsertWithInt32IdentityAsync(new VisualisationRegistry
            {
                Guid = registryGuid,
                Name = $"{DatabaseFixture.Prefix}Vis{Guid.NewGuid():N}"[..30],
                TenantRegistryId = tenant.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = tenant.UserName,
                CreatedDate = DateTime.UtcNow
            });

            await db.InsertWithInt32IdentityAsync(new CaseWorkflow
            {
                Guid = workflowGuid,
                Name = $"{DatabaseFixture.Prefix}ViWf{Guid.NewGuid():N}"[..30],
                EntityAnalysisModelId = modelId,
                VisualisationRegistryGuid = registryGuid,
                EnableVisualisation = 1,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = tenant.UserName,
                CreatedDate = DateTime.UtcNow
            });

            var options = PreservationHarness.ExportOptionsFor();
            options.Visualisations = true;

            var bytes = await harness.ExportAsync(tenant, options);
            await harness.ImportAsync(tenant, bytes);

            var live = await db.CaseWorkflow.Where(w => w.EntityAnalysisModel.TenantRegistryId == tenant.Id
                                                        && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync();

            live.Should().ContainSingle("the import soft-deletes the old workflow and inserts one live replacement");

            live[0].VisualisationRegistryGuid.Should().Be(registryGuid,
                "a registry that was not rekeyed is still the right reference, and the model Guid is never a valid fallback");

            (await db.VisualisationRegistry.CountAsync(w => w.Guid == live[0].VisualisationRegistryGuid
                                                             && (w.Deleted == 0 || w.Deleted == null)))
                .Should().Be(1, "the reference must resolve to exactly one live registry");
        }
    }
}
