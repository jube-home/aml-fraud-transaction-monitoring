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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseReport;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseReport;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.CaseReport
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseReportServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:example:report"
                       xmlns="urn:example:report" elementFormDefault="qualified">
              <xs:element name="report">
                <xs:complexType><xs:sequence>
                  <xs:element name="caseKey" type="xs:string"/>
                  <xs:element name="reason" type="xs:string"/>
                  <xs:element name="amount" type="xs:decimal" minOccurs="0"/>
                </xs:sequence></xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        private const string Template = """
            <report xmlns="urn:example:report" xmlns:jr="urn:jube:regulatory-report:template:1">
              <caseKey jr:value="case.keyValue" jr:required="true"/>
              <reason jr:value="fields.reason" jr:required="true"/>
              <amount jr:value="fields.amount|number:0.00"/>
            </report>
            """;

        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly string profileName = "ErProfile" + Guid.NewGuid().ToString("N")[..8];
        private readonly List<Guid> reports = [];

        private string Filer => fx.Seed.UserWithPermissionNoApproval;
        private string Approver => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var ids = await dbContext.CaseReport.Where(w => reports.Contains(w.Guid)).Select(s => s.Id).ToListAsync();
            await dbContext.CaseReportFeedback.Where(w => ids.Contains(w.CaseReportId)).DeleteAsync();
            await dbContext.CaseReportApproval.Where(w => ids.Contains(w.CaseReportId)).DeleteAsync();
            await dbContext.CaseReportVersion.Where(w => ids.Contains(w.CaseReportId)).DeleteAsync();
            await dbContext.CaseReport.Where(w => ids.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseReportProfile.Where(w => w.Name == profileName).DeleteAsync();
            await dbContext.UserNotification.Where(w => w.DedupeKey.StartsWith("case-report-")
                                                        && seeder.CaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseReportService> BuildAsync(DbContext dbContext, string user) =>
            CaseReportService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<int> VisibleCaseAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Filer, Approver);

            return await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "AccountR");
        }

        private async Task ImportProfileAsync(DbContext dbContext)
        {
            var profiles = await CaseReportProfileService.CreateAsync(dbContext, Approver, TestLog.NoOp,
                new NullServiceChangeBus());

            var result = await profiles.ImportAsync(new CaseReportProfileDto
            {
                Name = profileName, Description = "Example", TemplateXml = Template,
                SchemaDocuments = new Dictionary<string, string> { ["example.xsd"] = Schema }
            });

            result.Valid.Should().BeTrue(string.Join("; ", result.Issues.Select(i => i.Message)));
        }

        private async Task<(CaseReportService Filer, CaseReportService Approver, int CaseId)> ArrangeAsync(
            DbContext dbContext)
        {
            await ImportProfileAsync(dbContext);

            return (await BuildAsync(dbContext, Filer), await BuildAsync(dbContext, Approver),
                await VisibleCaseAsync(dbContext));
        }

        private async Task<CaseReportDto> StartAsync(CaseReportService filer, int caseId)
        {
            var report = await filer.StartAsync(new CaseReportStartDto
                { CaseId = caseId, ProfileName = profileName, ReportCode = "STR" });
            reports.Add(report.Guid);

            return report;
        }

        private async Task<CaseReportDto> ValidatedAsync(CaseReportService filer, int caseId)
        {
            var report = await StartAsync(filer, caseId);
            await filer.SaveAsync(new CaseReportSaveDto { Guid = report.Guid, FieldsJson = "{\"reason\":\"Structuring\",\"amount\":\"1250.5\"}" });

            return await filer.ValidateAsync(report.Guid);
        }

        [Fact]
        public async Task AReportIsStartedSavedValidatedAndItsXmlIsStoredWithItsHashAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, _, caseId) = await ArrangeAsync(dbContext);

            var validated = await ValidatedAsync(filer, caseId);

            validated.StateId.Should().Be((int)CaseReportState.Validated);
            validated.Issues.Should().BeEmpty();
            validated.Versions.Should().ContainSingle().Which.HasXml.Should().BeTrue();
            var version = validated.Versions[0];
            version.PayloadSha256.Should().HaveLength(64);

            var row = await dbContext.CaseReportVersion.SingleAsync(w => w.PayloadSha256 == version.PayloadSha256);
            var xml = row.Xml!;
            xml.Should().Contain("<caseKey>AccountR</caseKey>").And.Contain("<reason>Structuring</reason>")
                .And.Contain("<amount>1250.50</amount>");
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml))).ToLowerInvariant().Should().Be(version.PayloadSha256);
        }

        [Fact]
        public async Task AValidationProblemNamesTheFieldAndLeavesTheReportAsADraftAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, _, caseId) = await ArrangeAsync(dbContext);
            var report = await StartAsync(filer, caseId);

            var result = await filer.ValidateAsync(report.Guid);

            result.StateId.Should().Be((int)CaseReportState.Draft);
            result.Issues.Should().ContainSingle(i => i.Code == "MissingRequired" && i.Path == "$.fields.reason");
            result.Versions[0].HasXml.Should().BeFalse();
        }

        [Fact]
        public async Task EditingAValidatedReportMakesANewVersionAndTheOldOneIsKeptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, _, caseId) = await ArrangeAsync(dbContext);
            var validated = await ValidatedAsync(filer, caseId);

            var edited = await filer.SaveAsync(new CaseReportSaveDto { Guid = validated.Guid, FieldsJson = "{\"reason\":\"Changed\"}" });

            edited.StateId.Should().Be((int)CaseReportState.Draft);
            edited.CurrentVersion.Should().Be(2);
            edited.Versions.Select(v => (v.Version, v.HasXml)).Should().Equal((2, false), (1, true));
            edited.Versions[0].SupersedesVersion.Should().Be(1);
        }

        [Fact]
        public async Task ASecondPersonApprovesNeverTheAuthorAndOnlyAPendingReportAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var validated = await ValidatedAsync(filer, caseId);

            await ((Func<Task>)(() => approver.ApproveAsync(new CaseReportDecisionDto { Guid = validated.Guid })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();

            await filer.RequestApprovalAsync(validated.Guid);

            await ((Func<Task>)(() => filer.ApproveAsync(new CaseReportDecisionDto { Guid = validated.Guid })))
                .Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var selfApprover = await BuildAsync(dbContext, Approver);
            var own = await StartAsync(selfApprover, caseId);
            await selfApprover.SaveAsync(new CaseReportSaveDto { Guid = own.Guid, FieldsJson = "{\"reason\":\"x\"}" });
            await selfApprover.ValidateAsync(own.Guid);
            await selfApprover.RequestApprovalAsync(own.Guid);
            await ((Func<Task>)(() => selfApprover.ApproveAsync(new CaseReportDecisionDto { Guid = own.Guid })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();

            var approved = await approver.ApproveAsync(new CaseReportDecisionDto { Guid = validated.Guid, Note = "Fine." });
            approved.StateId.Should().Be((int)CaseReportState.Approved);
            approved.Approvals.Should().ContainSingle().Which.CreatedUser.Should().Be(Approver);
        }

        [Fact]
        public async Task ARejectionNeedsAReasonAndReturnsTheReportToDraftAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var validated = await ValidatedAsync(filer, caseId);
            await filer.RequestApprovalAsync(validated.Guid);

            await ((Func<Task>)(() => approver.RejectAsync(new CaseReportDecisionDto { Guid = validated.Guid })))
                .Should().ThrowAsync<ArgumentException>();

            var rejected = await approver.RejectAsync(new CaseReportDecisionDto { Guid = validated.Guid, Note = "Wrong amount." });
            rejected.StateId.Should().Be((int)CaseReportState.Draft);
            rejected.Approvals.Single().DecisionId.Should().Be((int)CaseReportDecision.Rejected);
        }

        [Fact]
        public async Task DownloadNeedsAnApprovedReportReturnsTheStoredBytesAndChecksTheirHashAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var validated = await ValidatedAsync(filer, caseId);

            await ((Func<Task>)(() => filer.DownloadAsync(validated.Guid))).Should()
                .ThrowAsync<CaseExceptions.ConflictException>();

            await filer.RequestApprovalAsync(validated.Guid);
            await approver.ApproveAsync(new CaseReportDecisionDto { Guid = validated.Guid });

            var (bytes, name, sha) = await filer.DownloadAsync(validated.Guid);
            name.Should().EndWith(".xml");
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant().Should().Be(sha);

            var row = await dbContext.CaseReportVersion.SingleAsync(w => w.PayloadSha256 == sha);
            await dbContext.CaseReportVersion.Where(w => w.Id == row.Id).Set(s => s.Xml, "<tampered/>").UpdateAsync();
            await ((Func<Task>)(() => filer.DownloadAsync(validated.Guid))).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task SubmittingRecordsTheReferenceAndPutsTheCaseUnderLegalHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var validated = await ValidatedAsync(filer, caseId);
            await filer.RequestApprovalAsync(validated.Guid);
            await approver.ApproveAsync(new CaseReportDecisionDto { Guid = validated.Guid });

            await ((Func<Task>)(() => filer.RecordSubmissionAsync(new CaseReportSubmissionDto { Guid = validated.Guid })))
                .Should().ThrowAsync<ArgumentException>();

            var submitted = await filer.RecordSubmissionAsync(new CaseReportSubmissionDto
                { Guid = validated.Guid, FiuReference = " FIU-123 " });

            (submitted.StateId, submitted.FiuReference, submitted.SubmittedUser).Should()
                .Be(((int)CaseReportState.Submitted, "FIU-123", Filer));
            var theCase = await dbContext.Case.SingleAsync(w => w.Id == caseId);
            (theCase.LegalHold, theCase.LegalHoldReason).Should().Be(((byte?)1, CaseReportService.HoldReason));

            await ((Func<Task>)(() => filer.RecordSubmissionAsync(new CaseReportSubmissionDto { Guid = validated.Guid, FiuReference = "again" })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
        }

        [Fact]
        public async Task FiuFeedbackAcknowledgesOrRejectsAndARejectedReportIsCorrectedAsANewVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var validated = await ValidatedAsync(filer, caseId);
            await filer.RequestApprovalAsync(validated.Guid);
            await approver.ApproveAsync(new CaseReportDecisionDto { Guid = validated.Guid });
            await filer.RecordSubmissionAsync(new CaseReportSubmissionDto { Guid = validated.Guid, FiuReference = "R1" });

            var rejected = await filer.RecordFeedbackAsync(new CaseReportFeedbackRequestDto
                { Guid = validated.Guid, KindId = 2, Message = "Bad country.", FieldPath = "/report/country" });
            rejected.StateId.Should().Be((int)CaseReportState.Rejected);
            rejected.Feedback.Should().ContainSingle().Which.FieldPath.Should().Be("/report/country");

            var corrected = await filer.NewVersionAsync(validated.Guid);
            (corrected.StateId, corrected.CurrentVersion).Should().Be(((int)CaseReportState.Draft, 2));
            corrected.Versions[0].SupersedesVersion.Should().Be(1);
            corrected.FieldsJson.Should().Contain("Structuring");
        }

        [Fact]
        public async Task AnAcknowledgementEndsTheReportAndBadFeedbackIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var validated = await ValidatedAsync(filer, caseId);

            await ((Func<Task>)(() => filer.RecordFeedbackAsync(new CaseReportFeedbackRequestDto { Guid = validated.Guid, KindId = 1 })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();

            await filer.RequestApprovalAsync(validated.Guid);
            await approver.ApproveAsync(new CaseReportDecisionDto { Guid = validated.Guid });
            await filer.RecordSubmissionAsync(new CaseReportSubmissionDto { Guid = validated.Guid, FiuReference = "R2" });

            await ((Func<Task>)(() => filer.RecordFeedbackAsync(new CaseReportFeedbackRequestDto { Guid = validated.Guid, KindId = 9 })))
                .Should().ThrowAsync<ArgumentException>();

            var acknowledged = await filer.RecordFeedbackAsync(new CaseReportFeedbackRequestDto { Guid = validated.Guid, KindId = 1 });
            acknowledged.StateId.Should().Be((int)CaseReportState.Acknowledged);
        }

        [Fact]
        public async Task WithdrawingIsForTheAuthorOrAnApproverAndOnlyBeforeSubmissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var one = await ValidatedAsync(filer, caseId);
            var two = await ValidatedAsync(filer, caseId);

            (await filer.WithdrawAsync(one.Guid)).StateId.Should().Be((int)CaseReportState.Withdrawn);
            (await approver.WithdrawAsync(two.Guid)).StateId.Should().Be((int)CaseReportState.Withdrawn);
            await ((Func<Task>)(() => filer.WithdrawAsync(one.Guid))).Should().ThrowAsync<CaseExceptions.ConflictException>();
        }

        [Fact]
        public async Task ReportsAreVisibleOnlyToHoldersOfThePermissionsAndOnlyOnVisibleCasesAndTenantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, _, caseId) = await ArrangeAsync(dbContext);
            var report = await ValidatedAsync(filer, caseId);

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.GetAsync(report.Guid))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.StartAsync(new CaseReportStartDto { CaseId = caseId, ProfileName = profileName, ReportCode = "X" })))
                .Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var other = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await ((Func<Task>)(() => other.GetAsync(report.Guid))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => other.ListAsync(caseId))).Should().ThrowAsync<CaseExceptions.NotFoundException>();

            var viewOnly = await BuildAsync(dbContext, Approver);
            (await viewOnly.ListAsync(caseId)).Should().ContainSingle(r => r.Guid == report.Guid);
            (await CaseReportService.HoldsAnyAsync(dbContext, fx.Seed.UserWithoutPermission, TestLog.NoOp)).Should().BeFalse();
        }

        [Fact]
        public async Task OnlyAnApproverSeesTheAwaitingListAndOnlyForCasesTheyCanOpenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var report = await ValidatedAsync(filer, caseId);
            await filer.RequestApprovalAsync(report.Guid);

            (await approver.ListAwaitingApprovalAsync()).Select(r => r.Guid).Should().Contain(report.Guid);
            await ((Func<Task>)(() => filer.ListAwaitingApprovalAsync())).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
        }

        [Fact]
        public async Task ApprovalAndDecisionNotifyTheRightPeopleOncePerStepAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, approver, caseId) = await ArrangeAsync(dbContext);
            var report = await ValidatedAsync(filer, caseId);
            await filer.RequestApprovalAsync(report.Guid);
            await approver.ApproveAsync(new CaseReportDecisionDto { Guid = report.Guid });

            var rows = await dbContext.UserNotification.Where(w => w.CaseId == caseId).ToListAsync();
            rows.Where(r => r.KindId == (short)NotificationKind.CaseReportAwaitingApproval)
                .Select(r => r.RecipientUser).Should().NotContain(Filer);
            rows.Should().Contain(r => r.KindId == (short)NotificationKind.CaseReportDecided && r.RecipientUser == Filer);

            using var arguments = System.Text.Json.JsonDocument.Parse(rows
                .Single(r => r.KindId == (short)NotificationKind.CaseReportDecided && r.RecipientUser == Filer).ArgumentsJson);
            arguments.RootElement.GetProperty("detailId").GetInt32().Should().Be(1);
            arguments.RootElement.TryGetProperty("detail", out _).Should().BeFalse();
        }

        [Fact]
        public async Task AProfileIsImportedAsAnAppendOnlyVersionAndABadOneIsRefusedWithItsIssuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await ImportProfileAsync(dbContext);
            var profiles = await CaseReportProfileService.CreateAsync(dbContext, Approver, TestLog.NoOp, new NullServiceChangeBus());

            var second = await profiles.ImportAsync(new CaseReportProfileDto
                { Name = profileName, TemplateXml = Template, SchemaDocuments = new Dictionary<string, string> { ["example.xsd"] = Schema } });
            second.Version.Should().Be(2);

            var bad = await profiles.ImportAsync(new CaseReportProfileDto
                { Name = profileName, TemplateXml = "<report", SchemaDocuments = new Dictionary<string, string> { ["x.xsd"] = "<xs:schema" } });
            bad.Valid.Should().BeFalse();
            bad.Issues.Select(i => i.Code).Should().Contain(["TemplateInvalid", "SchemaInvalid"]);

            (await profiles.ListAsync()).Should().Contain(p => p.Name == profileName && p.Version == 2);
            (await profiles.GetAsync(profileName)).Version.Should().Be(2);
        }

        [Fact]
        public async Task ProfilesAreImportedOnlyByApproversAndStayInTheirTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await ImportProfileAsync(dbContext);

            var filerProfiles = await CaseReportProfileService.CreateAsync(dbContext, Filer, TestLog.NoOp, new NullServiceChangeBus());
            await ((Func<Task>)(() => filerProfiles.ImportAsync(new CaseReportProfileDto { Name = "x" }))).Should()
                .ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => filerProfiles.GetAsync(profileName))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            (await filerProfiles.ListAsync()).Should().Contain(p => p.Name == profileName);

            var other = await CaseReportProfileService.CreateAsync(dbContext, fx.Seed.UserTenantB, TestLog.NoOp, new NullServiceChangeBus());
            (await other.ListAsync()).Should().NotContain(p => p.Name == profileName);
            await ((Func<Task>)(() => other.GetAsync(profileName))).Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }

        [Fact]
        public async Task BadStartsAndBadFieldsAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (filer, _, caseId) = await ArrangeAsync(dbContext);
            var report = await StartAsync(filer, caseId);

            foreach (var bad in new[]
                     {
                         new CaseReportStartDto { CaseId = 0, ProfileName = profileName, ReportCode = "A" },
                         new CaseReportStartDto { CaseId = caseId, ProfileName = "", ReportCode = "A" },
                         new CaseReportStartDto { CaseId = caseId, ProfileName = profileName, ReportCode = new string('x', 51) }
                     })
            {
                await ((Func<Task>)(() => filer.StartAsync(bad))).Should().ThrowAsync<ArgumentException>();
            }

            await ((Func<Task>)(() => filer.StartAsync(new CaseReportStartDto { CaseId = caseId, ProfileName = "nope", ReportCode = "A" })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();

            foreach (var fields in new[] { "[1]", "not json", "{\"a\":", new string('x', 200_001) })
            {
                await ((Func<Task>)(() => filer.SaveAsync(new CaseReportSaveDto { Guid = report.Guid, FieldsJson = fields })))
                    .Should().ThrowAsync<ArgumentException>();
            }
        }
    }
}
