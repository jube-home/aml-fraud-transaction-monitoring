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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Analytics.Datasets.Models;
using Jube.Service.Repository.ExhaustiveInstanceDataset;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Service.ExhaustiveInstanceDataset
{
    [Trait("Category", "Service")]
    public sealed class ExhaustiveDatasetWiringTests
    {
        private static string Root()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull();

            return directory!.FullName;
        }

        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Root(), relative));
        }

        private static string ReadWithoutLicenceHeader(string relative)
        {
            var text = Read(relative);
            var end = text.IndexOf("*/", StringComparison.Ordinal);

            return text.StartsWith("/*", StringComparison.Ordinal) && end >= 0 ? text[(end + 2)..] : text;
        }

        [Fact]
        public void Page_LoadsTheDatasetScriptBeforeThePageScriptAndHasEveryControl()
        {
            var page = Read("Jube.App/Pages/Model/Frame/Exhaustive.cshtml");

            page.IndexOf("/js/Model/Frame/ExhaustiveDataset.js", StringComparison.Ordinal).Should()
                .BeLessThan(page.IndexOf("/js/Model/Frame/Exhaustive.js", StringComparison.Ordinal));

            foreach (var id in new[]
                     {
                         "DataSourceId", "ExhaustiveDatasetReference", "DatasetTable", "DatasetOpenButton",
                         "DatasetDeleteButton", "DatasetUploadTable", "DatasetFile", "DatasetMessage",
                         "DatasetSummary", "DatasetWarnings",
                         "DatasetColumnsGrid", "DatasetConfirmButton", "DatasetCancelButton"
                     })
            {
                page.Should().Contain($"id=\"{id}\"");
            }

            page.Should().Contain("datasetMappingRow");
            page.Should().Contain("Upload dataset");
            page.Should().Contain("accept=\".csv,.txt,.json,text/csv,text/plain,application/json\"");
            page.Should().NotContain("id=\"DatasetName\"");
            page.Should().NotContain("id=\"DatasetUploadButton\"");
            page.Should().NotContain("id=\"DatasetDependent\"");

            var script = Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveDataset.js");
            script.Should().Contain("kendoUpload").And.Contain("autoUpload: true");
        }

        [Fact]
        public void PageScript_ReadsAndWritesTheDataSourceThroughTheDatasetScript()
        {
            var script = Read("Jube.App/wwwroot/js/Model/Frame/Exhaustive.js");

            script.Should().Contain("getExhaustiveDataSource()");
            script.Should().Contain("initExhaustiveDataSource(data)");
            script.Should().Contain("initExhaustiveDataSource();");
        }

        [Fact]
        public void DatasetScript_UsesTheEndpointsKendoWidgetsAndNeverInjectsHtml()
        {
            var script = Read("Jube.App/wwwroot/js/Model/Frame/ExhaustiveDataset.js");

            script.Should().Contain("/api/ExhaustiveInstanceDataset");
            script.Should().Contain("/api/ExhaustiveInstanceCsv").And.Contain("/api/ExhaustiveInstanceJson");
            script.Should().Contain("exhaustiveInstanceCsvId").And.Contain("exhaustiveInstanceJsonId");
            script.Should().Contain("/Upload");
            script.Should().Contain("/Confirm");
            script.Should().Contain("kendoDropDownList").And.Contain("kendoGrid").And.Contain("kendoUpload");
            script.Should().NotContain(".html(");
            script.Should().NotContain("innerHTML");
            script.Should().NotContain("eval(");
            Regex.IsMatch(script, @"^\s*//", RegexOptions.Multiline).Should().BeFalse("no comments in scripts");
            script.Should().NotContain("/*");
        }

        [Fact]
        public void TheDemoEnvironmentVariableIsGoneFromCodeAndDocs()
        {
            foreach (var path in new[]
                     {
                         "Jube.DynamicEnvironment/DynamicEnvironment.cs",
                         "Jube.Engine/Exhaustive/Stages/SampleStage.cs",
                         "docs/Concepts/EnvironmentVariables/index.md"
                     })
            {
                Read(path).Should().NotContain("UseMockDataExhaustive", path);
            }

            Read("docs/AdvancedAnalytics/ExhaustiveJsonDatasets.md").Should().Contain("ExhaustiveInstanceJson");

            Read("docs/AdvancedAnalytics/ExhaustiveDataSources.md").Should()
                .Contain("UseMockDataExhaustive").And.Contain("Preservation impact");
        }

        [Fact]
        public void EnvironmentDefaults_AreFiftyMegabytesAMillionRowsAndFiveHundredColumns()
        {
            var environment = TestDynamicEnvironment.Create();

            environment.AppSettings("ExhaustiveDatasetUploadMaxBytes").Should().Be("52428800");
            environment.AppSettings("ExhaustiveDatasetUploadMaxRows").Should().Be("1000000");
            environment.AppSettings("ExhaustiveDatasetUploadMaxColumns").Should().Be("500");
            environment.AppSettings("UseMockDataExhaustive").Should().BeNull();

            var limits = DatasetLimitsFactory.FromEnvironment(environment);
            limits.MaxBytes.Should().Be(50L * 1024 * 1024);
            limits.MaxRows.Should().Be(1_000_000);
            limits.MaxColumns.Should().Be(500);
        }

        [Fact]
        public void EnvironmentOverrides_AreHonoured_AndGarbageFallsBackToDefaults()
        {
            var overridden = DatasetLimitsFactory.FromEnvironment(TestDynamicEnvironment.Create(
                new Dictionary<string, string>
                {
                    ["ExhaustiveDatasetUploadMaxBytes"] = "1000", ["ExhaustiveDatasetUploadMaxRows"] = "7",
                    ["ExhaustiveDatasetUploadMaxColumns"] = "3"
                }));
            overridden.MaxBytes.Should().Be(1000);
            overridden.MaxRows.Should().Be(7);
            overridden.MaxColumns.Should().Be(3);

            var garbage = DatasetLimitsFactory.FromEnvironment(TestDynamicEnvironment.Create(
                new Dictionary<string, string>
                {
                    ["ExhaustiveDatasetUploadMaxBytes"] = "lots", ["ExhaustiveDatasetUploadMaxRows"] = "-5",
                    ["ExhaustiveDatasetUploadMaxColumns"] = "0"
                }));

            garbage.MaxBytes.Should().Be(DatasetLimits.Default.MaxBytes);
            garbage.MaxRows.Should().Be(DatasetLimits.Default.MaxRows);
            garbage.MaxColumns.Should().Be(DatasetLimits.Default.MaxColumns);
        }

        [Fact]
        public void NewSourceFiles_CarryNoCodeComments()
        {
            var files = new[]
            {
                "Jube.Analytics/Datasets/DatasetCsvParser.cs", "Jube.Analytics/Datasets/DatasetJsonParser.cs",
                "Jube.Analytics/Datasets/DatasetMappingRules.cs", "Jube.Analytics/Datasets/Models/DatasetModels.cs",
                "Jube.Analytics/Datasets/DatasetRowJson.cs", "Jube.Analytics/Datasets/DatasetProfile.cs",
                "Jube.Analytics/Datasets/DatasetColumnNames.cs", "Jube.Analytics/Datasets/DatasetColumnAccumulator.cs",
                "Jube.Analytics/Datasets/DatasetContentSniffer.cs", "Jube.Analytics/Datasets/DatasetStreams.cs",
                "Jube.Service/Repository/ExhaustiveInstanceDataset/ExhaustiveInstanceDatasetService.cs",
                "Jube.Service/Repository/ExhaustiveInstanceDataset/ExhaustiveInstanceDatasetMapper.cs",
                "Jube.Service/Repository/ExhaustiveInstanceDataset/HashingStream.cs",
                "Jube.App/Endpoints/ExhaustiveInstanceDatasetEndpoints.cs",
                "Jube.Data/Repository/ExhaustiveInstanceDatasetRepository.cs",
                "Jube.Data/Repository/Interfaces/IDatasetStore.cs",
                "Jube.Engine/Exhaustive/Storage/DatasetTrainingPlan.cs",
                "Jube.Migrations/Branches/AskJooby/AddExhaustiveSearchInstanceDatasetTables.cs",
                "Jube.Preservation/Models/ExhaustiveInstanceCsvExport.cs",
                "Jube.Preservation/Models/ExhaustiveInstanceJsonExport.cs"
            };

            foreach (var file in files)
            {
                ReadWithoutLicenceHeader(file).Split('\n').Any(l =>
                        (l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                         && !l.Contains("ReSharper disable", StringComparison.Ordinal)
                         && !l.Contains("ReSharper restore", StringComparison.Ordinal))
                        || l.TrimStart().StartsWith("/*", StringComparison.Ordinal))
                    .Should().BeFalse(file);
            }
        }

        [Fact]
        public void Endpoints_MapTheCombinedAndBothKindRoutesWithTheJsonBodyAndEncodingChecks()
        {
            var endpoints = Read("Jube.App/Endpoints/ExhaustiveInstanceDatasetEndpoints.cs");

            endpoints.Should().Contain("/api/ExhaustiveInstanceDataset");
            endpoints.Should().Contain("\"/api/ExhaustiveInstance\" + name");

            endpoints.Should().Contain("MapKind(endpoints, DatasetKind.Csv)").And
                .Contain("MapKind(endpoints, DatasetKind.Json)");
            endpoints.Should().Contain("application/json").And.Contain("ContentEncoding");
            endpoints.Should().Contain("RequireAuthorization");
            Read("Jube.App/Startup.cs").Should().Contain("MapExhaustiveInstanceDatasetEndpoints");
        }

        [Fact]
        public void NoCodeOrDocsCarryTheRetiredDatasetNames()
        {
            foreach (var file in new[]
                     {
                         "Jube.Data/Context/DbContext.cs",
                         "Jube.Data/Repository/ExhaustiveInstanceDatasetRepository.cs",
                         "Jube.Engine/Exhaustive/Stages/SampleStage.cs", "Jube.Preservation/Preservation.cs",
                         "Jube.App/wwwroot/js/Model/Frame/ExhaustiveDataset.js",
                         "Jube.App/Pages/Model/Frame/Exhaustive.cshtml",
                         "Jube.Dto/ExhaustiveSearchInstance/ExhaustiveSearchInstanceDto.cs"
                     })
            {
                Read(file).Should().NotContain("ExhaustiveSearchInstanceDataset", file);
            }

            foreach (var file in Directory.EnumerateFiles(Path.Combine(Root(), "Jube.Data/Poco"), "*.cs"))
            {
                File.ReadAllText(file).Should().NotContain("ExhaustiveSearchInstanceDataset", file);
            }
        }

        [Fact]
        public void NewFiles_NeverUseTheForbiddenLibraryWord()
        {
            var word = string.Concat("Ac", "cord");

            foreach (var file in new[]
                     {
                         "Jube.Analytics/Datasets/DatasetJsonParser.cs", "Jube.Analytics/Datasets/DatasetProfile.cs",
                         "Jube.Data/Repository/Interfaces/IDatasetStore.cs",
                         "Jube.Data/Repository/ExhaustiveInstanceDatasetRepository.cs",
                         "Jube.Service/Repository/ExhaustiveInstanceDataset/ExhaustiveInstanceDatasetService.cs",
                         "Jube.App/Endpoints/ExhaustiveInstanceDatasetEndpoints.cs",
                         "docs/AdvancedAnalytics/ExhaustiveJsonDatasets.md"
                     })
            {
                Read(file).Should().NotContainEquivalentOf(word, file);
            }
        }
    }
}