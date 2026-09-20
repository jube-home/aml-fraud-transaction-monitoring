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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using AuthService = Jube.Service.Authentication.AuthenticationLoginService;

namespace Jube.Test.Service.Authentication;

public abstract class AuthenticationTestBase(DatabaseFixture fx) : IAsyncLifetime
{
    public const string DefaultPassword = "Correct#Horse9battery";
    public const string HashKey = "unit-test-PasswordHashingKey";

    private Guid roleAGuid;
    private Guid roleBGuid;
    protected int TenantAId;
    protected int TenantBId;

    private string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    protected string Prefix => $"ZzTestAuth{Id}";
    protected string Marker => $"ZzTestAuthUA-{Id}";
    protected DatabaseFixture Fx { get; } = fx;
    protected FakeClock Clock { get; } = new(new DateTimeOffset(DateTime.UtcNow.Date.AddHours(12), TimeSpan.Zero));
    protected FastHashScheme Hash { get; } = new();
    protected CapturingCookieIssuer Cookies { get; } = new();
    private ScriptedMfa? mfa;

    protected ScriptedMfa Mfa => mfa.Required();
    protected TestLog Log { get; } = new();
    protected TestLog Audit { get; } = new();

    public async Task InitializeAsync()
    {
        mfa = new ScriptedMfa(Clock);
        await using var db = Fx.GetDbContext();
        TenantAId = await InsertTenantAsync(db, $"{Prefix}TenantA");
        TenantBId = await InsertTenantAsync(db, $"{Prefix}TenantB");
        roleAGuid = await InsertRoleAsync(db, $"{Prefix}RoleA", TenantAId);
        roleBGuid = await InsertRoleAsync(db, $"{Prefix}RoleB", TenantBId);
    }

    public virtual async Task DisposeAsync()
    {
        await using var db = Fx.GetDbContext();
        await db.UserLogin.Where(w =>
                w.UserAgent == Marker || (w.CreatedUser != null && w.CreatedUser.StartsWith(Prefix)))
            .DeleteAsync();
        await db.UserInTenant.Where(w => w.User != null && w.User.StartsWith(Prefix)).DeleteAsync();
        await db.UserRegistry.Where(w => w.Name != null && w.Name.StartsWith(Prefix)).DeleteAsync();
        await db.RoleRegistry.Where(w => w.Name != null && w.Name.StartsWith(Prefix)).DeleteAsync();
        await db.TenantRegistry.Where(w => w.Name != null && w.Name.StartsWith(Prefix)).DeleteAsync();
    }

    private static Task<int> InsertTenantAsync(DbContext db, string name)
    {
        return db.InsertWithInt32IdentityAsync(new TenantRegistry
        {
            Name = name, Active = 1, Locked = 0, Deleted = 0, Landlord = 0, Version = 1,
            CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
        });
    }

    private static async Task<Guid> InsertRoleAsync(DbContext db, string name, int tenantId)
    {
        var guid = Guid.NewGuid();
        await db.InsertWithInt32IdentityAsync(new RoleRegistry
        {
            Guid = guid, Name = name, Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenantId, Version = 1,
            CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
        });
        return guid;
    }

    protected async Task SetTenantAsync(Action<TenantRegistry> change)
    {
        await using var db = Fx.GetDbContext();
        var tenant = await db.TenantRegistry.FirstAsync(f => f.Id == TenantAId);
        change(tenant);
        await db.UpdateAsync(tenant);
    }

    protected async Task<TestUser> AddUserAsync(string tag, Action<UserRegistry>? tweak = null,
        string? password = DefaultPassword, bool inTenant = true, bool tenantB = false, string? exactName = null,
        string? storedHash = "\0default")
    {
        var name = exactName ?? $"{Prefix}{tag}";
        var hash = storedHash == "\0default"
            ? password == null ? null : FastHashScheme.Stored(password, HashKey)
            : storedHash;
        var user = new UserRegistry
        {
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = tenantB ? roleBGuid : roleAGuid,
            Name = name,
            Email = $"{Prefix}{tag}@example.invalid",
            Password = hash,
            Active = 1,
            PasswordLocked = 0,
            Deleted = 0,
            FailedPasswordCount = 0,
            PasswordCreatedDate = Clock.GetUtcNow().UtcDateTime,
            PasswordExpiryDate = Clock.GetUtcNow().UtcDateTime.AddDays(30),
            WirePasswordHash = 0,
            Version = 1,
            CreatedDate = DateTime.UtcNow,
            CreatedUser = DatabaseFixture.Prefix
        };
        tweak?.Invoke(user);

        await using var db = Fx.GetDbContext();
        var id = await db.InsertWithInt32IdentityAsync(user);
        if (inTenant)
        {
            await db.InsertAsync(new UserInTenant
                { User = name, TenantRegistryId = tenantB ? TenantBId : TenantAId });
        }

        return new TestUser(name, password ?? String.Empty, id, hash ?? String.Empty);
    }

    protected AuthService Service(DbContext db, params (string Key, string Value)[] env)
    {
        return Service(db, Hash, env);
    }

    protected AuthService Service(DbContext db, IPasswordHashScheme scheme, params (string Key, string Value)[] env)
    {
        var dynamicEnvironment = TestDynamicEnvironment.Create(env.ToDictionary(e => e.Key, e => e.Value));
        return new AuthService(db, dynamicEnvironment, Log, Cookies, Mfa, scheme, Clock, Audit);
    }

    protected AuthenticationRequestContext Context(NegotiateIdentity? identity = null, string? localIp = "10.0.0.1",
        string? remoteIp = "192.0.2.10")
    {
        return new AuthenticationRequestContext(identity ?? NegotiateIdentity.Anonymous, Marker, localIp, remoteIp);
    }

    protected async Task<AuthenticationOutcome> LoginAsync(string? userName, string? password,
        Action<AuthenticationRequestDto>? tweak = null, NegotiateIdentity? identity = null,
        CancellationToken token = default, params (string Key, string Value)[] env)
    {
        await using var db = Fx.GetDbContext();
        var dto = new AuthenticationRequestDto { UserName = userName, Password = password };
        tweak?.Invoke(dto);
        return await Service(db, env).ByUserNamePasswordAsync(dto, Context(identity), token);
    }

    protected async Task<UserRegistry> ReloadAsync(TestUser user)
    {
        await using var db = Fx.GetDbContext();
        return await db.UserRegistry.FirstAsync(f => f.Id == user.Id);
    }

    protected async Task<List<global::Jube.Data.Poco.UserLogin>> HistoryAsync()
    {
        await using var db = Fx.GetDbContext();
        return await db.UserLogin.Where(w => w.UserAgent == Marker).OrderBy(o => o.Id).ToListAsync();
    }

    protected string AllLogText()
    {
        return String.Join("\n", Log.Entries.Concat(Audit.Entries)
            .Select(e => $"{e.Level} {e.Message} {e.Exception}"));
    }

    protected static void AssertPlainOutcome(AuthenticationOutcome outcome, AuthenticationOutcomeKind kind)
    {
        outcome.Kind.Should().Be(kind);
        outcome.Response.Should().BeNull();
        outcome.Validation.Should().BeNull();
        outcome.Errors.Should().BeNull();
        outcome.Message.Should().BeNull();
        outcome.WirePasswordHash.Should().BeNull();
    }

    protected static readonly (string Key, string Value) NegotiateOn = ("NegotiateAuthentication", "True");
    protected static readonly (string Key, string Value) OAuthOn = ("OAuthAuthentication", "True");
    protected static readonly (string Key, string Value) MfaOn = ("EnableMultifactorAuthentication", "True");

    protected static (string Key, string Value) Attempts(int n)
    {
        return ("PasswordAttempts", n.ToString());
    }
}