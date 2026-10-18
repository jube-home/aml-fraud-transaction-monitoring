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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Tools.Export;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ExportCachedTableToolTests
    {
        [Fact]
        public async Task ExportCachedTableToCsvWritesRealCsvBytesAndRecordsTheFileAsync()
        {
            var table = new CachedTable(
                [new CachedColumn("Name", "String"), new CachedColumn("Count", "Double")],
                [
                    new Dictionary<string, object?>
                        { ["Name"] = "Detailed Account Financial Transactions", ["Count"] = 4.0 }
                ]);

            var cache = new FakeToolResultCache("tbl_test", table);
            var exportClient = new FakeFileExportClient(42);
            var sink = new ExportSink();
            var tool = new ExportCachedTableTool(cache, exportClient, sink);

            var result = await tool.ExportCachedTableToCsv("tbl_test", "activation rules");

            result.Should().Contain("1 row(s)");
            result.Should().Contain("activation rules.csv");
            exportClient.LastFileName.Should().Be("activation rules.csv");
            exportClient.LastContentType.Should().Be("text/csv");
            var csvText = Encoding.UTF8.GetString(exportClient.LastContent!).TrimStart('﻿');
            csvText.Should().Contain("Name,Count");
            csvText.Should().Contain("Detailed Account Financial Transactions,4");
            sink.Files.Should().ContainSingle(f => f.Id == 42 && f.Name == "activation rules.csv");
        }

        [Fact]
        public async Task ExportCachedTableToCsvQuotesFieldsContainingCommasAndQuotesAsync()
        {
            var table = new CachedTable(
                [new CachedColumn("Label", "String")],
                [new Dictionary<string, object?> { ["Label"] = "contains, a comma and \"quotes\"" }]);

            var cache = new FakeToolResultCache("tbl_test", table);
            var exportClient = new FakeFileExportClient(1);
            var tool = new ExportCachedTableTool(cache, exportClient, new ExportSink());

            await tool.ExportCachedTableToCsv("tbl_test", "quoting");

            var csvText = Encoding.UTF8.GetString(exportClient.LastContent!).TrimStart('﻿');
            csvText.Should().Contain("\"contains, a comma and \"\"quotes\"\"\"");
        }

        [Fact]
        public async Task ExportCachedTableToCsvReturnsAFriendlyMessageForAnUnknownPointerAsync()
        {
            var cache = new FakeToolResultCache("tbl_missing", null);
            var exportClient = new FakeFileExportClient(1);
            var sink = new ExportSink();
            var tool = new ExportCachedTableTool(cache, exportClient, sink);

            var result = await tool.ExportCachedTableToCsv("tbl_missing", "whatever");

            result.Should().Contain("No cached table matches that pointer");
            sink.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task ExportCachedTableToCsvRefusesAnEmptyTableAsync()
        {
            var table = new CachedTable([new CachedColumn("Id", "Int32")], []);
            var cache = new FakeToolResultCache("tbl_empty", table);
            var exportClient = new FakeFileExportClient(1);
            var sink = new ExportSink();
            var tool = new ExportCachedTableTool(cache, exportClient, sink);

            var result = await tool.ExportCachedTableToCsv("tbl_empty", "whatever");

            result.Should().Contain("no rows");
            sink.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task ExportCachedTableToCsvSanitizesAnUnsafeFileNameAsync()
        {
            var table = new CachedTable(
                [new CachedColumn("Id", "Int32")], [new Dictionary<string, object?> { ["Id"] = 1 }]);

            var cache = new FakeToolResultCache("tbl_test", table);
            var exportClient = new FakeFileExportClient(1);
            var tool = new ExportCachedTableTool(cache, exportClient, new ExportSink());

            await tool.ExportCachedTableToCsv("tbl_test", "../../etc/passwd");

            exportClient.LastFileName.Should().NotContain("/");
            exportClient.LastFileName.Should().EndWith(".csv");
        }

        private sealed class FakeToolResultCache(string pointer, CachedTable? table) : IToolResultCache
        {
            public Task<string> StoreAsync(CachedTable storedTable, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                return Task.FromResult(pointer);
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(pointerKey == pointer ? table : null);
            }
        }

        private sealed class FakeFileExportClient(int fileId) : IFileExportClient
        {
            public byte[]? LastContent { get; private set; }
            public string? LastFileName { get; private set; }
            public string? LastContentType { get; private set; }

            public Task<int> ExportAsync(byte[] content, string fileName, string contentType,
                CancellationToken token = default)
            {
                LastContent = content;
                LastFileName = fileName;
                LastContentType = contentType;

                return Task.FromResult(fileId);
            }
        }
    }
}