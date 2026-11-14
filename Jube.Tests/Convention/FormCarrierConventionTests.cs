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
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Convention
{
    [Trait("Category", "Convention")]
    public sealed class FormCarrierConventionTests
    {
        private static readonly string[] FileCarrierMarkers =
        [
            "IFormFile", "IFormCollection", "ReadFormAsync", "HasFormContentType", "MultipartReader"
        ];

        private static readonly Dictionary<string, string> AllowedFileOnlyCarriers = new(StringComparer.Ordinal)
        {
            ["Endpoints/AskJoobyFileEndpoints.cs"] =
                "a conversation attachment is a binary file; the conversation is named by the route and the form carries only the file",
            ["Endpoints/ExhaustiveInstanceDatasetEndpoints.cs"] =
                "a CSV or JSON dataset can run to the dataset size limit and is streamed through a multipart reader without buffering; the form carries only the file",
            ["Endpoints/InvokeEndpoints.cs"] =
                "a batch of records for an Exhaustive search instance is streamed through a multipart reader; the instance is named by the route and the form carries only the file",
            ["Endpoints/EntityAnalysisModelListValueEndpoints.cs"] =
                "a CSV of list values is a binary file that can be large; the list is named by the route and the form carries only the file",
            ["Endpoints/EntityAnalysisModelDictionaryKvpEndpoints.cs"] =
                "a CSV of dictionary key value pairs is a binary file that can be large; the dictionary is named by the route and the form carries only the file",
            ["Endpoints/Repository/SanctionEntrySourceEndpoints.cs"] =
                "a sanctions list file can be tens of megabytes and is read as a stream; the source is named by the route and the form carries only the file",
            ["Wire/WireEndpointCatalogue.cs"] =
                "the wire catalogue skips file parameter types so they are never mistaken for a serialised payload",
            ["Wire/WireCatalogueServiceGuess.cs"] =
                "the wire catalogue skips file parameter types when guessing which parameters are services"
        };

        private static string AppDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.App", "Jube.App.csproj")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory.Required().FullName, "Jube.App");
        }

        private static List<(string Relative, string Text)> AppSources()
        {
            var root = AppDirectory();
            var separator = Path.DirectorySeparatorChar;

            return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"))
                .Select(p => (Path.GetRelativePath(root, p).Replace(separator, '/'), File.ReadAllText(p)))
                .ToList();
        }

        [Fact]
        public void NothingInTheAppBindsAnEndpointParameterFromAForm()
        {
            var offenders = AppSources()
                .Where(source => source.Text.Contains("[FromForm", StringComparison.Ordinal))
                .Select(source => source.Relative)
                .ToList();

            offenders.Should().BeEmpty(
                "data reaches an endpoint through the serialisation pipeline as a JSON or MessagePack body, a route segment or, hardly ever, a query string; a form never carries a scalar");
        }

        [Fact]
        public void EveryUseOfAFormOrFileCarrierIsOnTheAllowListOfFileOnlyEndpoints()
        {
            var offenders = AppSources()
                .Where(source => FileCarrierMarkers.Any(marker => source.Text.Contains(marker, StringComparison.Ordinal)))
                .Select(source => source.Relative)
                .Where(relative => !AllowedFileOnlyCarriers.ContainsKey(relative))
                .ToList();

            offenders.Should().BeEmpty(
                "a binary file may travel in a file-only multipart body, but only on an endpoint named on the allow list with a reason");
        }

        [Fact]
        public void EveryAllowListEntryStillUsesAFormOrFileCarrierAndStatesWhy()
        {
            var sources = AppSources().ToDictionary(source => source.Relative, source => source.Text);

            var stale = AllowedFileOnlyCarriers.Keys
                .Where(relative => !sources.TryGetValue(relative, out var text)
                                   || !FileCarrierMarkers.Any(marker => text.Contains(marker, StringComparison.Ordinal)))
                .ToList();

            stale.Should().BeEmpty("an allow list entry for a file that no longer uses a form or file carrier must be removed");
            AllowedFileOnlyCarriers.Values.Should().OnlyContain(reason => !string.IsNullOrWhiteSpace(reason));
        }

        [Fact]
        public void NoAllowListedEndpointReadsAScalarFromTheForm()
        {
            var scalarReads = new[] { "form.TryGetValue", "Form[\"", "Form.TryGetValue", ".Form.Keys", "form[\"" };

            var offenders = AppSources()
                .Where(source => AllowedFileOnlyCarriers.ContainsKey(source.Relative))
                .Where(source => scalarReads.Any(read => source.Text.Contains(read, StringComparison.Ordinal)))
                .Select(source => source.Relative)
                .ToList();

            offenders.Should().BeEmpty("a file-only multipart body carries no scalar field");
        }
    }
}
