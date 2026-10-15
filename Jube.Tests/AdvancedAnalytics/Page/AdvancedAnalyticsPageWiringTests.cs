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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Page
{
    [Trait("Category", "Service")]
    public sealed class AdvancedAnalyticsPageWiringTests
    {
        private const string Page = "Jube.App/Pages/Model/AdvancedAnalytics.cshtml";
        private const string PageModel = "Jube.App/Pages/Model/AdvancedAnalytics.cshtml.cs";
        private const string Script = "Jube.App/wwwroot/js/Model/AdvancedAnalytics.js";
        private const string Styles = "Jube.App/wwwroot/styles/advanced-analytics.css";
        private const string Layout = "Jube.App/Pages/Shared/_Layout.cshtml";

        private static string Read(string relative)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull();

            return File.ReadAllText(Path.Combine(directory!.FullName, relative));
        }

        private static string[] EndpointFiles()
        {
            return
            [
                "Jube.App/Endpoints/AdvancedAnalytics/AdvancedAnalyticsInstanceEndpoints.cs",
                "Jube.App/Endpoints/AdvancedAnalytics/AdvancedAnalyticsModelEndpoints.cs",
                "Jube.App/Endpoints/ChartsEndpoints.cs",
                "Jube.App/Endpoints/AdvancedAnalytics/AdvancedAnalyticsEndpointRunner.cs"
            ];
        }

        [Fact]
        public void ThePageLoadsItsScriptAndStylesheetAndCarriesTheGridsAndTabs()
        {
            var page = Read(Page);

            page.Should().Contain("@page");
            page.Should().Contain("/js/Model/AdvancedAnalytics.js");
            page.Should().Contain("/styles/advanced-analytics.css");

            foreach (var id in new[]
                     {
                         "InstanceGrid", "EvidenceTree",
                         "DiagramChart", "CalibrationChart", "RocChart", "RefreshInstances", "CanDelete",
                         "CanDeleteAllocated", "CanSetActive"
                     })
            {
                page.Should().Contain($"id=\"{id}\"");
            }

            page.Should().NotContain("id=\"tabstrip\"");
            page.Should().NotContain(">Models</li>");
            page.Should().NotContain(">Runs</li>");
            page.Should().NotContain("id=\"RunGrid\"");
            page.Should().NotContain("id=\"InstanceDetail\"");
            page.Should().NotContain("id=\"SlidersPlaceholder\"");
            page.Should().NotContain("id=\"ContributionChart\"");
            page.Should().NotContain("id=\"Coefficients\"");
            page.Should().NotContain("id=\"Metrics\"");
            page.Should().NotContain("id=\"Warnings\"");
            page.Should().NotContain("id=\"EvidenceSignificance\"");

            page.Should().Contain("view only");
            page.Should().Contain("junk statistics");
            page.Should().NotContain("style=");

            foreach (var removed in new[]
                     {
                         "Promote\"", "Rollback\"", "Allocate\"", "Deallocate\"", "id=\"Save\"",
                         "id=\"Name\" class=\"k-textbox"
                     })
            {
                page.Should().NotContain(removed);
            }

            page.Should().NotContain("<input type=\"text\"");
        }

        [Fact]
        public void
            ThePageModelIsGatedOnTheFourAdvancedAnalyticsPermissionsAndDeletingAnAllocatedInstanceNeedsFortySeven()
        {
            var model = Read(PageModel);

            model.Should().Contain("[Authorize]");
            model.Should().Contain("permissionValidation.Validate(");
            Regex.Replace(model, @"\s+", "").Should().Contain("45,46,47,48");
            model.Should().Contain("return Forbid();");
            var compact = Regex.Replace(model, @"\s+", "");
            compact.Should().Contain("CanDelete=permissionValidation.Validate([48]);");
            compact.Should().Contain("CanDeleteAllocated=CanDelete&&permissionValidation.Validate([47]);");
            compact.Should().Contain("CanSetActive=permissionValidation.Validate([47]);");
        }

        [Fact]
        public void TheScriptUsesOnlyTheReadRoutesTheChartRoutesTheDeleteRouteTheActiveSwitchRouteAndTheApprovalRoutes()
        {
            var script = Read(Script);

            foreach (var route in new[]
                     {
                         "/api/AdvancedAnalyticsInstance", "/api/AdvancedAnalyticsModel",
                         "/api/Chart/AnalyticsModelVersion", "/ModelDiagram/", "/Calibration/", "/Roc/",
                         "/PromotionEvidence",
                         "/versions", "/api/EntityAnalysisModel", "/active"
                     })
            {
                script.Should().Contain(route);
            }

            foreach (var forbidden in new[]
                     {
                         "/promote", "/rollback", "/allocate", "/deallocate", "/Run",
                         "/api/Invoke/AdvancedAnalytics/", "/Contribution/"
                     })
            {
                script.Should().NotContain(forbidden);
            }

            Regex.Matches(script, "call\\(\"(?<verb>[A-Z]+)\"").Select(m => m.Groups["verb"].Value).Distinct()
                .OrderBy(v => v).Should().Equal("DELETE", "GET", "POST", "PUT");

            script.Should().Contain("call(\"POST\", \"/api/EntityApproval/\"");
            script.Should().Contain("call(\"POST\", instanceEndpoint + \"/\" + instanceId + \"/\" + action");

            script.Should().NotContain("TrainLogisticRegression");
            script.Should().NotContain("eval(");
            script.Should().NotContain("new Function");
        }

        [Fact]
        public void TheScriptEscapesServerTextBeforeItBuildsMarkup()
        {
            var script = Read(Script);

            script.Should().Contain("kendo.htmlEncode");
            script.Should().Contain("esc(");
            script.Should().Contain("jc-message");
            script.Should().Contain("canDelete");
        }

        [Theory]
        [InlineData(Script)]
        public void TheScriptCarriesNoComments(string path)
        {
            var text = Read(path);
            var afterLicence = text[(text.IndexOf("*/", StringComparison.Ordinal) + 2)..];

            afterLicence.Should().NotContain("/*");

            afterLicence.Split('\n').Where(l => l.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .Should().BeEmpty();

            Regex.IsMatch(afterLicence, @"^[^""'`]*\s//\s", RegexOptions.Multiline).Should().BeFalse();
        }

        [Fact]
        public void NothingWrittenForThePageMentionsTheLibraryName()
        {
            var files = new[] { Page, PageModel, Script, Styles }.Concat(EndpointFiles());

            foreach (var path in files)
            {
                Read(path).Should().NotContainEquivalentOf("accord", path);
            }
        }

        [Fact]
        public void TheModelsMenuGroupLinksTheAnalyticsPageUnderTheFourPermissions()
        {
            var layout = Regex.Replace(Read(Layout), @"\s+", "");

            layout.Should().Contain(
                "@if(permissionValidation.Validate([45,46,47,48])){<li><ahref='/Model/AdvancedAnalytics'>AdvancedAnalytics</a></li>}");

            var models = layout.IndexOf("Models<ul>", StringComparison.Ordinal);
            var entry = layout.IndexOf("/Model/AdvancedAnalytics", StringComparison.Ordinal);
            models.Should().BeGreaterThan(0);
            entry.Should().BeGreaterThan(models);
        }

        [Fact]
        public void TheModelsGroupIsVisibleToAUserWhoHoldsOnlyAnAdvancedAnalyticsPermission()
        {
            var layout = Regex.Replace(Read(Layout), @"\s+", "");

            layout.Should().Contain(
                "5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,37,45,46,47,48,60]))");
        }

        [Fact]
        public void TheEndpointsAreRegisteredAndEveryGroupRequiresAuthorisation()
        {
            var startup = Read("Jube.App/Startup.cs");

            startup.Should().Contain("endpoints.MapAdvancedAnalyticsInstanceEndpoints();");
            startup.Should().Contain("endpoints.MapAdvancedAnalyticsModelEndpoints();");
            startup.Should().Contain("endpoints.MapChartsEndpoints();");

            foreach (var path in EndpointFiles().Take(3))
            {
                var text = Read(path);
                text.Should().Contain(".RequireAuthorization()");
                text.Should().NotContain("AllowAnonymous");
                text.Should().NotContain("TrainLogisticRegression");
                text.Should().NotContain("AdvancedAnalyticsTrainingService");
            }
        }

        [Fact]
        public void TheHttpSurfaceIsReadRoutesChartRoutesOneDeleteTheApprovalRoutesAndNothingThatMutatesElse()
        {
            var text = string.Join("\n", EndpointFiles().Select(Read));

            Regex.Matches(text, @"group\.Map(?<verb>[A-Za-z]+)\(").Select(m => m.Groups["verb"].Value)
                .Distinct().OrderBy(v => v).Should().Equal("Delete", "Get", "Post", "Put");

            Regex.Matches(text, @"group\.MapDelete\(").Should().ContainSingle();
            Regex.Matches(text, @"group\.MapPut\(").Should().ContainSingle();
            text.Should().Contain("group.MapPut(\"{id:int}/active\", SetActiveAsync)");

            Regex.Matches(text, @"group\.MapPost\(").Should().HaveCount(3);
            text.Should().Contain("group.MapPost(\"Topology/{id:int}\", TopologyWithLiveRelevanceAsync)");
            text.Should().Contain("group.MapPost(\"{id:int}/Resubmit\", ResubmitAsync)");
            text.Should().Contain("group.MapPost(\"{id:int}/Retract\", RetractAsync)");

            foreach (var removed in new[]
                     {
                         "PromoteVersionAsync", "RollbackAsync", "AllocateToModelAsync", "DeallocateAsync",
                         "service.UpdateAsync", "service.InsertAsync", "TrainLogisticRegression", "MapPatch"
                     })
            {
                text.Should().NotContain(removed);
            }

            Read(EndpointFiles()[0]).Should().Contain("group.MapDelete(\"{id:int}\", DeleteAsync)");
        }

        [Fact]
        public void TheChartEndpointsCarryTheSvgHeadersAndTheRunnerMapsTheServiceCodes()
        {
            var charts = Read(EndpointFiles()[2]);
            var runner = Read(EndpointFiles()[3]);

            charts.Should().Contain("image/svg+xml; charset=utf-8");
            charts.Should().Contain("nosniff");
            charts.Should().Contain("default-src 'none'");
            charts.Should().Contain("no-store");
            runner.Should().Contain("TypedResults.Unauthorized()");
            runner.Should().Contain("TypedResults.Forbid()");
            runner.Should().Contain("TypedResults.NotFound()");
            runner.Should().Contain("TypedResults.Conflict(");
        }

        [Fact]
        public void ThePermissionDropdownOfTheRoleScreenReadsTheSpecificationTableSoTheNewPermissionsAppear()
        {
            var script = Read("Jube.App/wwwroot/js/Administration/Frame/RoleRegistryPermission.js");

            script.Should().Contain("../api/PermissionSpecification");
            script.Should().Contain("value.id");
            script.Should().NotContain("\"AskJooby").And.NotContain("'AskJooby");

            Read("Jube.Migrations/Branches/AskJooby/AddAdvancedAnalyticsPermissions.cs").Should()
                .Contain("(48, \"Advanced Analytics\")").And.Contain("(47, \"Advanced Analytics Promote Model\")");
        }
    }
}