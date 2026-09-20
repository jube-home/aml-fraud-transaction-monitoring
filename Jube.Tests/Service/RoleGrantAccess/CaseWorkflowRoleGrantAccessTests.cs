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
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.RoleGrantAccess
{
    public sealed class CaseWorkflowRoleGrantAccessTests(DatabaseFixture fx) : RoleGrantAccessBase(fx)
    {
        public static IEnumerable<object[]> Subjects =>
            new[]
            {
                "CaseWorkflow", "CaseWorkflowStatus", "CaseWorkflowXPath", "CaseWorkflowAction", "CaseWorkflowDisplay",
                "CaseWorkflowFilter", "CaseWorkflowForm", "CaseWorkflowMacro"
            }.Select(subject => new object[] { subject });

        private async Task<(RoleGrantWorld World, ArrangedSubject Subject)> ArrangeAsync(string subject)
        {
            var world = await CreateWorldAsync();
            var (workflowId, workflowGuid) = await CreateWorkflowAsync();

            if (subject == "CaseWorkflow")
            {
                return (world, new ArrangedSubject("CaseWorkflowRole", workflowGuid, async userName =>
                {
                    await using var dbContext = Fx.GetDbContext();
                    return await new Data.Repository.CaseWorkflowRepository(dbContext, userName)
                        .GetByGuidActiveOnlyWithRoleAsync(workflowGuid) != null;
                }));
            }

            if (subject != "CaseWorkflowForm")
            {
                await GrantAsync("CaseWorkflowRole", workflowGuid, world.RoleAGuid);
                await GrantAsync("CaseWorkflowRole", workflowGuid, world.RoleBGuid);
            }

            await using var db = Fx.GetDbContext();
            var childGuid = Guid.NewGuid();
            var name = $"{DatabaseFixture.Prefix}RG{subject}{Guid.NewGuid():N}"[..40];
            var createdUser = Fx.Seed.UserWithPermission;
            var now = DateTime.UtcNow;

            switch (subject)
            {
                case "CaseWorkflowStatus":
                    await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
                    {
                        Name = name, Guid = childGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0,
                        Deleted = 0, Priority = 3, Version = 1, CreatedUser = createdUser, CreatedDate = now
                    });
                    return (world, new ArrangedSubject("CaseWorkflowStatusRole", childGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.CaseWorkflowStatusRepository(dbContext, userName)
                            .GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid)).Any(w => w.Guid == childGuid);
                    }));
                case "CaseWorkflowXPath":
                    await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowXPath
                    {
                        Name = name, Guid = childGuid, CaseWorkflowId = workflowId, XPath = "/Case/Field", Active = 1,
                        Locked = 0, Deleted = 0, CreatedUser = createdUser, CreatedDate = now
                    });
                    return (world, new ArrangedSubject("CaseWorkflowXPathRole", childGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.CaseWorkflowXPathRepository(dbContext, userName)
                            .GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid)).Any(w => w.Guid == childGuid);
                    }));
                case "CaseWorkflowAction":
                    await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowAction
                    {
                        Name = name, Guid = childGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0,
                        Deleted = 0, EnableNotification = 0, EnableHttpEndpoint = 0, CreatedUser = createdUser,
                        CreatedDate = now
                    });
                    return (world, new ArrangedSubject("CaseWorkflowActionRole", childGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.CaseWorkflowActionRepository(dbContext, userName)
                            .GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid)).Any(w => w.Guid == childGuid);
                    }));
                case "CaseWorkflowDisplay":
                    await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowDisplay
                    {
                        Name = name, Guid = childGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0,
                        Deleted = 0, Html = "<div></div>", CreatedUser = createdUser, CreatedDate = now
                    });
                    return (world, new ArrangedSubject("CaseWorkflowDisplayRole", childGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.CaseWorkflowDisplayRepository(dbContext, userName)
                            .GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid)).Any(w => w.Guid == childGuid);
                    }));
                case "CaseWorkflowFilter":
                    await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowFilter
                    {
                        Name = name, Guid = childGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0,
                        Deleted = 0, CreatedUser = createdUser, CreatedDate = now
                    });
                    return (world, new ArrangedSubject("CaseWorkflowFilterRole", childGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.CaseWorkflowFilterRepository(dbContext, userName)
                            .GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid)).Any(w => w.Guid == childGuid);
                    }));
                case "CaseWorkflowForm":
                    await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowForm
                    {
                        Name = name, Guid = childGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0,
                        Deleted = 0, Html = "<div></div>", EnableHttpEndpoint = 0, EnableNotification = 0,
                        CreatedUser = createdUser, CreatedDate = now
                    });
                    return (world, new ArrangedSubject("CaseWorkflowFormRole", childGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.CaseWorkflowFormRepository(dbContext, userName)
                            .GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid)).Any(w => w.Guid == childGuid);
                    }));
                case "CaseWorkflowMacro":
                    await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowMacro
                    {
                        Name = name, Guid = childGuid, CaseWorkflowId = workflowId, Active = 1, Locked = 0,
                        Deleted = 0, Javascript = "console.log('test');", EnableHttpEndpoint = 0,
                        EnableNotification = 0, CreatedUser = createdUser, CreatedDate = now
                    });
                    return (world, new ArrangedSubject("CaseWorkflowMacroRole", childGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.CaseWorkflowMacroRepository(dbContext, userName)
                            .GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid)).Any(w => w.Guid == childGuid);
                    }));
                default:
                    // ReSharper disable once NotResolvedInText
                    // ReSharper disable once LocalizableElement
                    throw new ArgumentOutOfRangeException(nameof(subject), subject, "Unknown subject.");
            }
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task NoGrantMeansNoAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioNoGrantMeansNoAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task GrantGivesAccessToThatRoleOnlyAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioGrantGivesAccessToThatRoleOnlyAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task RevokeRemovesAccessAtOnceAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioRevokeRemovesAccessAtOnceAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task RegrantAfterRevokeRestoresAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioRegrantAfterRevokeRestoresAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task EachRoleKeepsItsOwnAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioEachRoleKeepsItsOwnAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task DeletedRoleLosesAccessWithoutErrorsAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioDeletedRoleLosesAccessWithoutErrorsAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task MovingAUserToAnotherRoleMovesTheirAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioMovingAUserToAnotherRoleMovesTheirAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task AnotherTenantsRoleNeverGrantsAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioAnotherTenantsRoleNeverGrantsAsync(arranged, world);
        }
    }
}