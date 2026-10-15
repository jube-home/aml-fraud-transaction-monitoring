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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Data.Repository.Exceptions;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using Jube.Analytics.Datasets;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsCacheRepositoryTests(DatabaseFixture fx)
        : AdvancedAnalyticsRepositoryTestBase(fx)
    {
        private AdvancedAnalyticsCacheRepository ForTenant(int tenant,
            AdvancedAnalyticsCacheLimits? limits = null)
        {
            return new AdvancedAnalyticsCacheRepository(Db, tenant, "ZzTestUser", limits);
        }

        private static (byte[] Data, int Rows, int Columns) Packed(params double[] values)
        {
            return (AdvancedAnalyticsPacking.Pack(values), values.Length, 1);
        }

        [Fact]
        public async Task PutThenGetRoundTripsPackedDoublesBitExactlyAsync()
        {
            double[] values =
            [
                1.5, -0.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.Epsilon,
                double.MaxValue, 0.1 + 0.2
            ];

            var (data, rows, columns) = Packed(values);

            var put = await ForTenant(TenantA).PutAsync(data, rows, columns);
            var got = await ForTenant(TenantA).GetAsync(put.Guid);

            got.Should().NotBeNull();
            got!.Rows.Should().Be(values.Length);
            got.Columns.Should().Be(1);
            got.PackingVersion.Should().Be(AdvancedAnalyticsPacking.CurrentVersion);
            got.SizeBytes.Should().Be(data.Length);
            got.TenantRegistryId.Should().Be(TenantA);
            got.CreatedUser.Should().Be("ZzTestUser");

            var unpacked = AdvancedAnalyticsPacking.Unpack(got.Data, got.PackingVersion);

            unpacked.Select(BitConverter.DoubleToInt64Bits)
                .Should().Equal(values.Select(BitConverter.DoubleToInt64Bits));
        }

        [Fact]
        public async Task ForeignTenantKeyLooksLikeNotFoundAsync()
        {
            var (data, rows, columns) = Packed(1, 2, 3);
            var put = await ForTenant(TenantA).PutAsync(data, rows, columns);

            (await ForTenant(TenantB).GetAsync(put.Guid)).Should().BeNull();
            (await ForTenant(TenantB).GetMetadataAsync(put.Guid)).Should().BeNull();
            (await ForTenant(TenantB).DeleteAsync(put.Guid)).Should().BeFalse();
            (await ForTenant(TenantA).GetAsync(put.Guid)).Should().NotBeNull();
        }

        [Fact]
        public async Task ExpiredEntryLooksLikeNotFoundAndIsRemovedByCleanupAsync()
        {
            var (data, rows, columns) = Packed(1, 2, 3);
            var put = await ForTenant(TenantA).PutAsync(data, rows, columns);

            await Db.AdvancedAnalyticsCache.Where(w => w.Guid == put.Guid)
                .Set(s => s.ExpiryDate, DateTime.UtcNow.AddSeconds(-1))
                .UpdateAsync();

            (await ForTenant(TenantA).GetAsync(put.Guid)).Should().BeNull();
            (await ForTenant(TenantA).GetMetadataAsync(put.Guid)).Should().BeNull();
            Db.AdvancedAnalyticsCache.Count(w => w.Guid == put.Guid).Should().Be(1);

            var removed = await ForTenant(TenantA).DeleteExpiredAsync();

            removed.Should().BeGreaterThanOrEqualTo(1);
            Db.AdvancedAnalyticsCache.Count(w => w.Guid == put.Guid).Should().Be(0);
        }

        [Fact]
        public async Task TenantCleanupLeavesOtherTenantsExpiredRowsAndAllTenantsCleanupRemovesThemAsync()
        {
            var (data, rows, columns) = Packed(1);
            var a = await ForTenant(TenantA).PutAsync(data, rows, columns);
            var b = await ForTenant(TenantB).PutAsync(data, rows, columns);

            await Db.AdvancedAnalyticsCache.Where(w => w.Guid == a.Guid || w.Guid == b.Guid)
                .Set(s => s.ExpiryDate, DateTime.UtcNow.AddSeconds(-5))
                .UpdateAsync();

            await ForTenant(TenantA).DeleteExpiredAsync();

            Db.AdvancedAnalyticsCache.Count(w => w.Guid == a.Guid).Should().Be(0);
            Db.AdvancedAnalyticsCache.Count(w => w.Guid == b.Guid).Should().Be(1);

            await ForTenant(TenantA).DeleteExpiredAllTenantsAsync();

            Db.AdvancedAnalyticsCache.Count(w => w.Guid == b.Guid).Should().Be(0);
        }

        [Fact]
        public async Task CleanupNeverRemovesLiveEntriesAsync()
        {
            var (data, rows, columns) = Packed(1, 2);
            var live = await ForTenant(TenantA).PutAsync(data, rows, columns);

            await ForTenant(TenantA).DeleteExpiredAsync();
            await ForTenant(TenantA).DeleteExpiredAllTenantsAsync();

            (await ForTenant(TenantA).GetAsync(live.Guid)).Should().NotBeNull();
        }

        [Fact]
        public async Task TtlComesFromLimitsAndCanBeOverriddenPerEntryAsync()
        {
            var limits = new AdvancedAnalyticsCacheLimits { Ttl = TimeSpan.FromMinutes(5) };
            var (data, rows, columns) = Packed(1);

            var byLimit = await ForTenant(TenantA, limits).PutAsync(data, rows, columns);

            var byOverride = await ForTenant(TenantA, limits)
                .PutAsync(data, rows, columns, ttl: TimeSpan.FromHours(2));

            (byLimit.ExpiryDate - byLimit.CreatedDate).Should().BeCloseTo(TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(1));
            (byOverride.ExpiryDate - byOverride.CreatedDate).Should().BeCloseTo(TimeSpan.FromHours(2),
                TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ShortRealTtlExpiresAsync()
        {
            var (data, rows, columns) = Packed(1);
            var put = await ForTenant(TenantA).PutAsync(data, rows, columns, ttl: TimeSpan.FromMilliseconds(300));

            (await ForTenant(TenantA).GetAsync(put.Guid)).Should().NotBeNull();
            await Task.Delay(700);

            (await ForTenant(TenantA).GetAsync(put.Guid)).Should().BeNull();
        }

        [Fact]
        public async Task EntryLargerThanTheEntryLimitIsRejectedAndNothingIsStoredAsync()
        {
            var limits = new AdvancedAnalyticsCacheLimits { MaxEntryBytes = 24 };
            var (data, rows, columns) = Packed(1, 2, 3, 4);

            var put = () => ForTenant(TenantA, limits).PutAsync(data, rows, columns);

            await put.Should().ThrowAsync<AdvancedAnalyticsCacheLimitExceededException>();
            (await ForTenant(TenantA).GetTenantBytesAsync()).Should().Be(0);
        }

        [Fact]
        public async Task EntryAtTheEntryLimitIsAcceptedAsync()
        {
            var limits = new AdvancedAnalyticsCacheLimits { MaxEntryBytes = 24 };
            var (data, rows, columns) = Packed(1, 2, 3);

            var put = await ForTenant(TenantA, limits).PutAsync(data, rows, columns);

            put.SizeBytes.Should().Be(24);
        }

        [Fact]
        public async Task TenantTotalLimitIsEnforcedPerTenantAndIgnoresExpiredEntriesAsync()
        {
            var limits = new AdvancedAnalyticsCacheLimits { MaxTenantBytes = 32 };
            var (data, rows, columns) = Packed(1, 2);

            var first = await ForTenant(TenantA, limits).PutAsync(data, rows, columns);
            await ForTenant(TenantA, limits).PutAsync(data, rows, columns);
            var third = () => ForTenant(TenantA, limits).PutAsync(data, rows, columns);

            await third.Should().ThrowAsync<AdvancedAnalyticsCacheLimitExceededException>();
            (await ForTenant(TenantA).GetTenantBytesAsync()).Should().Be(32);

            var otherTenant = await ForTenant(TenantB, limits).PutAsync(data, rows, columns);
            otherTenant.Should().NotBeNull();

            await Db.AdvancedAnalyticsCache.Where(w => w.Guid == first.Guid)
                .Set(s => s.ExpiryDate, DateTime.UtcNow.AddSeconds(-1))
                .UpdateAsync();

            var afterExpiry = await ForTenant(TenantA, limits).PutAsync(data, rows, columns);
            afterExpiry.Should().NotBeNull();
            (await ForTenant(TenantA).GetTenantBytesAsync()).Should().Be(32);
        }

        [Fact]
        public async Task ShapeMustMatchTheDataLengthAsync()
        {
            var data = AdvancedAnalyticsPacking.Pack([1, 2, 3, 4]);

            var wrong = () => ForTenant(TenantA).PutAsync(data, 3, 2);
            var negative = () => ForTenant(TenantA).PutAsync(data, -1, -4);

            await wrong.Should().ThrowAsync<ArgumentException>();
            await negative.Should().ThrowAsync<ArgumentOutOfRangeException>();
            (await ForTenant(TenantA).PutAsync(data, 2, 2)).Rows.Should().Be(2);
        }

        [Fact]
        public Task NonPositiveTtlIsRejectedAsync()
        {
            var (data, rows, columns) = Packed(1);

            var put = () => ForTenant(TenantA).PutAsync(data, rows, columns, ttl: TimeSpan.Zero);

            return put.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task MetadataOmitsTheDataAndDeleteRemovesTheEntryAsync()
        {
            var (data, rows, columns) = Packed(1, 2, 3);
            var put = await ForTenant(TenantA).PutAsync(data, rows, columns);

            var metadata = await ForTenant(TenantA).GetMetadataAsync(put.Guid);

            metadata.Should().NotBeNull();
            metadata!.Data.Should().BeNull();
            metadata.SizeBytes.Should().Be(24);
            (await ForTenant(TenantA).DeleteAsync(put.Guid)).Should().BeTrue();
            (await ForTenant(TenantA).GetAsync(put.Guid)).Should().BeNull();
            (await ForTenant(TenantA).DeleteAsync(put.Guid)).Should().BeFalse();
        }

        [Fact]
        public async Task CancelledTokenStopsEveryOperationAsync()
        {
            var (data, rows, columns) = Packed(1);
            var put = await ForTenant(TenantA).PutAsync(data, rows, columns);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;
            var repository = ForTenant(TenantA);

            await ((Func<Task>)(() => repository.GetAsync(put.Guid, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => repository.PutAsync(data, rows, columns, token: token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => repository.DeleteExpiredAsync(token)))
                .Should().ThrowAsync<OperationCanceledException>();
            await ((Func<Task>)(() => repository.GetTenantBytesAsync(token)))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}