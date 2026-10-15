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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsTrainingRunRepositoryTests(DatabaseFixture fx)
        : AdvancedAnalyticsRepositoryTestBase(fx)
    {
        private AdvancedAnalyticsTrainingRunRepository Runs(int tenant, string user = "ZzTestUser")
        {
            return new AdvancedAnalyticsTrainingRunRepository(Db, tenant, user);
        }

        [Fact]
        public async Task InsertStartsPendingStampsUserAndRoundTripsEveryKindAndJobModeAsync()
        {
            var instance = await NewInstanceAsync(TenantA);

            foreach (var kind in Enum.GetValues<AdvancedAnalyticsKind>())
            {
                foreach (var mode in Enum.GetValues<AdvancedAnalyticsJobMode>())
                {
                    var inserted = await Runs(TenantA, "ZzTestTrainer").InsertAsync(new AdvancedAnalyticsTrainingRun
                    {
                        AdvancedAnalyticsInstanceId = instance.Id,
                        KindId = kind,
                        JobMode = mode,
                        StatusId = AdvancedAnalyticsStatus.Succeeded,
                        Progress = 0.9,
                        Seed = long.MaxValue
                    });

                    var read = await Runs(TenantA).GetByIdAsync(inserted.Id);

                    read!.KindId.Should().Be(kind);
                    read.JobMode.Should().Be(mode);
                    read.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
                    read.Progress.Should().Be(0);
                    read.UserName.Should().Be("ZzTestTrainer");
                    read.Seed.Should().Be(long.MaxValue);
                    read.Guid.Should().NotBeEmpty();
                    read.StartedDate.Should().BeNull();
                    read.CompletedDate.Should().BeNull();
                }
            }
        }

        [Fact]
        public async Task StatusIsStoredAsANumberAndEveryStatusRoundTripsAsync()
        {
            var instance = await NewInstanceAsync(TenantA);

            foreach (var status in Enum.GetValues<AdvancedAnalyticsStatus>())
            {
                var run = await NewRunAsync(TenantA, instance.Id);
                await Runs(TenantA).UpdateStatusAsync(run.Id, status);

                var stored = Db.Execute<int>(
                    "select \"StatusId\" from \"AdvancedAnalyticsTrainingRun\" where \"Id\" = " + run.Id);

                stored.Should().Be((int)status);
                (await Runs(TenantA).GetByIdAsync(run.Id))!.StatusId.Should().Be(status);
            }
        }

        [Fact]
        public async Task ArgumentsAndInputPointerRoundTripAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var pointer = Guid.NewGuid();

            var inserted = await Runs(TenantA).InsertAsync(new AdvancedAnalyticsTrainingRun
            {
                AdvancedAnalyticsInstanceId = instance.Id,
                KindId = AdvancedAnalyticsKind.Regressor,
                JobMode = AdvancedAnalyticsJobMode.Batch,
                Arguments = "{\"learningRate\":0.1,\"columns\":[\"a\",\"b\"]}",
                InputPointer = pointer
            });

            var read = await Runs(TenantA).GetByGuidAsync(inserted.Guid);

            read!.InputPointer.Should().Be(pointer);
            read.Arguments.Should().Contain("\"learningRate\"").And.Contain("0.1");
        }

        [Fact]
        public async Task StatusTransitionsStampTimingsAndTerminalStatesAreFinalAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var run = await NewRunAsync(TenantA, instance.Id);

            (await Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Running)).Should().BeTrue();
            var running = await Runs(TenantA).GetByIdAsync(run.Id);
            running!.StartedDate.Should().NotBeNull();
            running.CompletedDate.Should().BeNull();

            (await Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Running)).Should().BeTrue();
            (await Runs(TenantA).GetByIdAsync(run.Id))!.StartedDate.Should().Be(running.StartedDate);

            (await Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Failed, "boom")).Should()
                .BeTrue();

            var failed = await Runs(TenantA).GetByIdAsync(run.Id);
            failed!.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            failed.Error.Should().Be("boom");
            failed.CompletedDate.Should().NotBeNull();

            (await Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Succeeded)).Should().BeFalse();
            (await Runs(TenantA).GetByIdAsync(run.Id))!.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
        }

        [Fact]
        public async Task CancellingRunCanStillBecomeCancelledAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var run = await NewRunAsync(TenantA, instance.Id);

            await Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Running);
            await Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Cancelling);
            (await Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Cancelled)).Should().BeTrue();

            var read = await Runs(TenantA).GetByIdAsync(run.Id);
            read!.StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            read.CompletedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task ProgressIsClampedAndNanBecomesZeroAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var run = await NewRunAsync(TenantA, instance.Id);

            await Runs(TenantA).UpdateProgressAsync(run.Id, 7);
            (await Runs(TenantA).GetByIdAsync(run.Id))!.Progress.Should().Be(1);
            await Runs(TenantA).UpdateProgressAsync(run.Id, -3);
            (await Runs(TenantA).GetByIdAsync(run.Id))!.Progress.Should().Be(0);
            await Runs(TenantA).UpdateProgressAsync(run.Id, 0.375);
            (await Runs(TenantA).GetByIdAsync(run.Id))!.Progress.Should().Be(0.375);
            await Runs(TenantA).UpdateProgressAsync(run.Id, double.NaN);
            (await Runs(TenantA).GetByIdAsync(run.Id))!.Progress.Should().Be(0);
        }

        [Fact]
        public async Task ForeignTenantIsNotFoundOnEveryPathAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var run = await NewRunAsync(TenantA, instance.Id);

            (await Runs(TenantB).GetByIdAsync(run.Id)).Should().BeNull();
            (await Runs(TenantB).GetByGuidAsync(run.Guid)).Should().BeNull();
            (await Runs(TenantB).ListAsync()).Should().NotContain(c => c.Id == run.Id);

            await ((Func<Task>)(() => Runs(TenantB).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Running)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => Runs(TenantB).UpdateProgressAsync(run.Id, 0.5)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => Runs(TenantB).DeleteAsync(run.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => NewRunAsync(TenantB, instance.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            (await Runs(TenantA).GetByIdAsync(run.Id))!.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
        }

        [Fact]
        public async Task ListFiltersByInstanceAndStatusNewestFirstAndCapsTakeAsync()
        {
            var one = await NewInstanceAsync(TenantA);
            var two = await NewInstanceAsync(TenantA);
            var first = await NewRunAsync(TenantA, one.Id);
            var second = await NewRunAsync(TenantA, one.Id);
            var third = await NewRunAsync(TenantA, two.Id);
            await Runs(TenantA).UpdateStatusAsync(second.Id, AdvancedAnalyticsStatus.Running);

            (await Runs(TenantA).ListAsync(one.Id)).Select(s => s.Id).Should().Equal(second.Id, first.Id);

            (await Runs(TenantA).ListAsync(one.Id, AdvancedAnalyticsStatus.Running)).Select(s => s.Id)
                .Should().Equal(second.Id);

            (await Runs(TenantA).ListAsync(two.Id)).Select(s => s.Id).Should().Equal(third.Id);
            (await Runs(TenantA).ListAsync(one.Id, take: 1)).Should().HaveCount(1);
            (await Runs(TenantA).ListAsync(one.Id, take: 0)).Should().HaveCount(1);
        }

        [Fact]
        public async Task SoftDeleteHidesTheRunAndKeepsTheRowAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var run = await NewRunAsync(TenantA, instance.Id);

            await Runs(TenantA, "ZzTestDeleter").DeleteAsync(run.Id);

            (await Runs(TenantA).GetByIdAsync(run.Id)).Should().BeNull();
            (await Runs(TenantA).ListAsync(instance.Id)).Should().BeEmpty();
            var raw = Db.AdvancedAnalyticsTrainingRun.Single(s => s.Id == run.Id);
            raw.Deleted.Should().Be(1);
            raw.DeletedUser.Should().Be("ZzTestDeleter");

            await ((Func<Task>)(() => Runs(TenantA).DeleteAsync(run.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Running)))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task InsertAgainstADeletedInstanceIsNotFoundAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            await new AdvancedAnalyticsInstanceRepository(Db, TenantA, "ZzTestUser").DeleteAsync(instance.Id);

            await ((Func<Task>)(() => NewRunAsync(TenantA, instance.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task CancelledTokenStopsEveryOperationAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            var run = await NewRunAsync(TenantA, instance.Id);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;

            await ((Func<Task>)(() => Runs(TenantA).GetByIdAsync(run.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Runs(TenantA).ListAsync(null, null, 10, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Runs(TenantA).UpdateStatusAsync(run.Id, AdvancedAnalyticsStatus.Running, null,
                    token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Runs(TenantA).UpdateProgressAsync(run.Id, 0.5, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => Runs(TenantA).DeleteAsync(run.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            (await Runs(TenantA).GetByIdAsync(run.Id))!.StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
        }
    }
}