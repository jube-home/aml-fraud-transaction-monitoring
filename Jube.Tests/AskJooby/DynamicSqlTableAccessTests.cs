/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using FluentAssertions;
using Jube.Data.Reporting;
using Jube.Service.DynamicSql;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class DynamicSqlTableAccessTests
    {
        [Fact]
        public void ScopedSqlSurvivesTheValidatorTheAgentPathActuallyRunsItThrough()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true);

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                "SELECT \"Name\" FROM \"EntityAnalysisModelRequestXpath\"", allowed);

            rewritten.Should().Contain("UserInTenant");

            var act = () => PostgresSqlValidator.AssertSelectOnly(rewritten, false);

            act.Should().NotThrow(
                "the agent path rewrites before it executes, so the scoping the rewriter itself "
                + "injected must not be read as the caller reaching for a protected table");
        }

        [Fact]
        public void TheProtectedRelationGateStillBlocksScopedSqlAndIsWhyTheAgentPathMustNotUseIt()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true);

            var rewritten = PostgresSqlValidator.RewriteTableReferences(
                "SELECT \"Name\" FROM \"EntityAnalysisModelRequestXpath\"", allowed);

            var act = () => PostgresSqlValidator.AssertSelectOnly(rewritten, true);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*protected table*",
                    "this gate exists for user-authored visualisation SQL, which has no allow-list; "
                    + "applied to already-scoped agent SQL it rejects the scoping as though the "
                    + "caller had written it");
        }

        [Fact]
        public void TheCredentialColumnsAreProjectedAwaySoReachingTheTableCannotReachTheSecret()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true);

            allowed["UserRegistry"].Should().NotContain("\"Password\"")
                .And.Contain("\"Name\"", "the row stays queryable, only the secret leaves the projection");

            allowed["UserRegistryVersion"].Should().NotContain("\"Password\"");

            allowed["UserRegistryApiKey"].Should().NotContain("\"ApiKey\"")
                .And.Contain("\"ApiKeyDisplay\"", "the masked value is not the key and stays readable");
        }

        [Fact]
        public void TheRawGateRefusesCredentialTablesEvenThoughTheAllowListRegistersThem()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("Administrator", _ => true);

            allowed.Should().ContainKey("UserRegistry",
                "the allow-list registers it, so the allow-list alone cannot be what protects it");

            var act = () => PostgresSqlValidator.AssertSelectOnly(
                "SELECT \"Password\" FROM \"UserRegistry\"", true);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*protected table*",
                    "the agent path must gate the caller's own SQL before rewriting, because after "
                    + "rewriting the injected scoping is indistinguishable from the caller naming "
                    + "these tables deliberately");
        }

        [Fact]
        public void TheRawGateStillAdmitsAnOrdinaryTableTheCallerIsEntitledTo()
        {
            var act = () => PostgresSqlValidator.AssertSelectOnly(
                "SELECT \"Name\" FROM \"EntityAnalysisModelRequestXpath\"", true);

            act.Should().NotThrow();
        }

        [Fact]
        public void BuildAllowedTablesIncludesAKnownRegisteredTableWhenPermissionGranted()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true);

            allowed.Should().ContainKey("EntityAnalysisModel");
        }

        [Fact]
        public void BuildAllowedTablesIncludesCaseFamilyTablesWhenPermission1IsGranted()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", p => p.Contains(1));

            allowed.Should().ContainKey("Case")
                .And.ContainKey("CaseNote")
                .And.ContainKey("CaseFile")
                .And.ContainKey("CaseEvent");
        }

        [Fact]
        public void BuildAllowedTablesIncludesCaseCreationObservabilityTablesWhenPermission27IsGranted()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", p => p.Contains(27));

            allowed.Should().ContainKey("CaseCreationWarning");

            DynamicSqlTableAccess.RegisteredTables.Should().NotContain("CaseCreationStagePerformanceCounter",
                "the stage performance counter has no tenant column, so it cannot be scoped and is not " +
                "registered at all");

            allowed.Should().NotContainKey("Case",
                "permission 27 is observability, not permission 1 (Read Write Case) -- it must not also " +
                "unlock the Case content tables themselves");
        }

        [Fact]
        public void BuildAllowedTablesOmitsCaseFamilyTablesWhenNeitherPermissionIsGranted()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => false);

            allowed.Keys.Should().NotContain(k => k.StartsWith("Case", StringComparison.Ordinal));
        }

        [Fact]
        public void BuildAllowedTablesLooksUpByTableNameCaseInsensitively()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true);

            allowed.Should().ContainKey("entityanalysismodel");
            allowed.Should().ContainKey("ENTITYANALYSISMODEL");
        }

        [Fact]
        public void BuildAllowedTablesAddsARoleExistsCheckOnTopOfTenantForARoleGatedTable()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true);

            allowed["EntityAnalysisModel"].Should().Contain("EntityAnalysisModelRole",
                "a table sitting under EntityAnalysisModelRole must be filtered by it, not just by " +
                "tenant membership -- see CompileSql.cs's own CaseWorkflowRole/CaseWorkflowStatusRole " +
                "pattern this mirrors");
        }

        [Fact]
        public void BuildAllowedTablesOmitsEverythingWhenPermissionDenied()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => false);

            allowed.Should().BeEmpty(
                "a table the caller lacks permission for must be absent, not present-but-unfiltered");
        }

        [Fact]
        public void BuildAllowedTablesThrowsForAnEmptyUsername()
        {
            var act = () => DynamicSqlTableAccess.BuildAllowedTables("", _ => true);

            act.Should().Throw<InvalidOperationException>(
                "there is no identity to resolve tenant/role access against, so nothing should become queryable");
        }

        [Fact]
        public void BuildAllowedTablesThrowsForANullUsername()
        {
            var act = () => DynamicSqlTableAccess.BuildAllowedTables(null!, _ => true);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ThePointerCacheIsNotQueryableWithoutAConversationToBindItTo()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true);

            allowed.Should().NotContainKey("AskJoobyDataCache",
                "there is no conversation to scope the pointer cache to, so it must read as unregistered");
        }

        [Fact]
        public void ThePointerCacheIsBoundToTheCallersOwnConversation()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true, 18);

            allowed["AskJoobyDataCache"].Should().Contain("\"AskJoobyConversationId\" = 18",
                "a pointer belongs to the conversation that made it, not to the whole tenant");
        }

        [Fact]
        public void ThePointerCacheHonoursTheSameExpiryTheToolPathEnforces()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true, 18);

            allowed["AskJoobyDataCache"].Should().Contain("\"ExpiresAt\"",
                "reading a pointer through SQL must not outlive reading it through the tool");
        }

        [Fact]
        public void TheConversationIdIsSplicedIntoExactlyTheConversationOwnedTables()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true, 18);

            var conversationBound = allowed
                .Where(entry => entry.Value is not null && entry.Value.Contains("\" = 18"))
                .Select(entry => entry.Key);

            conversationBound.Should().BeEquivalentTo(
            [
                "AskJoobyDataCache", "AskJoobyConversation", "AskJoobyMessage", "AskJoobyFile", "AskJoobyRun",
                "AskJoobyToolCall", "AskJoobyModelCall", "AskJoobyEmbeddingCall", "AskJoobyRerankCall",
                "AskJoobyWebSearchCall", "AskJoobyOpenCorporatesCall", "AskJoobyTurnCost", "AskJoobyCompaction",
                "AskJoobyRouting", "AskJoobyWorkflowStep", "AskJoobyMemory", "DynamicSqlQueryLog"
            ], "conversation-owned rows are visible only inside their own conversation, and no other table " +
               "may narrow on the conversation silently");
        }

        [Fact]
        public void NoTemplateIsLeftHoldingAnUnresolvedToken()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables("alice", _ => true, 18);

            allowed.Values.Where(_ => true)
                .Should().OnlyContain(template => !template.Contains("{{"),
                    "an unresolved token would reach Postgres as a syntax error at best");
        }
    }
}