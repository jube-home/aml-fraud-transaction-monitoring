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
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models.Hold;
using Jube.Dto.Hold;
using Jube.Dto.Options;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.ArchiveWrites
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ArchiveHoldWriteTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly DateTime Created = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        private readonly List<Guid> guids = [];

        private static ResponseHold Held()
        {
            return new ResponseHold
            {
                StateId = (int)ResponseHoldState.Held, RangeId = 7, RangeName = "Review hold", RangeMinimum = 10,
                RangeMaximum = 19, Elevation = 12, StampedDate = Created,
                Legs = [new ResponseHoldLeg { KindId = (int)ResponseHoldLegKind.Webhook, OutcomeId = 1, Date = Created }]
            };
        }

        private async Task<Guid> ArchiveAsync(ResponseHold? hold)
        {
            var guid = Guid.NewGuid();
            guids.Add(guid);

            var json = "{\"payload\":{\"a\":1}}";

            if (hold != null)
            {
                json = ResponseHoldJson.Write(json, hold);
            }

            await using var dbContext = fx.GetDbContext();

            await dbContext.InsertAsync(new Archive
            {
                Json = json, EntityAnalysisModelInstanceEntryGuid = guid, EntryKeyValue = "E1", ResponseElevation = 12,
                ActivationRuleCount = 1, CreatedDate = Created, ReferenceDate = Created, Version = 1
            });

            return guid;
        }

        private async Task<Archive> ReadAsync(Guid guid)
        {
            await using var dbContext = fx.GetDbContext();

            return await dbContext.Archive.SingleAsync(a => a.EntityAnalysisModelInstanceEntryGuid == guid);
        }

        private async Task<List<ArchiveVersion>> VersionsAsync(Guid guid)
        {
            await using var dbContext = fx.GetDbContext();

            return await dbContext.ArchiveVersion.Where(v => v.EntityAnalysisModelInstanceEntryGuid == guid)
                .OrderBy(v => v.Version).ToListAsync();
        }

        private async Task<HoldUpdate> UpdateAsync(Guid guid, Func<ResponseHold, Task<ResponseHold>> change)
        {
            await using var dbContext = fx.GetDbContext();

            return await new ArchiveRepository(dbContext).UpdateHoldAsync(guid, change);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var guid in guids)
            {
                var archiveIds = await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == guid)
                    .Select(a => a.Id).ToListAsync();

                await dbContext.ArchiveVersion.Where(v => archiveIds.Contains(v.ArchiveId)).DeleteAsync();
                await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == guid).DeleteAsync();
            }
        }

        [Fact]
        public async Task ChangingTheHoldRewritesItInThePayloadBumpsTheVersionAndKeepsTheEarlierStateAsync()
        {
            var guid = await ArchiveAsync(Held());

            var update = await UpdateAsync(guid, hold =>
            {
                hold.StateId = (int)ResponseHoldState.Released;
                hold.Decision = new ResponseHoldDecision { Actor = "Reviewer", Date = Created, Reason = "Cleared" };

                return Task.FromResult(hold);
            });

            update.Archive.Should().NotBeNull();
            update.Hold!.StateId.Should().Be((int)ResponseHoldState.Released);

            var archive = await ReadAsync(guid);
            archive.Version.Should().Be(2);

            var stored = ResponseHoldJson.Read(archive.Json)!;
            stored.StateId.Should().Be((int)ResponseHoldState.Released);
            stored.Decision!.Reason.Should().Be("Cleared");
            stored.Decision.Actor.Should().Be("Reviewer");
            stored.RangeName.Should().Be("Review hold");

            var versions = await VersionsAsync(guid);
            versions.Should().ContainSingle();
            versions[0].Version.Should().Be(1);
            ResponseHoldJson.Read(versions[0].Json)!.StateId.Should().Be((int)ResponseHoldState.Held,
                "the version keeps the state the payment was in before the change");
        }

        [Fact]
        public async Task TheTransactionDateIsNotMovedByAHoldChangeAsync()
        {
            var guid = await ArchiveAsync(Held());

            await UpdateAsync(guid, hold => Task.FromResult(hold));

            var archive = await ReadAsync(guid);
            archive.CreatedDate.Should().Be(Created);
            archive.ReferenceDate.Should().Be(Created);
            archive.ResponseElevation.Should().Be(12);
        }

        [Fact]
        public async Task EveryChangeIsItsOwnVersionAsync()
        {
            var guid = await ArchiveAsync(Held());

            await UpdateAsync(guid, hold =>
            {
                hold.Legs.Add(new ResponseHoldLeg { KindId = (int)ResponseHoldLegKind.Webhook, OutcomeId = 2, Date = Created });

                return Task.FromResult(hold);
            });

            await UpdateAsync(guid, hold =>
            {
                hold.StateId = (int)ResponseHoldState.Refused;

                return Task.FromResult(hold);
            });

            (await ReadAsync(guid)).Version.Should().Be(3);
            (await VersionsAsync(guid)).Select(s => s.Version).Should().Equal(1, 2);
            ResponseHoldJson.Read((await ReadAsync(guid)).Json)!.Legs.Should().HaveCount(2);
        }

        [Fact]
        public async Task AMissingArchiveAndATransactionWithoutAHoldAreReportedNotChangedAsync()
        {
            var missing = await UpdateAsync(Guid.NewGuid(), hold => Task.FromResult(hold));
            missing.Archive.Should().BeNull();
            missing.Hold.Should().BeNull();

            var guid = await ArchiveAsync(null);
            var none = await UpdateAsync(guid, hold => Task.FromResult(hold));

            none.Archive.Should().NotBeNull();
            none.Hold.Should().BeNull();
            (await ReadAsync(guid)).Version.Should().Be(1);
            (await VersionsAsync(guid)).Should().BeEmpty();
        }

        [Fact]
        public async Task ARefusedChangeRollsBackAndLeavesNeitherAVersionNorAChangeAsync()
        {
            var guid = await ArchiveAsync(Held());

            var act = () => UpdateAsync(guid, hold =>
            {
                hold.StateId = (int)ResponseHoldState.Released;

                throw new InvalidOperationException("refused");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();

            var archive = await ReadAsync(guid);
            archive.Version.Should().Be(1);
            ResponseHoldJson.Read(archive.Json)!.StateId.Should().Be((int)ResponseHoldState.Held);
            (await VersionsAsync(guid)).Should().BeEmpty();
        }

        [Fact]
        public async Task TwoDecisionsAtOnceAreSerialisedSoOnlyTheFirstFindsThePaymentHeldAsync()
        {
            var guid = await ArchiveAsync(Held());
            var calls = 0;

            async Task<ResponseHold> DecideAsync(ResponseHold hold)
            {
                if (hold.StateId != (int)ResponseHoldState.Held)
                {
                    throw new InvalidOperationException("not held");
                }

                calls++;
                await Task.Delay(300);
                hold.StateId = (int)ResponseHoldState.Released;

                return hold;
            }

            var results = await Task.WhenAll(
                Task.Run(async () =>
                {
                    try
                    {
                        await UpdateAsync(guid, DecideAsync);

                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                }),
                Task.Run(async () =>
                {
                    try
                    {
                        await UpdateAsync(guid, DecideAsync);

                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                }));

            results.Count(c => c).Should().Be(1);
            calls.Should().Be(1);
            (await ReadAsync(guid)).Version.Should().Be(2);
        }

        [Fact]
        public async Task AReprocessedArchiveKeepsTheHoldItAlreadyHasAsync()
        {
            var guid = await ArchiveAsync(Held());

            await UpdateAsync(guid, hold =>
            {
                hold.StateId = (int)ResponseHoldState.Released;

                return Task.FromResult(hold);
            });

            await using (var dbContext = fx.GetDbContext())
            {
                await new ArchiveRepository(dbContext).UpdateAsync(new Archive
                {
                    Json = "{\"payload\":{\"a\":2},\"hold\":{\"stateId\":1}}",
                    EntityAnalysisModelInstanceEntryGuid = guid, EntryKeyValue = "E1", ResponseElevation = 99,
                    ActivationRuleCount = 2
                });
            }

            var archive = await ReadAsync(guid);
            archive.ResponseElevation.Should().Be(99);
            archive.Json.Should().Contain("\"a\": 2");
            ResponseHoldJson.Read(archive.Json)!.StateId.Should().Be((int)ResponseHoldState.Released,
                "a reprocess never revives a released payment");
        }

        [Fact]
        public async Task ABatchReprocessKeepsTheHoldAndAddsNoneToATransactionWithoutOneAsync()
        {
            var held = await ArchiveAsync(Held());
            var plain = await ArchiveAsync(null);

            await using (var dbContext = fx.GetDbContext())
            {
                await new ArchiveRepository(dbContext).UpdateBatchAsync(
                [
                    new Archive
                    {
                        Json = "{\"payload\":{\"a\":3},\"hold\":{\"stateId\":4}}",
                        EntityAnalysisModelInstanceEntryGuid = held, EntryKeyValue = "E1", ResponseElevation = 5
                    },
                    new Archive
                    {
                        Json = "{\"payload\":{\"a\":3}}", EntityAnalysisModelInstanceEntryGuid = plain,
                        EntryKeyValue = "E1", ResponseElevation = 5
                    }
                ]);
            }

            ResponseHoldJson.Read((await ReadAsync(held)).Json)!.StateId.Should().Be((int)ResponseHoldState.Held);
            ResponseHoldJson.Read((await ReadAsync(plain)).Json).Should().BeNull();
        }
    }
}
