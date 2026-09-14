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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Sanctions;
using Jube.Engine.Sanctions.Models;
using LinqToDB;
using Microsoft.VisualBasic.FileIO;
using Xunit;
using SanctionEntry = Jube.Data.Poco.SanctionEntry;

namespace Jube.Test.Infrastructure
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class SanctionsSdnLoadFixture : IAsyncLifetime
    {
        private const string MultiPartStringIndex = "1";
        private const int ReferenceIndex = 0;

        private string ConnectionString { get; } =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public int SourceId { get; private set; }
        public int ImportId { get; private set; }
        public SanctionEntryFileImportResult ImportResult { get; private set; } = null!;
        public int InsertedCount { get; private set; }
        public int RevivedCount { get; private set; }
        public int UnchangedCount { get; private set; }

        public async Task InitializeAsync()
        {
            await using var dbContext = GetDbContext();

            SourceId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.DatabaseFixture.Prefix}SanctionSourceSdn{Guid.NewGuid():N}"[..40],
                Severity = 1,
                Delimiter = ',',
                MultiPartStringIndex = MultiPartStringIndex,
                ReferenceIndex = ReferenceIndex,
                EnableDirectoryLocation = 0,
                EnableHttpLocation = 0,
                Skip = 0
            });

            ImportId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntryImport
            {
                SanctionEntrySourceId = SourceId,
                StartDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            });

            var entryRepository = new SanctionsEntryRepository(dbContext);
            var rejectionRepository = new SanctionEntryRejectionRepository(dbContext);

            await using var stream = File.OpenRead("sdn.csv");
            using var parser = new TextFieldParser(stream);
            parser.TextFieldType = FieldType.Delimited;
            parser.Delimiters = [","];

            ImportResult = await SanctionEntryFileImporter.ImportAsync(parser, SourceId, MultiPartStringIndex,
                ReferenceIndex, 0,
                async (record, token) =>
                {
                    var (_, outcome) = await entryRepository.UpsertAsync(new SanctionEntry
                    {
                        SanctionEntryElementValue = record.ElementValue,
                        SanctionEntrySourceId = SourceId,
                        SanctionPayload = record.Payload,
                        SanctionEntryReference = record.Reference,
                        SanctionEntryHash = record.Hash,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = DatabaseFixture.DatabaseFixture.Prefix
                    }, token).ConfigureAwait(false);

                    switch (outcome)
                    {
                        case SanctionEntryUpsertOutcome.Inserted:
                            InsertedCount++;
                            break;
                        case SanctionEntryUpsertOutcome.Revived:
                            RevivedCount++;
                            break;
                        default:
                            UnchangedCount++;
                            break;
                    }
                },
                async (rejection, token) =>
                {
                    await rejectionRepository.InsertAsync(new SanctionEntryRejection
                    {
                        SanctionEntryImportId = ImportId,
                        SanctionEntrySourceId = SourceId,
                        RowNumber = rejection.RowNumber,
                        RawData = rejection.RawData,
                        ReasonId = (int)rejection.ReasonId,
                        CreatedDate = DateTime.UtcNow
                    }, token).ConfigureAwait(false);
                },
                TestLog.NoOp).ConfigureAwait(false);

            var import = await dbContext.SanctionEntryImport.FirstAsync(w => w.Id == ImportId)
                .ConfigureAwait(false);
            import.EndDate = DateTime.UtcNow;
            import.TotalRows = ImportResult.TotalRows;
            import.InsertedCount = InsertedCount;
            import.RevivedCount = RevivedCount;
            import.UnchangedCount = UnchangedCount;
            import.RejectedCount = ImportResult.RejectedRows;
            import.Successful = 1;
            await dbContext.UpdateAsync(import).ConfigureAwait(false);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = GetDbContext();

            await dbContext.SanctionEntryRejection.Where(w => w.SanctionEntrySourceId == SourceId)
                .DeleteAsync().ConfigureAwait(false);
            await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == SourceId)
                .DeleteAsync().ConfigureAwait(false);
            await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == SourceId)
                .DeleteAsync().ConfigureAwait(false);
            await dbContext.SanctionEntrySource.Where(w => w.Id == SourceId)
                .DeleteAsync().ConfigureAwait(false);
        }

        public DbContext GetDbContext()
        {
            return DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
        }
    }
}