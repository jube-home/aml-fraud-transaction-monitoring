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
using Jube.Data.Context;
using Jube.Service.Authentication;
using Jube.Service.UserLogout;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Service.Authentication
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AuthenticationLogoutServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly string tag = Guid.NewGuid().ToString("N")[..10];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.UserLogout.Where(w => w.CreatedUser != null && w.CreatedUser.EndsWith(tag)
                                                  || w.UserAgent != null && w.UserAgent.EndsWith(tag)
                                                  || w.RemoteIp != null && w.RemoteIp.EndsWith(tag)).DeleteAsync();
        }

        private string User => $"{DatabaseFixture.Prefix}Lo{tag}";

        private async Task SeedUserAsync(string name)
        {
            await using var dbContext = fx.GetDbContext();
            var template = await dbContext.UserRegistry.FirstAsync(f => f.Name == fx.Seed.UserWithoutPermission);
            var tenant = await dbContext.UserInTenant.FirstAsync(f => f.User == fx.Seed.UserWithoutPermission);
            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = template.RoleRegistryGuid, Name = name,
                Email = $"{name}@example.invalid", Password = "not-used", Active = 1, PasswordLocked = 0, Deleted = 0,
                Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            await dbContext.InsertAsync(new Data.Poco.UserInTenant
                { User = name, TenantRegistryId = tenant.TenantRegistryId });
        }

        private async Task DeleteUserAsync(string name)
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.UserInTenant.Where(w => w.User == name).DeleteAsync();
            await dbContext.UserRegistry.Where(w => w.Name == name).DeleteAsync();
        }

        private async Task<DateTime?> TokensValidFromAsync(string name)
        {
            await using var dbContext = fx.GetDbContext();
            return await dbContext.ExecuteAsync<DateTime?>(
                "SELECT \"TokensValidFrom\" FROM \"UserRegistry\" WHERE \"Name\" = @name",
                new DataParameter("name", name, DataType.NVarChar));
        }

        private AuthenticationLogoutService Service(TestLog log, Action<string>? abort = null, TestLog? audit = null,
            Func<DbContext>? factory = null)
        {
            return new AuthenticationLogoutService(factory ?? (() => fx.GetDbContext()), log, abort, null,
                audit ?? TestLog.NoOp);
        }

        [Fact]
        public async Task AValidSession_RevokesTheUsersTokens_ClosesTheirHubsAndRecordsTheRowAsync()
        {
            await SeedUserAsync(User);
            try
            {
                var aborted = new List<string>();
                var started = DateTime.UtcNow.AddMinutes(-9);
                var log = new TestLog();

                var outcome = await Service(log, aborted.Add).LogoutAsync(new AuthenticationLogoutRequest(
                    new NegotiateIdentity(true, User), $"10.1.2.3{tag}", "127.0.0.1", $"Agent {tag}", started));

                outcome.Should().Be(LogoutOutcome.Revoked);
                aborted.Should().Equal(User);
                var validFrom = await TokensValidFromAsync(User);
                validFrom.Should().NotBeNull();
                validFrom.Required().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

                await using var dbContext = fx.GetDbContext();
                var row = await dbContext.UserLogout.SingleAsync(w => w.CreatedUser == User);
                row.ReasonId.Should().Be(UserLogoutReason.UserLogout);
                row.OutcomeId.Should().Be(UserLogoutOutcome.Revoked);
                row.RemoteIp.Should().Be($"10.1.2.3{tag}");
                row.UserAgent.Should().Be($"Agent {tag}");
                row.SessionStartDate.Should().BeCloseTo(started, TimeSpan.FromSeconds(1));
                row.TenantRegistryId.Should().NotBeNull();
                row.CutByUser.Should().BeNull();
                log.Entries.Should().NotContain(e => e.Level == "ERROR");
            }
            finally
            {
                await DeleteUserAsync(User);
            }
        }

        [Fact]
        public async Task NoSession_RevokesNothing_ClosesNothing_AndNeverThrowsAsync()
        {
            var aborted = new List<string>();

            var outcome = await Service(new TestLog(), aborted.Add).LogoutAsync(new AuthenticationLogoutRequest(
                NegotiateIdentity.Anonymous, $"10.2.3.4{tag}"));

            outcome.Should().Be(LogoutOutcome.NoSession);
            aborted.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task AnAuthenticatedIdentityWithoutANameIsNoSessionAsync(string? name)
        {
            var outcome = await Service(new TestLog()).LogoutAsync(new AuthenticationLogoutRequest(
                new NegotiateIdentity(true, name), $"10.2.3.5{tag}"));

            outcome.Should().Be(LogoutOutcome.NoSession);
        }

        [Fact]
        public async Task ADatabaseFailure_StillEndsTheBrowserSession_LogsAnError_AndClosesTheHubsAsync()
        {
            var aborted = new List<string>();
            var log = new TestLog();

            var outcome = await Service(log, aborted.Add,
                    factory: () => throw new InvalidOperationException("database down"))
                .LogoutAsync(new AuthenticationLogoutRequest(new NegotiateIdentity(true, User), $"10.3.4.5{tag}"));

            outcome.Should().Be(LogoutOutcome.RevokeFailed);
            aborted.Should().Equal(User);
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("could not revoke")
                                                                 && e.Exception != null &&
                                                                 e.Exception.Message == "database down");
        }

        [Fact]
        public async Task AFailingHubClose_IsLogged_AndDoesNotFailTheLogoutAsync()
        {
            await SeedUserAsync(User);
            try
            {
                var log = new TestLog();

                var outcome = await Service(log, _ => throw new InvalidOperationException("hub gone"))
                    .LogoutAsync(new AuthenticationLogoutRequest(new NegotiateIdentity(true, User), $"10.4.5.6{tag}"));

                outcome.Should().Be(LogoutOutcome.Revoked);
                log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("hub connections"));
            }
            finally
            {
                await DeleteUserAsync(User);
            }
        }

        [Fact]
        public async Task TheAuditRow_IsBestEffort_AFailingInsertNeverChangesTheOutcomeAsync()
        {
            var log = new TestLog();

            var outcome = await Service(log, factory: () => throw new InvalidOperationException("no database"))
                .LogoutAsync(new AuthenticationLogoutRequest(new NegotiateIdentity(true, User), $"10.5.6.7{tag}"));

            outcome.Should().Be(LogoutOutcome.RevokeFailed);
            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains("audit row"));
        }

        [Fact]
        public async Task ALogoutIsAuditedWithItsOutcomeAndSpanAsync()
        {
            await SeedUserAsync(User);
            try
            {
                var audit = new TestLog();

                await Service(new TestLog(), audit: audit).LogoutAsync(new AuthenticationLogoutRequest(
                    new NegotiateIdentity(true, User), $"10.6.7.8{tag}"));

                audit.Entries.Should().ContainSingle().Which.Message.Should().Contain("area=Authentication")
                    .And.Contain("op=Logout").And.Contain($"actor={User}").And.Contain("outcome=revoked");
            }
            finally
            {
                await DeleteUserAsync(User);
            }
        }

        [Fact]
        public async Task TheActorOfTheAuditLineIsNeverControlCharactersOfTheNameAsync()
        {
            var audit = new TestLog();
            var hostile = $"{DatabaseFixture.Prefix}x\r\nFORGED actor=admin outcome=ok {tag}";

            await Service(new TestLog(), audit: audit, factory: () => throw new InvalidOperationException("x"))
                .LogoutAsync(new AuthenticationLogoutRequest(new NegotiateIdentity(true, hostile), $"10.7.8.9{tag}"));

            audit.Entries.Should().ContainSingle().Which.Message.Should().NotContain("\r").And.NotContain("\n");
        }

        [Fact]
        public async Task NoSessionRows_AreWrittenOncePerAddressPerMinute_ButAlwaysForASessionAsync()
        {
            var address = $"192.0.2.10{tag}";
            var request = new AuthenticationLogoutRequest(NegotiateIdentity.Anonymous, address, null, $"ua {tag}");

            for (var i = 0; i < 5; i++)
            {
                await Service(new TestLog()).LogoutAsync(request);
            }

            await using var dbContext = fx.GetDbContext();
            (await dbContext.UserLogout.CountAsync(w =>
                    w.RemoteIp == address && w.OutcomeId == UserLogoutOutcome.NoSession))
                .Should().Be(1);

            var other = request with { RemoteIp = $"192.0.2.11{tag}" };
            await Service(new TestLog()).LogoutAsync(other);
            (await dbContext.UserLogout.CountAsync(w => w.RemoteIp == $"192.0.2.11{tag}")).Should().Be(1);
        }

        [Fact]
        public async Task TheRecorder_ClipsLengthAndStripsControlCharactersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var entry = new UserLogoutEntry($"{DatabaseFixture.Prefix}Clip{tag}",
                UserLogoutReason.RevokedByAdministrator,
                UserLogoutOutcome.Revoked, $"1.1.1.1{tag}", null, new string('u', 4000) + "\u0007\r\n" + tag,
                null, "Admin\tX", "line1\nline2");

            await UserLogoutRecorder.RecordAsync(dbContext, TestLog.NoOp, entry);

            var row = await dbContext.UserLogout.SingleAsync(w => w.CreatedUser == entry.UserName);
            row.UserAgent.Should().HaveLength(512);
            row.UserAgent.Any(char.IsControl).Should().BeFalse();
            row.CutByUser.Should().Be("Admin?X");
            row.Message.Should().Be("line1?line2");
            row.ReasonId.Should().Be(2);
        }

        [Fact]
        public async Task TheRecorder_NeverThrows_WhateverTheDatabaseDoesAsync()
        {
            var log = new TestLog();

            DbContext Failing() => throw new InvalidOperationException("boom");

            await UserLogoutRecorder.RecordAsync((Func<DbContext>)Failing, log,
                new UserLogoutEntry($"{DatabaseFixture.Prefix}Boom{tag}", 1, 1));

            log.Entries.Should().ContainSingle(e => e.Level == "WARN");
        }
    }
}