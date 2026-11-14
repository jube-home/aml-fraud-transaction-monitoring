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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.CaseChecklist;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using ConflictException = Jube.Service.Exceptions.Repository.Case.ConflictException;

namespace Jube.Test.Service.Repository.CaseReport
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseReportCloseGateTests(DatabaseFixture fx) : CaseChecklistDemoTestBase(fx)
    {
        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly string plainUser = $"{DatabaseFixture.Prefix}Plain{Guid.NewGuid():N}"[..30];
        private int roleId;

        private async Task<(int CaseId, Guid Status, long ReportId)> ArrangeAsync(DbContext dbContext)
        {
            var tenant = (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission))!.Value;
            var roleGuid = Guid.NewGuid();

            roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid, Name = plainUser, Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenant,
                Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
            {
                Guid = Guid.NewGuid(), RoleRegistryId = roleId, PermissionSpecificationId = 1, Active = 1, Locked = 0,
                Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = plainUser,
                Email = $"{plainUser}@example.invalid", Password = "not-used", Active = 1, PasswordLocked = 0,
                Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = plainUser, TenantRegistryId = tenant });

            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, plainUser, fx.Seed.UserWithPermissionNoApproval,
                fx.Seed.UserWithPermission);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var reportId = await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.CaseReport
            {
                Guid = Guid.NewGuid(), TenantRegistryId = tenant, CaseId = caseId, ProfileName = "gate",
                ProfileVersion = 1, ReportCode = "STR", StateId = (short)CaseReportState.PendingApproval,
                CurrentVersion = 1, CreatedUser = "someone", CreatedDate = DateTime.UtcNow
            });

            return (caseId, workflow.Neutral, reportId);
        }

        private async Task CleanAsync(DbContext dbContext)
        {
            await dbContext.CaseReport.Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.CaseEvent.Where(w => w.CaseId != null && seeder.CaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
            await dbContext.UserInTenant.Where(w => w.User == plainUser).DeleteAsync();
            await dbContext.UserRegistry.Where(w => w.Name == plainUser).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistryId == roleId).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => w.Id == roleId).DeleteAsync();
        }

        [Fact]
        public async Task AnAwaitingReportBlocksClosingAndOnlyReportHoldersAreToldWhyAsync()
        {
            await using var dbContext = fx.GetDbContext();

            try
            {
                var (caseId, status, _) = await ArrangeAsync(dbContext);

                var plain = await CasesAsync(dbContext, plainUser);
                var refusal = await FluentActions.Awaiting(() => plain.UpdateAsync(Move(caseId, status, 3, plainUser)))
                    .Should().ThrowAsync<ConflictException>();
                refusal.Which.Message.Should().Be("This case cannot be closed yet. Ask a supervisor to review it.");

                var filer = await CasesAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
                var informed = await FluentActions.Awaiting(() => filer.UpdateAsync(
                    Move(caseId, status, 3, fx.Seed.UserWithPermissionNoApproval))).Should().ThrowAsync<ConflictException>();
                informed.Which.Message.Should().Contain("awaiting approval or filing");

                (await dbContext.Case.FirstAsync(w => w.Id == caseId)).ClosedStatusId.Should().NotBe((byte)3);
            }
            finally
            {
                await CleanAsync(dbContext);
            }
        }

        [Fact]
        public async Task AnApproverMayCloseAndASubmittedReportNoLongerBlocksAnyoneAsync()
        {
            await using var dbContext = fx.GetDbContext();

            try
            {
                var (caseId, status, reportId) = await ArrangeAsync(dbContext);

                var approver = await CasesAsync(dbContext, fx.Seed.UserWithPermission);
                await approver.UpdateAsync(Move(caseId, status, 3, fx.Seed.UserWithPermission));
                (await dbContext.Case.FirstAsync(w => w.Id == caseId)).ClosedStatusId.Should().Be((byte)3);

                await dbContext.Case.Where(w => w.Id == caseId).Set(s => s.ClosedStatusId, (byte)0).Set(s => s.Locked, (byte?)0)
                    .Set(s => s.LockedUser, (string?)null).UpdateAsync();
                await dbContext.CaseReport.Where(w => w.Id == reportId)
                    .Set(s => s.StateId, (short)CaseReportState.Submitted).UpdateAsync();

                var plain = await CasesAsync(dbContext, plainUser);
                await plain.UpdateAsync(Move(caseId, status, 3, plainUser));
                (await dbContext.Case.FirstAsync(w => w.Id == caseId)).ClosedStatusId.Should().Be((byte)3);
            }
            finally
            {
                await CleanAsync(dbContext);
            }
        }
    }
}
