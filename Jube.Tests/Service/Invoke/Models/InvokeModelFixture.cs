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
using System.Threading.Tasks;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;

namespace Jube.Test.Service.Invoke.Models
{
    public sealed class InvokeModelFixture(DatabaseFixture database) : ModelScaffoldFixture
    {
        private int sourceId;

        protected override IReadOnlyDictionary<string, string> EngineSettings =>
            new Dictionary<string, string> { ["SanctionLoaderChangePoll"] = "200" };

        protected override async Task BeforeEngineStartAsync()
        {
            await using var dbContext = database.GetDbContext();
            sourceId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}InvokeSource{Guid.NewGuid():N}"[..40],
                Severity = 2,
                Delimiter = ',',
                MultiPartStringIndex = "0",
                ReferenceIndex = 0,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 0,
                DirectoryLocation = "/tmp/does-not-matter",
                Skip = 0
            });

            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.SanctionEntry
            {
                SanctionEntryElementValue = "Robert Mugabe",
                SanctionEntryReference = "INVOKE-TEST-MUGABE",
                SanctionEntrySourceId = sourceId,
                SanctionEntryHash = Guid.NewGuid().ToString("N"),
                SanctionPayload = "{}",
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "InvokeTest",
                Deleted = 0
            });
        }

        protected override async Task AfterDisposeAsync()
        {
            if (sourceId == 0)
            {
                return;
            }

            await using var dbContext = database.GetDbContext();
            await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
            await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
            await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
        }
    }
}