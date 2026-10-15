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

using System.Reflection;
using FluentAssertions;
using Jube.AskJooby.Prompts.Models;
using Jube.Data.Query.GetAskJoobyStartupStatsQuery;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyOpeningInstructionTests
    {
        [Fact]
        public void WithPageContext_StillCarriesTheFullInstructionBody()
        {
            var stats = new GetAskJoobyStartupStatsQueryDto
            {
                Bites = 92980, Trails = 1, Saltlicks = 0, Joobriks = 219, Yokes = 779
            };

            var instruction = InvokeBuildOpeningInstruction(stats, hasPageContext: true);

            instruction.Should().NotContain("{instruction}",
                "a plain, non-interpolated literal referencing {instruction} would leak as literal " +
                "text instead of substituting the real prompt built above it");

            instruction.Should().Contain("brand-new conversation",
                "the original instruction body must still be present after the pageContext prefix");
        }

        [Fact]
        public void WithoutPageContext_ReturnsTheInstructionBodyUnprefixed()
        {
            var stats = new GetAskJoobyStartupStatsQueryDto
            {
                Bites = 92980, Trails = 1, Saltlicks = 0, Joobriks = 219, Yokes = 779
            };

            var instruction = InvokeBuildOpeningInstruction(stats, hasPageContext: false);

            instruction.Should().NotContain("{instruction}");
            instruction.Should().Contain("brand-new conversation");
        }

        [Fact]
        public void ParsePageContext_WithJsonPageNameStateAndRecord_ResolvesCatalogueTextAndRecordSummary()
        {
            var pageContext = InvokeParsePageContext(
                """{"pageName":"Case","state":"edit","record":{"id":42,"caseKey":"AccountNumber"}}""");

            pageContext.Should().NotBeNull();
            pageContext!.PageName.Should().Be("Case");
            pageContext.State.Should().Be("edit");
            pageContext.Purpose.Should().NotBeNullOrWhiteSpace(
                "Case is in PagePurposeCatalogue, so its authored purpose should resolve");

            pageContext.LikelyAsks.Should().NotBeNullOrWhiteSpace();
            pageContext.RecordSummary.Should().Contain("42").And.Contain("AccountNumber");
        }

        [Fact]
        public void ParsePageContext_WithPageNameNotInTheCatalogue_StillReturnsPageNameWithoutPurposeText()
        {
            var pageContext = InvokeParsePageContext("""{"pageName":"SomeBrandNewPage","state":"readonly"}""");

            pageContext.Should().NotBeNull();
            pageContext!.PageName.Should().Be("SomeBrandNewPage");
            pageContext.Purpose.Should().BeNull();
            pageContext.LikelyAsks.Should().BeNull();
        }

        [Fact]
        public void ParsePageContext_WithNonJsonText_FallsBackToTreatingItAsThePageName()
        {
            var pageContext = InvokeParsePageContext("Administration > Rules");

            pageContext.Should().NotBeNull();
            pageContext!.PageName.Should().Be("Administration > Rules");
        }

        [Fact]
        public void ParsePageContext_WithNullOrWhitespace_ReturnsNull()
        {
            InvokeParsePageContext(null).Should().BeNull();
            InvokeParsePageContext("   ").Should().BeNull();
        }

        private static string InvokeBuildOpeningInstruction(GetAskJoobyStartupStatsQueryDto stats,
            bool hasPageContext)
        {
            var method = typeof(AskJoobyConversationService).GetMethod(
                "BuildOpeningInstruction", BindingFlags.NonPublic | BindingFlags.Static)!;

            return (string)method.Invoke(null, [stats, hasPageContext])!;
        }

        private static PageContext? InvokeParsePageContext(string? raw)
        {
            var method = typeof(AskJoobyConversationService).GetMethod(
                "ParsePageContext", BindingFlags.NonPublic | BindingFlags.Static)!;

            return (PageContext?)method.Invoke(null, [raw]);
        }
    }
}