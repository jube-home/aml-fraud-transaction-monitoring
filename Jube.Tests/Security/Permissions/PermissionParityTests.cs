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
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using LinqToDB;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Security.Permissions;

[Trait("Category", "PenTest")]
[Trait("Category", "PermissionParity")]
[Collection(PenTestCollection.Name)]
public sealed class PermissionParityTests(DatabaseFixture fx, ITestOutputHelper output) : PenTestBase(fx, output)
{
    private static readonly SemaphoreSlim gate = new(1, 1);
    private static readonly Dictionary<string, string> users = new();
    private static DatabaseFixture? owner;

    private static readonly (string Page, int Spec)[] roleAllocationPages =
    [
        ("/Model/EntityAnalysisModel", 6), ("/Model/Frame/CaseWorkflow", 18), ("/Model/Frame/CaseWorkflowStatus", 19),
        ("/Model/Frame/CaseWorkflowXPath", 20), ("/Model/Frame/CaseWorkflowForm", 21),
        ("/Model/Frame/CaseWorkflowAction", 22), ("/Model/Frame/CaseWorkflowDisplay", 23),
        ("/Model/Frame/CaseWorkflowMacro", 24), ("/Model/Frame/CaseWorkflowFilter", 25),
        ("/Administration/VisualisationRegistry", 31), ("/Administration/Frame/VisualisationRegistryParameter", 32),
        ("/Administration/Frame/VisualisationRegistryDatasource", 33)
    ];

    public static IEnumerable<object[]> RoleAllocationRows() =>
        roleAllocationPages.Select(p => new object[] { p.Page, p.Spec });

    private async Task<string> UserWithOnlyAsync(params int[] specs)
    {
        await gate.WaitAsync();
        try
        {
            if (!ReferenceEquals(owner, Fx))
            {
                users.Clear();
                owner = Fx;
            }

            var key = string.Join(",", specs.OrderBy(s => s));
            if (users.TryGetValue(key, out var existing))
            {
                return existing;
            }

            await using var db = Fx.GetDbContext();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantId = await db.UserInTenant.Where(w => w.User == Fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId).FirstAsync();
            var roleGuid = Guid.NewGuid();
            var name = $"{DatabaseFixture.Prefix}Par{key.Replace(",", "_")}{suffix}";
            var roleId = await db.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = name, Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenantId,
                Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            foreach (var spec in specs)
            {
                await db.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = roleId, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            await db.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = name, Email = $"{name}@example.invalid",
                Password = "not-used-by-permission-checks", Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            await db.InsertAsync(new UserInTenant { User = name, TenantRegistryId = tenantId });
            users[key] = name;
            return name;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<PenTestClient> ClientForOnlyAsync(params int[] specs) =>
        Anonymous.WithBearer(Host.TokenForName(await UserWithOnlyAsync(specs)));

    private static bool Denied(PenTestResponse response) =>
        response.Status is 401 or 403 or >= 300 and < 400;

    private async Task AssertAllowedAsync(PenTestClient client, string method, string target, string what)
    {
        var response = await client.SendAsync(method, target);
        Denied(response).Should().BeFalse($"{what}: {method} {target} => {response}");
        response.Status.Should().BeLessThan(500, $"{what}: {method} {target} => {response}");
    }

    private async Task AssertDeniedAsync(PenTestClient client, string method, string target, string what)
    {
        var response = await client.SendAsync(method, target);
        (response.Status is 401 or 403).Should().BeTrue($"{what}: {method} {target} => {response}");
    }

    [Theory]
    [MemberData(nameof(RoleAllocationRows))]
    public async Task RoleAllocationPage_OnlyItsOwnPermission_CanOpenAndLoadRolesAsync(string page, int spec)
    {
        var client = await ClientForOnlyAsync(spec);

        await AssertAllowedAsync(client, "GET", page, $"page {page} with only {spec}");
        await AssertAllowedAsync(client, "GET", "/api/RoleRegistry", $"role list for {page} with only {spec}");
        await AssertDeniedAsync(client, "DELETE", "/api/RoleRegistry/2147480001",
            $"role delete must stay with 34 ({page})");
        await AssertDeniedAsync(client, "GET", "/api/UserRegistry", $"unrelated user list ({page})");
    }

    public static IEnumerable<object[]> AuditTrailPages() =>
    [
        ["/Administration/UserLogin", "/api/UserLogin/"],
        ["/Administration/UserLogout", "/api/UserLogout/"]
    ];

    [Theory]
    [MemberData(nameof(AuditTrailPages))]
    public async Task AuditTrailPage_AndItsEndpoint_AreGatedByTheSamePermissionAsync(string page, string endpoint)
    {
        var withOnly35 = await ClientForOnlyAsync(35);
        await AssertAllowedAsync(withOnly35, "GET", page, $"page {page} with only 35");
        await AssertAllowedAsync(withOnly35, "GET", endpoint, $"endpoint {endpoint} with only 35");

        foreach (var spec in new[] { 1, 5, 6, 9, 18, 31, 34, 36, 40 })
        {
            var client = await ClientForOnlyAsync(spec);
            await AssertDeniedAsync(client, "GET", page, $"page {page} with only {spec}");
            await AssertDeniedAsync(client, "GET", endpoint, $"endpoint {endpoint} with only {spec}");
        }

        var none = Anonymous.WithBearer(Host.TokenForName(Fx.Seed.UserWithoutPermission));
        await AssertDeniedAsync(none, "GET", page, $"page {page} without the permission");
        await AssertDeniedAsync(none, "GET", endpoint, $"endpoint {endpoint} without the permission");
        await AssertDeniedAsync(Anonymous, "GET", endpoint, $"endpoint {endpoint} anonymously");
    }

    [Fact]
    public async Task UserLogoutMenuEntry_IsShownOnlyToTheHoldersOfItsPermissionAsync()
    {
        var withOnly35 = await ClientForOnlyAsync(35);
        var with35 = await withOnly35.GetAsync("/Account/ChangePassword");
        with35.Body.Should().Contain("/Administration/UserLogout").And.Contain("/Administration/UserLogin");

        var without = await ClientForOnlyAsync(5);
        var page = await without.GetAsync("/Account/ChangePassword");
        page.Body.Should().NotContain("/Administration/UserLogout");
    }

    [Fact]
    public async Task ChangePasswordFrame_IsGatedByUserRegistryPermissionAsync()
    {
        var withOnly35 = await ClientForOnlyAsync(35);
        await AssertAllowedAsync(withOnly35, "GET", "/Administration/Frame/ChangePassword", "frame with only 35");
        await AssertAllowedAsync(withOnly35, "GET", "/api/RoleRegistry", "role list with only 35");
        await AssertAllowedAsync(withOnly35, "GET", "/api/UserRegistry", "user list with only 35");

        foreach (var spec in new[] { 5, 9 })
        {
            var client = await ClientForOnlyAsync(spec);
            await AssertDeniedAsync(client, "GET", "/Administration/Frame/ChangePassword", $"frame with only {spec}");
            var response = await client.SendAsync("POST", "/api/UserRegistry/SetPassword",
                [.. "{\"id\":2147480001}"u8], "application/json");
            (response.Status is 401 or 403).Should().BeTrue($"set password with only {spec} => {response}");
        }
    }

    [Fact]
    public async Task CasePage_CanLoadTheModelTagsItNeeds_ButNotManageTagsAsync()
    {
        var client = await ClientForOnlyAsync(1);

        await AssertAllowedAsync(client, "GET", "/Case/Case", "case page with only 1");
        await AssertAllowedAsync(client, "GET", "/api/EntityAnalysisModelTag/ByEntityAnalysisModelId/2147480001",
            "tag list for the case grid with only 1");
        await AssertDeniedAsync(client, "GET", "/api/EntityAnalysisModelTag", "tag admin list with only 1");
        await AssertDeniedAsync(client, "DELETE", "/api/EntityAnalysisModelTag/2147480001", "tag delete with only 1");
    }

    [Fact]
    public async Task ModelSamplePage_CanListModels_ButNotChangeThemAsync()
    {
        var client = await ClientForOnlyAsync(40);

        await AssertAllowedAsync(client, "GET", "/Model/EntityAnalysisModelSample", "sample page with only 40");
        await AssertAllowedAsync(client, "GET", "/api/EntityAnalysisModel", "model list for the sample picker");
        await AssertDeniedAsync(client, "DELETE", "/api/EntityAnalysisModel/2147480001",
            "model delete with only 40");
    }

    [Fact]
    public async Task ExhaustivePage_ViewPermissionCannotPromote_ByDesignAsync()
    {
        var client = await ClientForOnlyAsync(16);

        await AssertAllowedAsync(client, "GET", "/Model/Frame/Exhaustive", "exhaustive frame with only 16");
        var response = await client.SendAsync("PUT", "/api/ExhaustiveSearchInstancePromotedTrialInstance",
            [.. "{\"id\":2147480001}"u8], "application/json");
        (response.Status is 401 or 403).Should().BeTrue($"promote with only 16 => {response}");
    }

    [Fact]
    public async Task EntityAnalysisInlineScript_IsSystemWideAndReadOnlyAsync()
    {
        var reader = await ClientForOnlyAsync(9);
        await AssertAllowedAsync(reader, "GET", "/api/EntityAnalysisInlineScript", "read with only 9");
        await AssertDeniedAsync(await ClientForOnlyAsync(1), "GET", "/api/EntityAnalysisInlineScript",
            "read without 9");

        foreach (var client in new[] { reader, As(PenTestUser.Landlord), As(PenTestUser.WithPermission) })
        {
            foreach (var method in new[] { "POST", "PUT", "DELETE", "PATCH" })
            {
                var response = await client.SendAsync(method, "/api/EntityAnalysisInlineScript/2147480001",
                    [.. "{}"u8], "application/json");
                response.Status.Should().BeOneOf([404, 405, 401, 403],
                    $"{method} on the global inline script library must not exist => {response}");
            }
        }
    }
}