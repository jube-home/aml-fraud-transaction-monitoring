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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Analytics.Datasets.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Dto.ExhaustiveInstanceDataset;
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Engine.Exhaustive;
using Jube.Engine.Helpers;
using Jube.Service.Exceptions.ExhaustiveInstanceDataset;
using Jube.Service.Exceptions.ExhaustiveSearchInstance;
using Jube.Service.Reactivity;
using Jube.Service.Repository.ExhaustiveInstanceDataset;
using Jube.Service.Repository.ExhaustiveSearchInstance;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Test.Service.ExhaustiveInstanceDataset
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveInstanceJsonServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private readonly List<int> createdCsvIds = [];
        private readonly List<int> createdInstanceIds = [];
        private readonly List<int> createdJsonIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var db = fx.GetDbContext();

            if (createdInstanceIds.Count > 0)
            {
                await db.ExhaustiveSearchInstance.Where(w => createdInstanceIds.Contains(w.Id))
                    .Set(s => s.ExhaustiveInstanceCsvId, (int?)null).Set(s => s.ExhaustiveInstanceJsonId, (int?)null)
                    .Set(s => s.DataSourceId, (byte?)0).UpdateAsync();
            }

            foreach (var id in createdJsonIds)
            {
                await db.ExhaustiveInstanceJsonRow.Where(w => w.DatasetId == id).DeleteAsync();
                await db.ExhaustiveInstanceJson.Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var id in createdCsvIds)
            {
                await db.ExhaustiveInstanceCsvRow.Where(w => w.DatasetId == id).DeleteAsync();
                await db.ExhaustiveInstanceCsv.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static string Name(string label)
        {
            return $"{DatabaseFixture.Prefix}Js{label}{Guid.NewGuid():N}"[..40];
        }

        private static Stream Body(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        private static string SmallJson()
        {
            return "[" + string.Join(",", Enumerable.Range(0, 30).Select(i =>
                       $"{{\"Amount\":{i * 3 + 1},\"Country\":\"{(i % 2 == 0 ? "SE" : "NO")}\",\"Score\":{i * 7 % 11},\"Fraud\":{(i % 3 == 0 ? 1 : 0)}}}")) +
                   "]";
        }

        private static string SmallCsv()
        {
            var builder = new StringBuilder("Amount,Country,Score,Fraud\n");

            for (var i = 0; i < 30; i++)
            {
                builder.Append(i * 3 + 1).Append(',').Append(i % 2 == 0 ? "SE" : "NO").Append(',')
                    .Append(i * 7 % 11).Append(',').Append(i % 3 == 0 ? 1 : 0).Append('\n');
            }

            return builder.ToString();
        }

        private static Task<ExhaustiveInstanceDatasetService> BuildAsync(DbContext db, string? user,
            DatasetLimits? limits = null)
        {
            return ExhaustiveInstanceDatasetService.CreateAsync(db, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), limits ?? DatasetLimits.Default, TestLog.NoOp);
        }

        private async Task<ExhaustiveInstanceDatasetDto> UploadJsonAsync(DbContext db, string user,
            string? json = null, string? name = null, string? label = "unit test")
        {
            var service = await BuildAsync(db, user);

            var dataset = await service.UploadJsonAsync(name ?? Name("Up"), label, "application/json",
                Body(json ?? SmallJson()));
            createdJsonIds.Add(dataset.Id);

            return dataset;
        }

        private static ExhaustiveInstanceDatasetMappingDto ValidMapping(ExhaustiveInstanceDatasetDto uploaded)
        {
            return new ExhaustiveInstanceDatasetMappingDto
            {
                Id = uploaded.Id,
                Columns =
                [
                    .. uploaded.Columns.Select(c => new ExhaustiveInstanceDatasetMappingColumnDto
                    {
                        Index = c.Index, Include = c.Header is "Amount" or "Score", Name = c.Name,
                        ProcessingTypeId = c.Header == "Score" ? 1 : 5
                    })
                ]
            };
        }

        private static void ApplyDatasetFilter(ExhaustiveSearchInstanceDto dto, int columnIndex, string value)
        {
            dto.Filter = true;
            dto.FilterJson = "{}";
            dto.FilterSql = "(\"Json\"->'values'->>'c" + columnIndex + "') = @1";
            dto.FilterTokens = "[\"" + value + "\"]";
        }

        private async Task<int> ParentModelAsync(DbContext db, string user)
        {
            var saved = await new EntityAnalysisModelRepository(db, user).InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                Locked = 0, Deleted = 0
            });

            return saved.Id;
        }

        private static Task<ExhaustiveSearchInstanceService> InstanceServiceAsync(DbContext db, string user)
        {
            return ExhaustiveSearchInstanceService.CreateAsync(db, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private async Task<int> TenantAsync(DbContext db, string user)
        {
            return (await UserInTenantRepository.GetTenantRegistryIdAsync(db, user, CancellationToken.None))!.Value;
        }

        [Fact]
        public async Task UploadJson_StoresAPendingJsonDatasetWithColumnsRowsHashAndLabelAsync()
        {
            await using var db = fx.GetDbContext();
            var json = SmallJson();

            var dataset = await UploadJsonAsync(db, fx.Seed.UserWithPermission, json, label: "agent tool result");

            dataset.Kind.Should().Be("Json");
            dataset.StatusId.Should().Be(0);
            dataset.SourceLabel.Should().Be("agent tool result");
            dataset.FileName.Should().BeNull();
            dataset.Delimiter.Should().BeNull();

            dataset.ContentHash.Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))
                .ToLowerInvariant());

            dataset.RowCount.Should().Be(30);
            dataset.SizeBytes.Should().Be(Encoding.UTF8.GetByteCount(json));
            dataset.ColumnCount.Should().Be(4);
            dataset.Warnings.Should().Contain(w => w.Contains("Country") && w.Contains("not numeric"));
            dataset.CreatedUser.Should().Be(fx.Seed.UserWithPermission);

            (await db.ExhaustiveInstanceJsonRow.CountAsync(w => w.DatasetId == dataset.Id)).Should().Be(30);
            var row = await db.ExhaustiveInstanceJsonRow.Where(w => w.DatasetId == dataset.Id).FirstAsync();

            JsonDocument.Parse(row.Json).RootElement.GetProperty("values").EnumerateObject()
                .Select(p => p.Name).Should().Equal("c0", "c1", "c2", "c3");

            (await db.ExhaustiveInstanceCsvRow.CountAsync(w => w.DatasetId == dataset.Id
                                                               && w.Json == row.Json)).Should().Be(0);
        }

        [Fact]
        public async Task UploadJson_ColumnarShapeAndStringOverload_AreAcceptedAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var dataset = await service.UploadJsonAsync(Name("Col"), "columnar",
                "{\"columns\":[\"a\",\"b\"],\"rows\":[[1,0],[2,1],[3,0]]}");
            createdJsonIds.Add(dataset.Id);

            dataset.Kind.Should().Be("Json");
            dataset.RowCount.Should().Be(3);
            dataset.Columns.Select(c => c.Header).Should().Equal("a", "b");
        }

        [Fact]
        public async Task UploadJson_HasTheSameRowsAndColumnsAsTheSameDataUploadedAsCsvAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var csv = await service.UploadCsvAsync(Name("Same"), "same.csv", "text/csv", Body(SmallCsv()));
            createdCsvIds.Add(csv.Id);
            var json = await UploadJsonAsync(db, fx.Seed.UserWithPermission);

            JsonSerializer.Serialize(json.Columns).Should()
                .Be(JsonSerializer.Serialize(csv.Columns));

            json.Warnings.Should().Equal(csv.Warnings);
            json.RowCount.Should().Be(csv.RowCount);

            (await db.ExhaustiveInstanceCsvRow.Where(w => w.DatasetId == csv.Id).Select(s => s.Json)
                    .OrderBy(o => o).ToListAsync()).Should()
                .Equal(await db.ExhaustiveInstanceJsonRow.Where(w => w.DatasetId == json.Id).Select(s => s.Json)
                    .OrderBy(o => o).ToListAsync());
        }

        [Theory]
        [InlineData("[{\"a\":{\"b\":1}}]")]
        [InlineData("[{\"a\":1,\"a\":2}]")]
        [InlineData("[{\"a\":1e400}]")]
        [InlineData("[{\"a\":1},]")]
        [InlineData("[]")]
        [InlineData("5")]
        [InlineData("[{\"a\":1}] trailing")]
        [InlineData("{\"columns\":[\"a\",\"a\"],\"rows\":[[1,2]]}")]
        [InlineData("a,b\n1,2\n")]
        public async Task UploadJson_RejectedBody_StoresNothingAsync(string body)
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var datasetsBefore = await db.ExhaustiveInstanceJson.CountAsync();
            var rowsBefore = await db.ExhaustiveInstanceJsonRow.CountAsync();

            var act = async () => await service.UploadJsonAsync(Name("Bad"), "x", "application/json", Body(body));

            var thrown = (await act.Should().ThrowAsync<DatasetRejectedException>()).Which;
            thrown.Report.Errors.Should().NotBeEmpty();
            (await db.ExhaustiveInstanceJson.CountAsync()).Should().Be(datasetsBefore);
            (await db.ExhaustiveInstanceJsonRow.CountAsync()).Should().Be(rowsBefore);
        }

        [Fact]
        public async Task UploadJson_ErrorLateInTheBody_RollsBackTheRowsAlreadyWrittenAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var rowsBefore = await db.ExhaustiveInstanceJsonRow.CountAsync();

            var body = "[" + string.Join(",", Enumerable.Range(0, 3000).Select(i => $"{{\"a\":{i}}}"))
                           + ",{\"a\":{\"nested\":1}}]";

            var act = async () => await service.UploadJsonAsync(Name("Late"), "x", "application/json", Body(body));

            await act.Should().ThrowAsync<DatasetRejectedException>();
            (await db.ExhaustiveInstanceJsonRow.CountAsync()).Should().Be(rowsBefore);
        }

        [Fact]
        public async Task UploadJson_CapsFromTheServiceLimitsAreEnforcedAsync()
        {
            await using var db = fx.GetDbContext();

            var tight = await BuildAsync(db, fx.Seed.UserWithPermission,
                DatasetLimits.Default with { MaxRows = 5, MaxColumns = 3, MaxBytes = 400 });

            foreach (var (body, code) in new[]
                     {
                         (SmallJson(), "TooLarge"),
                         ("[{\"a\":1},{\"a\":2},{\"a\":3},{\"a\":4},{\"a\":5},{\"a\":6}]", "TooManyRows"),
                         ("[{\"a\":1,\"b\":2,\"c\":3,\"d\":4}]", "TooManyColumns")
                     })
            {
                var act = async () => await tight.UploadJsonAsync(Name("Cap"), "x", "application/json", Body(body));

                (await act.Should().ThrowAsync<DatasetRejectedException>()).Which.Report.Errors
                    .Should().Contain(e => e.Code == code);
            }
        }

        [Fact]
        public async Task UploadJson_StringOverload_RejectsEmptyOversizeAndLoneSurrogatesAsync()
        {
            await using var db = fx.GetDbContext();

            var service = await BuildAsync(db, fx.Seed.UserWithPermission,
                DatasetLimits.Default with { MaxBytes = 100 });

            foreach (var bad in new[] { null, "", new string('x', 101), "[{\"a\":\"\ud800\"}]" })
            {
                var act = async () => await service.UploadJsonAsync(Name("Str"), "x", bad);
                await act.Should().ThrowAsync<UploadRejectedException>();
            }
        }

        [Theory]
        [InlineData("../../etc/passwd")]
        [InlineData("a/b")]
        [InlineData("a\\b")]
        [InlineData("..")]
        [InlineData("x<script>")]
        [InlineData("bad\u0001label")]
        public async Task UploadJson_SourceLabelThatIsAPathOrMarkup_IsRejectedAsync(string label)
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var act = async () =>
                await service.UploadJsonAsync(Name("Lbl"), label, "application/json", Body(SmallJson()));

            await act.Should().ThrowAsync<UploadRejectedException>();
        }

        [Fact]
        public async Task UploadJson_LongLabelIsRejected_AndMissingLabelGetsADefaultAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var act = async () => await service.UploadJsonAsync(Name("Lbl"), new string('x', 257), "application/json",
                Body(SmallJson()));

            await act.Should().ThrowAsync<UploadRejectedException>();

            var dataset = await service.UploadJsonAsync(Name("NoLbl"), null, "application/json", Body(SmallJson()));
            createdJsonIds.Add(dataset.Id);
            dataset.SourceLabel.Should().Be("Direct JSON");
        }

        [Theory]
        [InlineData("text/csv")]
        [InlineData("application/xml")]
        [InlineData("application/json; charset=utf-16")]
        [InlineData("application/json; charset=latin1")]
        [InlineData("image/png")]
        public async Task UploadJson_ContentTypeAndCharsetAreCheckedAsync(string contentType)
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var act = async () => await service.UploadJsonAsync(Name("Ct"), "x", contentType, Body(SmallJson()));

            await act.Should().ThrowAsync<UploadRejectedException>();
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("application/json; charset=utf-8")]
        [InlineData("application/json; charset=\"UTF-8\"")]
        [InlineData(null)]
        public async Task UploadJson_AcceptableContentTypesAreAcceptedAsync(string? contentType)
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var dataset = await service.UploadJsonAsync(Name("Ok"), "x", contentType, Body(SmallJson()));
            createdJsonIds.Add(dataset.Id);

            dataset.Kind.Should().Be("Json");
        }

        [Fact]
        public async Task JsonFileUpload_RequiresAJsonExtensionAndUsesTheLeafAsTheLabelAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            foreach (var bad in new[] { "data.csv", "data.txt", "data", "data.json.exe", "..", "", null })
            {
                var act = async () => await service.UploadJsonFileAsync(Name("F"), bad, "application/json",
                    Body(SmallJson()));

                await act.Should().ThrowAsync<UploadRejectedException>($"'{bad}'");
            }

            var dataset = await service.UploadJsonFileAsync(null, "..\\..\\folder/My Data.JSON", "application/json",
                Body(SmallJson()));
            createdJsonIds.Add(dataset.Id);
            dataset.SourceLabel.Should().Be("My Data.JSON");
            dataset.Name.Should().Be("My Data");
        }

        [Fact]
        public async Task CsvUpload_RejectsAJsonFileNameAndJsonContentAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var byName = async () => await service.UploadCsvAsync(Name("C"), "data.json", "text/csv",
                Body(SmallCsv()));

            await byName.Should().ThrowAsync<UploadRejectedException>();

            var byContent = async () => await service.UploadCsvAsync(Name("C"), "data.csv", "text/csv",
                Body(SmallJson()));

            (await byContent.Should().ThrowAsync<DatasetRejectedException>()).Which.Report.Errors[0].Message
                .Should().Contain("JSON");
        }

        [Fact]
        public async Task JsonUpload_WithCsvContentUnderAJsonNameIsRejectedAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var act = async () => await service.UploadJsonFileAsync(Name("J"), "data.json", "application/json",
                Body(SmallCsv()));

            (await act.Should().ThrowAsync<DatasetRejectedException>()).Which.Report.Errors
                .Should().Contain(e => e.Code == "NotJson");
        }

        [Fact]
        public async Task Names_AreUniqueAcrossBothKindsAndDefaultedFromTheLabelAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var csvName = Name("Dup");
            var csv = await service.UploadCsvAsync(csvName, "a.csv", "text/csv", Body(SmallCsv()));
            createdCsvIds.Add(csv.Id);

            var act = async () => await service.UploadJsonAsync(csvName.ToUpperInvariant(), "x", "application/json",
                Body(SmallJson()));

            await act.Should().ThrowAsync<UploadRejectedException>();

            var marker = Guid.NewGuid().ToString("N")[..8];

            var one = await service.UploadJsonAsync(null, $"{DatabaseFixture.Prefix}{marker}.json",
                "application/json", Body(SmallJson()));
            var two = await service.UploadJsonAsync(null, $"{DatabaseFixture.Prefix}{marker}.json",
                "application/json", Body(SmallJson()));

            createdJsonIds.AddRange([one.Id, two.Id]);
            one.Name.Should().Be($"{DatabaseFixture.Prefix}{marker}");
            two.Name.Should().Be($"{DatabaseFixture.Prefix}{marker} (2)");
        }

        [Fact]
        public async Task Confirm_MakesAJsonDatasetUsable_AndItIsNotUsableBeforeAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadJsonAsync(db, fx.Seed.UserWithPermission);

            var repository = new ExhaustiveInstanceDatasetRepository(db,
                await TenantAsync(db, fx.Seed.UserWithPermission));

            (await repository.IsConfirmedAsync(DatasetKind.Json, uploaded.Id)).Should().BeFalse();
            (await repository.GetConfirmedAsync(new DatasetReference(DatasetKind.Json, uploaded.Id))).Should().BeNull();

            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var confirmed = await service.ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded));

            confirmed.StatusId.Should().Be(1);
            confirmed.Kind.Should().Be("Json");
            confirmed.Columns.Where(c => c.Include).Select(c => c.Header).Should().Equal("Amount", "Score");
            (await repository.IsConfirmedAsync(DatasetKind.Json, uploaded.Id)).Should().BeTrue();
            (await repository.IsConfirmedAsync(DatasetKind.Csv, uploaded.Id)).Should().BeFalse();

            (await repository.GetConfirmedAsync(new DatasetReference(DatasetKind.Json, uploaded.Id)))!.Id
                .Should().Be(uploaded.Id);
        }

        [Fact]
        public async Task Confirm_UsesTheSharedMappingRules_ForJsonDatasetsAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadJsonAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var cases = new List<Action<ExhaustiveInstanceDatasetMappingDto>>
            {
                m => m.Columns![1].Include = true,
                m => m.Columns![0].Name = "1bad name",
                m => m.Columns![2].Name = m.Columns[0].Name,
                m => m.Columns![0].ProcessingTypeId = 99,
                m => m.Columns = [.. m.Columns!.Take(2)],
                m => m.Columns =
                [
                    .. m.Columns!.Select(c =>
                    {
                        c.Include = false;

                        return c;
                    })
                ]
            };

            foreach (var mutate in cases)
            {
                var mapping = ValidMapping(uploaded);
                mutate(mapping);
                var act = async () => await service.ConfirmAsync(DatasetKind.Json, mapping);
                await act.Should().ThrowAsync<DtoValidationException>();
            }

            (await service.GetByIdAsync(DatasetKind.Json, uploaded.Id))!.StatusId.Should().Be(0);
        }

        [Fact]
        public async Task Confirm_WithTheWrongKind_FindsNothingAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadJsonAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            await FluentActions.Awaiting(() => service.ConfirmAsync(DatasetKind.Csv, ValidMapping(uploaded)))
                .Should().ThrowAsync<NotFoundException>();

            (await service.GetByIdAsync(DatasetKind.Csv, uploaded.Id)).Should().BeNull();

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Csv, uploaded.Id)).Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Permissions_EveryJsonOperationIsRefusedWithoutThePermissionAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadJsonAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithoutPermission);

            await FluentActions.Awaiting(() => service.GetAsync()).Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.GetAsync(DatasetKind.Json)).Should()
                .ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.GetByIdAsync(DatasetKind.Json, uploaded.Id)).Should()
                .ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.UploadJsonAsync(Name("No"), "x", "application/json",
                Body(SmallJson()))).Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.UploadJsonAsync(Name("No"), "x", SmallJson())).Should()
                .ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.UploadJsonFileAsync(Name("No"), "a.json", "application/json",
                Body(SmallJson()))).Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded)))
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Json, uploaded.Id)).Should()
                .ThrowAsync<ForbiddenException>();

            (await db.ExhaustiveInstanceJson.CountAsync(w => w.Id == uploaded.Id && w.Deleted != 1)).Should().Be(1);
        }

        [Fact]
        public async Task TenantIsolation_AnotherTenantSeesAndChangesNoJsonDatasetAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadJsonAsync(db, fx.Seed.UserWithPermission);
            var other = await BuildAsync(db, fx.Seed.UserTenantB);

            (await other.GetAsync()).Should().NotContain(d => d.Id == uploaded.Id && d.Kind == "Json");
            (await other.GetByIdAsync(DatasetKind.Json, uploaded.Id)).Should().BeNull();

            await FluentActions.Awaiting(() => other.ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded))).Should()
                .ThrowAsync<NotFoundException>();

            await FluentActions.Awaiting(() => other.DeleteAsync(DatasetKind.Json, uploaded.Id)).Should()
                .ThrowAsync<NotFoundException>();

            var repository = new ExhaustiveInstanceDatasetRepository(db, await TenantAsync(db, fx.Seed.UserTenantB));
            (await repository.GetByIdAsync(DatasetKind.Json, uploaded.Id)).Should().BeNull();
            (await repository.IsConfirmedAsync(DatasetKind.Json, uploaded.Id)).Should().BeFalse();
            (await repository.DeleteAsync(DatasetKind.Json, uploaded.Id)).Should().BeFalse();
            (await new ExhaustiveInstanceDatasetRepository(db, 0).GetAsync()).Should().BeEmpty();
            (await db.ExhaustiveInstanceJsonRow.CountAsync(w => w.DatasetId == uploaded.Id)).Should().Be(30);
        }

        [Fact]
        public async Task CombinedListing_ShowsBothKinds_AndTheKindFilterNarrowsItAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var csv = await service.UploadCsvAsync(Name("LC"), "a.csv", "text/csv", Body(SmallCsv()));
            createdCsvIds.Add(csv.Id);
            var json = await UploadJsonAsync(db, fx.Seed.UserWithPermission);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == csv.Id && d.Kind == "Csv");
            all.Should().Contain(d => d.Id == json.Id && d.Kind == "Json");
            all.Should().OnlyContain(d => d.Columns.Count == 0);

            (await service.GetAsync(DatasetKind.Json)).Should().OnlyContain(d => d.Kind == "Json")
                .And.Contain(d => d.Id == json.Id);
            (await service.GetAsync(DatasetKind.Csv)).Should().OnlyContain(d => d.Kind == "Csv")
                .And.Contain(d => d.Id == csv.Id);
        }

        [Fact]
        public async Task Delete_RemovesTheDatasetAndItsRowsAcrossKindsAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadJsonAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            await service.DeleteAsync(DatasetKind.Json, uploaded.Id);

            (await service.GetByIdAsync(DatasetKind.Json, uploaded.Id)).Should().BeNull();
            (await db.ExhaustiveInstanceJsonRow.CountAsync(w => w.DatasetId == uploaded.Id)).Should().Be(0);
            (await db.ExhaustiveInstanceJson.FirstAsync(w => w.Id == uploaded.Id)).Deleted.Should().Be(1);

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Json, uploaded.Id)).Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Instance_ReferencesExactlyOneConfirmedDatasetOfTheSameTenantAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var pending = await UploadJsonAsync(db, user);
            var jsonUploaded = await UploadJsonAsync(db, user);
            var datasets = await BuildAsync(db, user);
            var json = await datasets.ConfirmAsync(DatasetKind.Json, ValidMapping(jsonUploaded));
            var csvUploaded = await datasets.UploadCsvAsync(Name("IC"), "a.csv", "text/csv", Body(SmallCsv()));
            createdCsvIds.Add(csvUploaded.Id);
            var csv = await datasets.ConfirmAsync(DatasetKind.Csv, ValidMapping(csvUploaded));
            var modelId = await ParentModelAsync(db, user);
            var instances = await InstanceServiceAsync(db, user);

            var jsonDto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("I"), DataSourceId = 2, ExhaustiveInstanceJsonId = json.Id
            };

            ApplyDatasetFilter(jsonDto, jsonUploaded.Columns.First(c => c.Header == "Fraud").Index, "1");
            var saved = await instances.InsertAsync(jsonDto);
            createdInstanceIds.Add(saved.Id);
            saved.ExhaustiveInstanceJsonId.Should().Be(json.Id);
            saved.ExhaustiveInstanceCsvId.Should().BeNull();
            var reread = (await instances.GetByIdAsync(saved.Id))!;
            reread.ExhaustiveInstanceJsonId.Should().Be(json.Id);
            reread.ExhaustiveInstanceCsvId.Should().BeNull();

            var csvDto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("IC"), DataSourceId = 2, ExhaustiveInstanceCsvId = csv.Id
            };

            ApplyDatasetFilter(csvDto, csvUploaded.Columns.First(c => c.Header == "Fraud").Index, "1");
            var viaCsv = await instances.InsertAsync(csvDto);
            createdInstanceIds.Add(viaCsv.Id);
            viaCsv.ExhaustiveInstanceCsvId.Should().Be(csv.Id);
            viaCsv.ExhaustiveInstanceJsonId.Should().BeNull();

            async Task ExpectAsync(ExhaustiveSearchInstanceDto dto, string code)
            {
                var thrown = (await FluentActions.Awaiting(() => instances.InsertAsync(dto))
                    .Should().ThrowAsync<DtoValidationException>()).Which;

                thrown.Result.Errors.Should().Contain(e => e.ErrorCode == code);
            }

            var basis = () => new ExhaustiveSearchInstanceDto
                { EntityAnalysisModelId = modelId, Name = Name("Bad"), DataSourceId = 2 };

            var none = basis();
            await ExpectAsync(none, "DatasetRequired");

            var both = basis();
            both.ExhaustiveInstanceCsvId = csv.Id;
            both.ExhaustiveInstanceJsonId = json.Id;
            await ExpectAsync(both, "DatasetAmbiguous");

            var unconfirmed = basis();
            unconfirmed.ExhaustiveInstanceJsonId = pending.Id;
            await ExpectAsync(unconfirmed, "DatasetNotFound");

            var wrongKind = basis();
            wrongKind.ExhaustiveInstanceCsvId = json.Id;
            await ExpectAsync(wrongKind, "DatasetNotFound");

            var unknown = basis();
            unknown.ExhaustiveInstanceJsonId = json.Id + 1_000_000;
            await ExpectAsync(unknown, "DatasetNotFound");

            foreach (var source in new int?[] { null, 0, 1 })
            {
                var notPermitted = basis();
                notPermitted.DataSourceId = source;
                notPermitted.ExhaustiveInstanceJsonId = json.Id;
                await ExpectAsync(notPermitted, "DatasetNotPermitted");
            }

            var demo = await instances.InsertAsync(new ExhaustiveSearchInstanceDto
                { EntityAnalysisModelId = modelId, Name = Name("Demo"), DataSourceId = 1 });
            createdInstanceIds.Add(demo.Id);
            demo.ExhaustiveInstanceJsonId.Should().BeNull();
            demo.ExhaustiveInstanceCsvId.Should().BeNull();
        }

        [Fact]
        public async Task Instance_JsonDatasetOfAnotherTenant_IsRejectedAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadJsonAsync(db, fx.Seed.UserWithPermission);
            var datasets = await BuildAsync(db, fx.Seed.UserWithPermission);
            var json = await datasets.ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded));
            var modelB = await ParentModelAsync(db, fx.Seed.UserTenantB);
            var service = await InstanceServiceAsync(db, fx.Seed.UserTenantB);

            var act = async () => await service.InsertAsync(new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelB, Name = Name("Foreign"), DataSourceId = 2,
                ExhaustiveInstanceJsonId = json.Id
            });

            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "DatasetNotFound");
        }

        [Fact]
        public async Task Delete_IsRefusedWhileAnyInstanceUsesTheJsonDatasetRegardlessOfStatusAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var uploaded = await UploadJsonAsync(db, user);
            var service = await BuildAsync(db, user);
            var json = await service.ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded));
            var modelId = await ParentModelAsync(db, user);

            var useDto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("Use"), DataSourceId = 2,
                ExhaustiveInstanceJsonId = json.Id
            };

            ApplyDatasetFilter(useDto, uploaded.Columns.First(c => c.Header == "Fraud").Index, "1");
            var saved = await (await InstanceServiceAsync(db, user)).InsertAsync(useDto);
            createdInstanceIds.Add(saved.Id);

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Json, json.Id)).Should()
                .ThrowAsync<DatasetInUseException>();

            await FluentActions.Awaiting(() => service.ConfirmAsync(DatasetKind.Json, ValidMapping(json))).Should()
                .ThrowAsync<DatasetInUseException>();

            await db.ExhaustiveSearchInstance.Where(w => w.Id == saved.Id).Set(s => s.StatusId, (byte)16)
                .UpdateAsync();

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Json, json.Id)).Should()
                .ThrowAsync<DatasetInUseException>(
                    "a finished (promoted) instance still depends on the dataset");

            await db.ExhaustiveSearchInstance.Where(w => w.Id == saved.Id).Set(s => s.Deleted, (byte)1)
                .UpdateAsync();

            await service.DeleteAsync(DatasetKind.Json, json.Id);
            (await service.GetByIdAsync(DatasetKind.Json, json.Id)).Should().BeNull();
        }

        [Fact]
        public async Task CheckConstraint_EnforcesExactlyOneDatasetReferenceOnlyForUploadedDatasetAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var uploaded = await UploadJsonAsync(db, user);
            var service = await BuildAsync(db, user);
            var json = await service.ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded));
            var csvUploaded = await service.UploadCsvAsync(Name("CK"), "a.csv", "text/csv", Body(SmallCsv()));
            createdCsvIds.Add(csvUploaded.Id);
            var modelId = await ParentModelAsync(db, user);

            var instance = await new ExhaustiveSearchInstanceRepository(db, user).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    Name = Name("Ck"), Guid = Guid.NewGuid(), EntityAnalysisModelId = modelId, Active = 1, Locked = 0,
                    Deleted = 0, StatusId = 0, DataSourceId = 0
                });
            createdInstanceIds.Add(instance.Id);

            async Task<PostgresException?> TryAsync(byte? source, int? csvId, int? jsonId)
            {
                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    await db.ExhaustiveSearchInstance.Where(w => w.Id == instance.Id)
                        .Set(s => s.DataSourceId, source).Set(s => s.ExhaustiveInstanceCsvId, csvId)
                        .Set(s => s.ExhaustiveInstanceJsonId, jsonId).UpdateAsync();

                    return null;
                }
                catch (PostgresException ex)
                {
                    return ex;
                }
            }

            (await TryAsync(2, null, null))!.ConstraintName.Should().Be("CK_ESI_DatasetReference");
            (await TryAsync(2, csvUploaded.Id, json.Id))!.ConstraintName.Should().Be("CK_ESI_DatasetReference");
            (await TryAsync(0, null, json.Id))!.ConstraintName.Should().Be("CK_ESI_DatasetReference");
            (await TryAsync(1, csvUploaded.Id, null))!.ConstraintName.Should().Be("CK_ESI_DatasetReference");
            (await TryAsync(null, csvUploaded.Id, null))!.ConstraintName.Should().Be("CK_ESI_DatasetReference");
            (await TryAsync(2, csvUploaded.Id, null)).Should().BeNull();
            (await TryAsync(2, null, json.Id)).Should().BeNull();
            (await TryAsync(0, null, null)).Should().BeNull();
            (await TryAsync(null, null, null)).Should().BeNull();
            (await TryAsync(2, null, json.Id + 1_000_000))!.SqlState.Should().Be("23503");
        }

        [Fact]
        public async Task Migration_CreatesTheStoresRowTablesColumnsAndIndexesOnAFreshDatabaseAsync()
        {
            await using var db = fx.GetDbContext();

            var columns = await db.QueryToListAsync<string>(
                "select table_name || '.' || column_name from information_schema.columns "
                + "where table_schema = 'public' and (table_name like 'ExhaustiveInstance%' "
                + "or (table_name = 'ExhaustiveSearchInstance' and column_name in ('DataSourceId', 'ExhaustiveInstanceCsvId', 'ExhaustiveInstanceJsonId')))");

            foreach (var table in new[] { "ExhaustiveInstanceCsv", "ExhaustiveInstanceJson" })
            {
                foreach (var column in new[]
                         {
                             "Id", "Guid", "TenantRegistryId", "Name", "ColumnsJson", "ReportJson",
                             "DependentColumnIndex", "RowCount", "SizeBytes", "StatusId", "CreatedUser", "CreatedDate",
                             "UpdatedDate", "Version", "Deleted", "DeletedUser", "DeletedDate", "ImportId"
                         })
                {
                    columns.Should().Contain($"{table}.{column}");
                }

                foreach (var column in new[] { "Id", "EntityAnalysisModelInstanceEntryGuid", "Json", table + "Id" })
                {
                    columns.Should().Contain($"{table}Row.{column}");
                }
            }

            columns.Should().Contain([
                "ExhaustiveInstanceCsv.FileName", "ExhaustiveInstanceCsv.DelimiterId",
                "ExhaustiveInstanceJson.SourceLabel", "ExhaustiveInstanceJson.ContentHash",
                "ExhaustiveSearchInstance.DataSourceId", "ExhaustiveSearchInstance.ExhaustiveInstanceCsvId",
                "ExhaustiveSearchInstance.ExhaustiveInstanceJsonId"
            ]);

            columns.Should().NotContain(c => c.Contains("ExhaustiveSearchInstanceDataset"));
            columns.Should().NotContain("ExhaustiveSearchInstance.ExhaustiveSearchInstanceDatasetId");

            var tables = await db.QueryToListAsync<string>(
                "select table_name from information_schema.tables where table_schema = 'public' "
                + "and table_name like 'ExhaustiveSearchInstanceDataset%'");
            tables.Should().BeEmpty();

            var constraints = await db.QueryToListAsync<string>(
                "select conname from pg_constraint where conrelid = '\"ExhaustiveSearchInstance\"'::regclass");
            constraints.Should().Contain(["CK_ESI_DatasetReference", "FK_ESI_InstanceCsv", "FK_ESI_InstanceJson"]);

            var indexes = await db.QueryToListAsync<string>(
                "select indexname from pg_indexes where tablename in ('ExhaustiveInstanceCsvRow', 'ExhaustiveInstanceJsonRow')");
            indexes.Should().HaveCount(4);
        }

        [Fact]
        public void SqlGate_DeniesEveryDatasetStoreAndRowTable()
        {
            foreach (var table in new[]
                     {
                         "ExhaustiveInstanceCsv", "ExhaustiveInstanceCsvRow", "ExhaustiveInstanceJson",
                         "ExhaustiveInstanceJsonRow"
                     })
            {
                var act = () =>
                    PostgresSqlValidator.AssertSelectOnly($"select * from \"{table}\"", true);

                act.Should().Throw<Exception>(table);
            }
        }

        private Training NewTraining()
        {
            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString,
                ["ExhaustiveTrialsLimit"] = "3",
                ["ExhaustiveTopologySinceImprovementLimit"] = "2",
                ["ExhaustiveTopologyExplorationEpochs"] = "2",
                ["ExhaustiveTopologyFinalisationEpochs"] = "2",
                ["ExhaustiveActivationFunctionExplorationEpochs"] = "3",
                ["ExhaustiveSimulationsCount"] = "50"
            });

            return new Training(LogManager.GetLogger(typeof(ExhaustiveInstanceJsonServiceTests)),
                environment, new JsonSerializationHelper())
            {
                seeded = new Random(7), pollDelay = TimeSpan.FromMilliseconds(20)
            };
        }

        private static (string Csv, string Json) SeparableData()
        {
            var random = new Random(11);
            var csv = new StringBuilder("Amount,Velocity,Age,Region,Noise,Fraud\n");
            var json = new StringBuilder("[");

            for (var i = 0; i < 240; i++)
            {
                var fraud = i % 2;

                var amount = Math.Round(fraud == 1 ? 300 + random.NextDouble() * 200 : 20 + random.NextDouble() * 200,
                    2);
                var velocity = Math.Round(fraud == 1 ? 6 + random.NextDouble() * 4 : 1 + random.NextDouble() * 4, 2);
                var age = 18 + random.Next(60);
                var noise = Math.Round(random.NextDouble(), 3);
                var region = i % 2 == 0 ? "North" : "South";
                var invariant = CultureInfo.InvariantCulture;

                csv.Append(amount.ToString(invariant)).Append(',').Append(velocity.ToString(invariant)).Append(',')
                    .Append(i % 40 == 0 ? "NA" : age.ToString(invariant)).Append(',').Append(region).Append(',')
                    .Append(noise.ToString(invariant)).Append(',').Append(fraud).Append('\n');

                json.Append(i == 0 ? "" : ",").Append("{\"Amount\":").Append(amount.ToString(invariant))
                    .Append(",\"Velocity\":").Append(velocity.ToString(invariant)).Append(",\"Age\":")
                    .Append(i % 40 == 0 ? "null" : age.ToString(invariant)).Append(",\"Region\":\"").Append(region)
                    .Append("\",\"Noise\":").Append(noise.ToString(invariant)).Append(",\"Fraud\":").Append(fraud)
                    .Append('}');
            }

            return (csv.ToString(), json.Append(']').ToString());
        }

        private static ExhaustiveInstanceDatasetMappingDto TrainingMapping(ExhaustiveInstanceDatasetDto uploaded)
        {
            return new ExhaustiveInstanceDatasetMappingDto
            {
                Id = uploaded.Id,
                Columns =
                [
                    .. uploaded.Columns.Select(c => new ExhaustiveInstanceDatasetMappingColumnDto
                    {
                        Index = c.Index, Include = c.Header is "Amount" or "Velocity" or "Age" or "Noise",
                        Name = c.Name,
                        ProcessingTypeId = c.Header == "Velocity" ? 1 : 5
                    })
                ]
            };
        }

        private async Task<(int InstanceId, List<double[]> Data, List<string> Variables, byte Status, double Score)>
            TrainAsync(DbContext db, string user, DatasetKind kind, int datasetId, int fraudIndex)
        {
            var modelId = await ParentModelAsync(db, user);
            var instances = await InstanceServiceAsync(db, user);

            var dto = new ExhaustiveSearchInstanceDto
                { EntityAnalysisModelId = modelId, Name = Name("Train"), DataSourceId = 2 };

            if (kind == DatasetKind.Csv)
            {
                dto.ExhaustiveInstanceCsvId = datasetId;
            }
            else
            {
                dto.ExhaustiveInstanceJsonId = datasetId;
            }

            ApplyDatasetFilter(dto, fraudIndex, "1");
            var saved = await instances.InsertAsync(dto);
            createdInstanceIds.Add(saved.Id);

            var run = new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = saved.Id, EntityAnalysisModelId = modelId, TenantRegistryId = await TenantAsync(db, user),
                DataSourceId = 2, ExhaustiveInstanceCsvId = dto.ExhaustiveInstanceCsvId,
                ExhaustiveInstanceJsonId = dto.ExhaustiveInstanceJsonId,
                Filter = dto.Filter, FilterJson = dto.FilterJson, FilterSql = dto.FilterSql,
                FilterTokens = dto.FilterTokens
            };

            (await NewTraining().RunOneInstanceAsync(run, db, CancellationToken.None)).Should().BeTrue();

            var row = await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == saved.Id);

            var variables = await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).OrderBy(o => o.VariableSequence).ToListAsync();

            var data = await db.GetTable<ExhaustiveSearchInstanceData>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).ToListAsync();
            var trials = await db.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).ToListAsync();

            (await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => trials.Select(t => t.Id).Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value))
                .ToListAsync()).Should().NotBeEmpty();

            return (saved.Id, [.. data.Select(d => d.Independent.Append(d.Dependent!.Value).ToArray())],
                [.. variables.Select(v => v.Name)], row.StatusId!.Value, row.Score ?? 0);
        }

        [Fact]
        public async Task EndToEnd_UploadJsonConfirmTrain_MatchesTheSameDataUploadedAsCsvAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var datasets = await BuildAsync(db, user);
            var (csvText, jsonText) = SeparableData();

            var csvUploaded = await datasets.UploadCsvAsync(Name("E2EC"), "fraud.csv", "text/csv", Body(csvText));
            createdCsvIds.Add(csvUploaded.Id);

            var jsonUploaded =
                await datasets.UploadJsonAsync(Name("E2EJ"), "fraud", "application/json", Body(jsonText));
            createdJsonIds.Add(jsonUploaded.Id);
            jsonUploaded.Warnings.Should().Contain(w => w.Contains("Region"));
            jsonUploaded.Warnings.Should().Contain(w => w.Contains("Age") && w.Contains("missing"));

            JsonSerializer.Serialize(jsonUploaded.Columns.Select(c => (c.Header, c.Kind,
                c.NumericCount,
                c.MissingCount, c.Min, c.Max, c.Include))).Should().Be(JsonSerializer.Serialize(
                csvUploaded.Columns.Select(c => (c.Header, c.Kind, c.NumericCount, c.MissingCount, c.Min, c.Max,
                    c.Include))));

            var csv = await datasets.ConfirmAsync(DatasetKind.Csv, TrainingMapping(csvUploaded));
            var jsonConfirmed = await datasets.ConfirmAsync(DatasetKind.Json, TrainingMapping(jsonUploaded));

            var fraudIndex = csvUploaded.Columns.First(c => c.Header == "Fraud").Index;
            var fromCsv = await TrainAsync(db, user, DatasetKind.Csv, csv.Id, fraudIndex);
            var fromJson = await TrainAsync(db, user, DatasetKind.Json, jsonConfirmed.Id, fraudIndex);

            fromJson.Status.Should().Be(16);
            fromJson.Score.Should().BeGreaterThan(0.5);

            fromJson.Variables.Should().Equal("Abstraction.Amount", "Payload.Velocity", "Abstraction.Age",
                "Abstraction.Noise");

            fromJson.Variables.Should().Equal(fromCsv.Variables);
            fromJson.Data.Should().HaveCount(fromCsv.Data.Count);
            fromJson.Data.Count(d => d[^1] == 1).Should().Be(fromJson.Data.Count(d => d[^1] == 0));

            for (var column = 0; column < fromJson.Data[0].Length; column++)
            {
                var jsonColumn = fromJson.Data.Select(d => d[column]).OrderBy(v => v).ToArray();
                var csvColumn = fromCsv.Data.Select(d => d[column]).OrderBy(v => v).ToArray();

                for (var i = 0; i < jsonColumn.Length; i++)
                {
                    jsonColumn[i].Should().BeApproximately(csvColumn[i], 1e-9, $"column {column}, rank {i}");
                }
            }
        }

        [Fact]
        public async Task Training_APendingOrForeignJsonDataset_StopsAtStatusSeventeenAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var pending = await UploadJsonAsync(db, user);
            var uploaded = await UploadJsonAsync(db, user);
            var confirmed = await (await BuildAsync(db, user)).ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded));
            var modelId = await ParentModelAsync(db, user);
            var tenantA = await TenantAsync(db, user);
            var tenantB = await TenantAsync(db, fx.Seed.UserTenantB);

            foreach (var (datasetId, tenant) in new[] { (pending.Id, tenantA), (confirmed.Id, tenantB) })
            {
                var instance = await new ExhaustiveSearchInstanceRepository(db, user).InsertAsync(
                    new Data.Poco.ExhaustiveSearchInstance
                    {
                        Name = Name("Stop"), Guid = Guid.NewGuid(), EntityAnalysisModelId = modelId, Active = 1,
                        Locked = 0, Deleted = 0, StatusId = 0, Anomaly = 0, Filter = 0, DataSourceId = 2,
                        ExhaustiveInstanceJsonId = datasetId
                    });
                createdInstanceIds.Add(instance.Id);

                var run = new GetNextExhaustiveSearchInstanceQueryDto
                {
                    Id = instance.Id, EntityAnalysisModelId = modelId, TenantRegistryId = tenant, DataSourceId = 2,
                    ExhaustiveInstanceJsonId = datasetId
                };

                (await NewTraining().RunOneInstanceAsync(run, db, CancellationToken.None)).Should().BeFalse();
                (await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == instance.Id)).StatusId.Should().Be(17);
            }
        }

        [Fact]
        public async Task Training_ARunReferencingBothOrNeitherDataset_StopsAtStatusSeventeenAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var uploaded = await UploadJsonAsync(db, user);
            var confirmed = await (await BuildAsync(db, user)).ConfirmAsync(DatasetKind.Json, ValidMapping(uploaded));
            var modelId = await ParentModelAsync(db, user);
            var tenant = await TenantAsync(db, user);

            foreach (var (csvId, jsonId) in new (int?, int?)[] { (null, null), (confirmed.Id, confirmed.Id) })
            {
                var instance = await new ExhaustiveSearchInstanceRepository(db, user).InsertAsync(
                    new Data.Poco.ExhaustiveSearchInstance
                    {
                        Name = Name("Both"), Guid = Guid.NewGuid(), EntityAnalysisModelId = modelId, Active = 1,
                        Locked = 0, Deleted = 0, StatusId = 0, Anomaly = 0, Filter = 0, DataSourceId = 0
                    });
                createdInstanceIds.Add(instance.Id);

                var run = new GetNextExhaustiveSearchInstanceQueryDto
                {
                    Id = instance.Id, EntityAnalysisModelId = modelId, TenantRegistryId = tenant, DataSourceId = 2,
                    ExhaustiveInstanceCsvId = csvId, ExhaustiveInstanceJsonId = jsonId
                };

                (await NewTraining().RunOneInstanceAsync(run, db, CancellationToken.None)).Should().BeFalse();
                (await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == instance.Id)).StatusId.Should().Be(17);
            }
        }
    }
}