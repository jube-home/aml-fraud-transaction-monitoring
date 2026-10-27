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

using FluentAssertions;
using Jube.AskJooby.Agents;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AgentToolNarrationTests
    {
        [Theory]
        [InlineData("PostgresActivityList", "Listing Postgres activity")]
        [InlineData("EntityAnalysisModelTagCreate", "Creating an entity analysis model tag")]
        [InlineData("AskJoobyAnalyticsInstanceValidate", "Validating an Ask Jooby analytics instance")]
        [InlineData("EntityAnalysisModelDelete", "Deleting an entity analysis model")]
        [InlineData("EntityAnalysisModelCount", "Counting entity analysis model")]
        [InlineData("EntityAnalysisModelFilterFields", "Filtering entity analysis model fields")]
        [InlineData("CaseWorkflowMacroExecutionExecute", "Running a case workflow macro execution")]
        public void DescribeStartingTurnsACataloguedNameIntoAParticiplePhrase(string toolName, string expected)
        {
            AgentToolNarration.DescribeStarting(toolName).Should().Be(expected);
        }

        [Theory]
        [InlineData("PostgresActivityList", "Listed Postgres activity")]
        [InlineData("EntityAnalysisModelTagCreate", "Created an entity analysis model tag")]
        [InlineData("CaseWorkflowMacroExecutionExecute", "Ran a case workflow macro execution")]
        [InlineData("EntityAnalysisModelGatewayRuleResetCounter",
            "Reset an entity analysis model gateway rule counter")]
        public void DescribeFinishedTurnsACataloguedNameIntoAPastTensePhrase(string toolName, string expected)
        {
            AgentToolNarration.DescribeFinished(toolName).Should().Be(expected);
        }

        [Theory]
        [InlineData("EntityAnalysisModelRequestXPathList", "Listing entity analysis model request XPath")]
        [InlineData("EntityAnalysisModelTtlCounterList", "Listing entity analysis model TTL counter")]
        [InlineData("TreeChildrenTTLCounterGet", "Fetching a tree children TTL counter")]
        [InlineData("HAProxyReachabilityProbeList", "Listing HAProxy reachability probe")]
        [InlineData("OtlpDispatchCounterList", "Listing OTLP dispatch counter")]
        [InlineData("OpenTelemetryMetricList", "Listing OpenTelemetry metric")]
        [InlineData("DotNetRuntimeMetricList", "Listing .NET runtime metric")]
        [InlineData("SessionCaseSearchCompiledSqlGetLast", "Fetching a session case search compiled SQL last")]
        [InlineData("EntityAnalysisModelListValueUploadCsv", "Uploading an entity analysis model list value CSV")]
        [InlineData("EntityAnalysisModelDictionaryKvpList", "Listing entity analysis model dictionary KVP")]
        [InlineData("EntityAnalysisModelHttpAdaptationList", "Listing entity analysis model HTTP adaptation")]
        [InlineData("WafAttackList", "Listing WAF attack")]
        [InlineData("AskJoobyConversationList", "Listing Ask Jooby conversation")]
        [InlineData("EntityAnalysisModelGatewayRuleBuildRuleFromBuilderJson",
            "Building an entity analysis model gateway rule from builder JSON")]
        public void DescribeStartingNeverMangesAnAcronymOrAProductName(string toolName, string expected)
        {
            AgentToolNarration.DescribeStarting(toolName).Should().Be(expected);
        }

        [Theory]
        [InlineData("EntityAnalysisModelTagGetByEntityAnalysisModelId",
            "Fetching an entity analysis model tag by entity analysis model ID")]
        [InlineData("EntityAnalysisModelListValueGetByEntityAnalysisModelListId",
            "Fetching an entity analysis model list value by entity analysis model list ID")]
        [InlineData("CaseWorkflowFilterRoleListByCaseWorkflowFilterGuid",
            "Listing case workflow filter role by case workflow filter GUID")]
        [InlineData("CaseByIdGet", "Fetching a case by ID")]
        public void DescribeStartingTakesTheOperationFromBeforeTheQualifierRatherThanInsideIt(
            string toolName, string expected)
        {
            AgentToolNarration.DescribeStarting(toolName).Should().Be(expected);
        }

        [Theory]
        [InlineData("EntityAnalysisModelGatewayRuleParseRule", "Parsing an entity analysis model gateway rule")]
        [InlineData("UserInTenantSwitchTenant", "Switching a user in tenant")]
        public void DescribeStartingCollapsesAWordTheOperationSuffixRepeats(string toolName, string expected)
        {
            AgentToolNarration.DescribeStarting(toolName).Should().Be(expected);
        }

        [Theory]
        [InlineData("AskJoobyAnalyticsModelCompareVersions", "Comparing Ask Jooby analytics model versions")]
        [InlineData("AskJoobyConversationGetMessages", "Fetching Ask Jooby conversation messages")]
        [InlineData("IconsGet", "Fetching icons")]
        [InlineData("CaseWorkflowStatusGet", "Fetching a case workflow status")]
        [InlineData("UserRegistryGet", "Fetching a user registry")]
        [InlineData("EntityAnalysisModelGet", "Fetching an entity analysis model")]
        public void DescribeStartingOnlyArticlesASingularSubjectAndPicksTheRightArticle(
            string toolName, string expected)
        {
            AgentToolNarration.DescribeStarting(toolName).Should().Be(expected);
        }

        [Theory]
        [InlineData("EntityAnalysisModelIntegrityGraph")]
        [InlineData("EntityAnalysisModelTimeWindowIntervals")]
        [InlineData("EntityAnalysisModelInvocationContextBlank")]
        [InlineData("EntityAnalysisModelInvocationContextFromArchive")]
        [InlineData("SomeOtherTool")]
        [InlineData("")]
        public void DescribeReturnsNothingWhenNoOperationIsRecognisedSoTheCallerKeepsTheRawName(string toolName)
        {
            (AgentToolNarration.DescribeStarting(toolName) is null).Should().BeTrue();
            (AgentToolNarration.DescribeFinished(toolName) is null).Should().BeTrue();
        }
    }
}