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

using Jube.Test.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Security;
using Jube.Dto.Authentication;
using Jube.Service.Authentication;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class AuthenticationSecretsInLogsTests(DatabaseFixture fx) : AuthenticationTestBase(fx)
{
    private const string Pw = "Sup3r-S3cret-PW!marker";
    private const string NewPw = "N3w-S3cret-PW!marker";
    private const string Wrong = "Wr0ng-S3cret-PW!marker";
    private const string MfaCode = "MFA-marker-913700";
    private const string JwtKey = "an-adequately-long-unit-test-signing-key-0123456789";

    private readonly List<string> secrets = [Pw, NewPw, Wrong, MfaCode, JwtKey, HashKey];

    private static readonly string[] scenarioNames =
    [
        "correct-login", "wrong-password", "unknown-user", "locked-user", "expired-password", "change-at-login",
        "weak-new-password", "lockout-sequence", "mfa-right", "mfa-wrong", "mfa-provider-down", "encrypted-right",
        "encrypted-garbage", "encrypted-wrong-key", "change-password-endpoint", "change-password-wrong",
        "negotiate-success", "negotiate-failure", "negotiate-mfa", "real-argon2", "http-login", "http-failures",
        "http-malformed-bodies", "wire-hash-probe", "hostile-inputs"
    ];

    public static TheoryData<string> Scenarios
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var name in scenarioNames)
            {
                data.Add(name);
            }

            return data;
        }
    }

    private async Task RunAsync(string scenario)
    {
        var user = await AddUserAsync("Logs" + Guid.NewGuid().ToString("N"), password: Pw);
        switch (scenario)
        {
            case "correct-login": await LoginAsync(user.Name, Pw); break;
            case "wrong-password": await LoginAsync(user.Name, Wrong); break;
            case "unknown-user": await LoginAsync($"{Prefix}Ghost", Pw); break;
            case "locked-user":
                var locked = await AddUserAsync("LogsLocked" + Guid.NewGuid().ToString("N"), u => u.PasswordLocked = 1);
                await LoginAsync(locked.Name, Pw);
                break;
            case "expired-password":
                var expired = await AddUserAsync("LogsExpired" + Guid.NewGuid().ToString("N"),
                    u => u.PasswordExpiryDate = DateTime.UtcNow.AddDays(-1));
                await LoginAsync(expired.Name, Pw);
                break;
            case "change-at-login": await LoginAsync(user.Name, Pw, d => d.NewPassword = NewPw); break;
            case "weak-new-password": await LoginAsync(user.Name, Pw, d => d.NewPassword = "weak"); break;
            case "lockout-sequence":
                for (var i = 0; i < 6; i++)
                {
                    await LoginAsync(user.Name, Wrong);
                }

                break;
            case "mfa-right":
                Mfa.Allow(user.Name, MfaCode);
                await LoginAsync(user.Name, Pw, d => d.Mfa = MfaCode, env: [MfaOn]);
                break;
            case "mfa-wrong": await LoginAsync(user.Name, Pw, d => d.Mfa = MfaCode, env: [MfaOn]); break;
            case "mfa-provider-down":
                Mfa.Throw = new InvalidOperationException("provider down");
                try
                {
                    await LoginAsync(user.Name, Pw, d => d.Mfa = MfaCode, env: [MfaOn]);
                }
                catch (InvalidOperationException)
                {
                }

                break;
            case "encrypted-right":
                secrets.Add(TestRsa.Encrypt(Pw));
                await LoginAsync(user.Name, TestRsa.Encrypt(Pw), env: Encrypted());
                break;
            case "encrypted-garbage": await LoginAsync(user.Name, $"{Wrong}%%%", env: Encrypted()); break;
            case "encrypted-wrong-key":
                secrets.Add(TestRsa.Encrypt(Pw, true));
                await LoginAsync(user.Name, TestRsa.Encrypt(Pw, true), env: Encrypted());
                break;
            case "change-password-endpoint":
                await using (var db = Fx.GetDbContext())
                {
                    await Service(db).ChangePasswordAsync(new ChangePasswordRequestDto(Pw, NewPw),
                        new NegotiateIdentity(true, user.Name));
                }

                break;
            case "change-password-wrong":
                await using (var db = Fx.GetDbContext())
                {
                    await Service(db).ChangePasswordAsync(new ChangePasswordRequestDto(Wrong, NewPw),
                        new NegotiateIdentity(true, user.Name));
                }

                break;
            case "negotiate-success":
                await using (var db = Fx.GetDbContext())
                {
                    await Service(db, NegotiateOn).ByNegotiateAsync(Context(new NegotiateIdentity(true, user.Name)));
                }

                break;
            case "negotiate-failure":
                await using (var db = Fx.GetDbContext())
                {
                    await Service(db, NegotiateOn)
                        .ByNegotiateAsync(Context(new NegotiateIdentity(true, $"{Prefix}Ghost")));
                }

                break;
            case "negotiate-mfa":
                await using (var db = Fx.GetDbContext())
                {
                    Mfa.Allow(user.Name, MfaCode);
                    await Service(db, NegotiateOn, MfaOn).ByNegotiateMfaAsync(
                        new AuthenticationRequestDto { Mfa = MfaCode },
                        Context(new NegotiateIdentity(true, user.Name)));
                }

                break;
            case "real-argon2":
                var hash = HashPassword.Argon2(Pw, HashKey);
                var real = await AddUserAsync("LogsArgon" + Guid.NewGuid().ToString("N"), storedHash: hash);
                secrets.AddRange(hash.Split('$').Where(p => p.Length >= 12));
                await using (var db = Fx.GetDbContext())
                {
                    var service = Service(db, new Argon2PasswordHashScheme());
                    await service.ByUserNamePasswordAsync(
                        new AuthenticationRequestDto { UserName = real.Name, Password = Wrong }, Context());
                    await service.ByUserNamePasswordAsync(
                        new AuthenticationRequestDto { UserName = real.Name, Password = Pw, NewPassword = NewPw },
                        Context());
                }

                secrets.AddRange((await ReloadAsync(real)).Password.Split('$').Where(p => p.Length >= 12));
                break;
            case "http-login":
                await using (var host = await AuthApiHost.StartAsync(Log, TimeProvider.System, Hash))
                {
                    var response = await host.PostAsync("/api/Authentication/ByUserNamePassword",
                        new { userName = user.Name, password = Pw }, ("User-Agent", Marker));
                    secrets.Add(System.Text.Json.JsonDocument.Parse(response.Body).RootElement.GetProperty("token")
                        .GetString().Required());
                }

                break;
            case "http-failures":
                await using (var host = await AuthApiHost.StartAsync(Log, TimeProvider.System, Hash))
                {
                    await host.PostAsync("/api/Authentication/ByUserNamePassword",
                        new { userName = user.Name, password = Wrong }, ("User-Agent", Marker));
                    await host.PostAsync("/api/Authentication/ByUserNamePassword",
                        new { userName = $"{Prefix}Ghost", password = Wrong }, ("User-Agent", Marker));
                }

                break;
            case "http-malformed-bodies":
                await using (var host = await AuthApiHost.StartAsync(Log, TimeProvider.System, Hash))
                {
                    foreach (var body in new[]
                             {
                                 $"{{\"password\":\"{Pw}\"", $"[\"{Pw}\"]", $"{{\"userName\":5,\"password\":\"{Pw}\"}}"
                             })
                    {
                        await host.SendAsync("POST", "/api/Authentication/ByUserNamePassword", body);
                        await host.SendAsync("POST", "/api/Authentication/ChangePassword", body);
                    }
                }

                break;
            case "wire-hash-probe":
                await using (var db = Fx.GetDbContext())
                {
                    await Service(db).WirePasswordHashAsync(new AuthenticationSchemaRequestDto(user.Name),
                        NegotiateIdentity.Anonymous);
                }

                break;
            default:
                foreach (var hostile in new[] { "' OR '1'='1", "\r\nFAKE: line", "\0", $"{Pw}\0{Wrong}" })
                {
                    await LoginAsync(hostile, Pw);
                    await LoginAsync(user.Name, hostile);
                }

                break;
        }
    }

    private (string, string)[] Encrypted() =>
    [
        ("PasswordAsymmetricEncryption", "True"),
        ("PasswordAsymmetricEncryptionPrivateKey", TestRsa.PrivateKeyEnv()),
        ("PasswordAsymmetricEncryptionPublicKey", "unused")
    ];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task NoSecretEverReachesAnyLogOrAuditLineAsync(string scenario)
    {
        await RunAsync(scenario);

        var text = AllLogText();
        foreach (var secret in secrets.Where(s => !String.IsNullOrEmpty(s)).Distinct())
        {
            text.Should().NotContain(secret, $"scenario '{scenario}' must not log a secret");
        }
    }

    [Fact]
    public async Task EveryScenario_TogetherProduceAtLeastOneAuditLine_PerOperation_AndNoneCarryAPasswordAsync()
    {
        foreach (var scenario in new[] { "correct-login", "wrong-password", "unknown-user", "negotiate-success" })
        {
            await RunAsync(scenario);
        }

        Audit.Entries.Should().HaveCountGreaterThanOrEqualTo(4);
        Audit.Entries.Should().OnlyContain(e => e.Message.StartsWith("area=Authentication op="));
        Audit.Entries.Select(e => e.Message).Should().NotContain(m => secrets.Any(m.Contains));
    }
}