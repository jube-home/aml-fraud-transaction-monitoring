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
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Analytics.Datasets.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.ExhaustiveInstanceDataset;
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Engine.Exhaustive;
using Jube.Engine.Helpers;
using Jube.Resources;
using Jube.Service.Exceptions.ExhaustiveInstanceDataset;
using Jube.Service.Exceptions.ExhaustiveSearchInstance;
using Jube.Service.Reactivity;
using Jube.Service.Repository.ExhaustiveInstanceDataset;
using Jube.Service.Repository.ExhaustiveSearchInstance;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Validations.ExhaustiveSearchInstance;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Test.Service.ExhaustiveInstanceDataset
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveInstanceDatasetServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private readonly List<int> createdDatasetIds = [];
        private readonly List<int> createdInstanceIds = [];
        private readonly List<int> createdModelIds = [];

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

            foreach (var id in createdDatasetIds)
            {
                await db.ExhaustiveInstanceCsvRow.Where(w => w.DatasetId == id)
                    .DeleteAsync();
                await db.ExhaustiveInstanceCsv.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static string Name(string label)
        {
            return $"{DatabaseFixture.Prefix}Ds{label}{Guid.NewGuid():N}"[..40];
        }

        private static Stream Csv(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
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

        private async Task<ExhaustiveInstanceDatasetDto> UploadAsync(DbContext db, string user,
            string? csv = null, string? name = null)
        {
            var service = await BuildAsync(db, user);

            var dataset = await service.UploadCsvAsync(name ?? Name("Up"), "sample.csv", "text/csv",
                Csv(csv ?? SmallCsv()));
            createdDatasetIds.Add(dataset.Id);

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
                        Index = c.Index,
                        Include = c.Header is "Amount" or "Score",
                        Name = c.Name,
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
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            createdModelIds.Add(saved.Id);

            return saved.Id;
        }

        [Fact]
        public async Task Upload_StoresAPendingDatasetWithColumnsRowsAndWarningsAsync()
        {
            await using var db = fx.GetDbContext();

            var dataset = await UploadAsync(db, fx.Seed.UserWithPermission);

            dataset.StatusId.Should().Be(0);
            dataset.RowCount.Should().Be(30);
            dataset.ColumnCount.Should().Be(4);
            dataset.Columns.Select(c => c.Kind).Should().Equal("Numeric", "Text", "Numeric", "Numeric");
            dataset.Warnings.Should().Contain(w => w.Contains("Country"));
            dataset.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            dataset.FileName.Should().Be("sample.csv");

            (await db.ExhaustiveInstanceCsvRow.CountAsync(w =>
                w.DatasetId == dataset.Id)).Should().Be(30);

            var row = await db.ExhaustiveInstanceCsvRow
                .Where(w => w.DatasetId == dataset.Id).FirstAsync();

            var values = JsonDocument.Parse(row.Json).RootElement.GetProperty("values");
            values.GetProperty("c1").ValueKind.Should().Be(JsonValueKind.String);
            values.GetProperty("c0").ValueKind.Should().Be(JsonValueKind.Number);
        }

        [Fact]
        public async Task Upload_ManyRows_AreBatchedAndAllStoredAsync()
        {
            await using var db = fx.GetDbContext();
            var builder = new StringBuilder("A,B,Y\n");

            for (var i = 0; i < 2500; i++)
            {
                builder.Append(i).Append(',').Append(i % 7).Append(',').Append(i % 2).Append('\n');
            }

            var dataset = await UploadAsync(db, fx.Seed.UserWithPermission, builder.ToString());

            dataset.RowCount.Should().Be(2500);

            (await db.ExhaustiveInstanceCsvRow.CountAsync(w =>
                w.DatasetId == dataset.Id)).Should().Be(2500);
        }

        [Fact]
        public async Task Upload_RejectedFile_StoresNothingAtAllAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var datasetsBefore = await db.ExhaustiveInstanceCsv.CountAsync();
            var rowsBefore = await db.ExhaustiveInstanceCsvRow.CountAsync();

            var act = async () => await service.UploadCsvAsync(Name("Bad"), "bad.csv", "text/csv",
                Csv("A,B\n1,2\n3\n4,5\n"));

            var thrown = (await act.Should().ThrowAsync<DatasetRejectedException>()).Which;
            thrown.Report.Errors.Should().ContainSingle().Which.Code.Should().Be("RaggedRow");
            (await db.ExhaustiveInstanceCsv.CountAsync()).Should().Be(datasetsBefore);
            (await db.ExhaustiveInstanceCsvRow.CountAsync()).Should().Be(rowsBefore);
        }

        [Fact]
        public async Task Upload_LateFailureAfterManyRows_RollsEverythingBackAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var rowsBefore = await db.ExhaustiveInstanceCsvRow.CountAsync();
            var builder = new StringBuilder("A,B\n");

            for (var i = 0; i < 1500; i++)
            {
                builder.Append(i).Append(',').Append(i).Append('\n');
            }

            builder.Append("only-one\n");

            var act = async () => await service.UploadCsvAsync(Name("Late"), "late.csv", "text/csv",
                Csv(builder.ToString()));

            await act.Should().ThrowAsync<DatasetRejectedException>();
            (await db.ExhaustiveInstanceCsvRow.CountAsync()).Should().Be(rowsBefore);
        }

        [Fact]
        public async Task Upload_ClientDisconnectMidStream_RollsBackAndStoresNothingAsync()
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var datasetsBefore = await db.ExhaustiveInstanceCsv.CountAsync();
            var rowsBefore = await db.ExhaustiveInstanceCsvRow.CountAsync();
            var builder = new StringBuilder("A,B\n");

            for (var i = 0; i < 6000; i++)
            {
                builder.Append(i).Append(',').Append(i % 5).Append('\n');
            }

            using var cts = new CancellationTokenSource();
            var stream = new CancellingStream(Encoding.UTF8.GetBytes(builder.ToString()), cts, 20000);
            var token = cts.Token;

            var act = async () => await service.UploadCsvAsync(Name("Cancel"), "c.csv", "text/csv", stream, token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            (await db.ExhaustiveInstanceCsv.CountAsync()).Should().Be(datasetsBefore);
            (await db.ExhaustiveInstanceCsvRow.CountAsync()).Should().Be(rowsBefore);
        }

        [Fact]
        public async Task Upload_CapsFromLimits_AreEnforcedAsync()
        {
            await using var db = fx.GetDbContext();

            var service = await BuildAsync(db, fx.Seed.UserWithPermission,
                DatasetLimits.Default with { MaxRows = 5, MaxColumns = 3, MaxBytes = 400 });

            (await FluentActions.Awaiting(() => service.UploadCsvAsync(Name("R"), "a.csv", "text/csv",
                    Csv("A,B\n1,2\n3,4\n5,6\n7,8\n9,10\n11,12\n"))).Should().ThrowAsync<DatasetRejectedException>())
                .Which.Report.Errors[0].Code.Should().Be("TooManyRows");

            (await FluentActions.Awaiting(() => service.UploadCsvAsync(Name("C"), "a.csv", "text/csv",
                    Csv("A,B,C,D\n1,2,3,4\n"))).Should().ThrowAsync<DatasetRejectedException>())
                .Which.Report.Errors[0].Code.Should().Be("TooManyColumns");

            (await FluentActions.Awaiting(() => service.UploadCsvAsync(Name("B"), "a.csv", "text/csv",
                    Csv("A\n" + new string('1', 1000) + "\n"))).Should().ThrowAsync<DatasetRejectedException>())
                .Which.Report.Errors[0].Code.Should().BeOneOf("TooLarge", "CellTooLong");
        }

        [Theory]
        [InlineData("../../etc/passwd.csv", "passwd.csv")]
        [InlineData("C:\\Users\\x\\data.csv", "data.csv")]
        [InlineData("a/b/c.TXT", "c.TXT")]
        [InlineData("plain.csv", "plain.csv")]
        public void FileName_IsReducedToItsLeaf(string supplied, string expected)
        {
            InvokeSanitise(supplied).Should().Be(expected);
        }

        [Theory]
        [InlineData("data.zip")]
        [InlineData("data.csv.exe")]
        [InlineData("data")]
        [InlineData(".csv")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("a<b>.csv")]
        [InlineData("a\u0000.csv")]
        [InlineData("../")]
        public void FileName_UnacceptableValuesAreRejected(string? supplied)
        {
            InvokeSanitise(supplied).Should().BeNull();
        }

        private static string? InvokeSanitise(string? name)
        {
            var method = typeof(ExhaustiveInstanceDatasetService).GetMethod("SanitiseFileName",
                BindingFlags.Static | BindingFlags.NonPublic |
                BindingFlags.Public, null, [typeof(string)], null)!;

            return (string?)method.Invoke(null, [name]);
        }

        [Theory]
        [InlineData("application/zip")]
        [InlineData("application/x-zip-compressed")]
        [InlineData("application/gzip")]
        [InlineData("image/png")]
        [InlineData("text/html")]
        [InlineData("application/json")]
        [InlineData("application/pdf")]
        public async Task Upload_HostileContentTypes_AreRejectedAsync(string contentType)
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var act = async () => await service.UploadCsvAsync(Name("Ct"), "a.csv", contentType, Csv(SmallCsv()));

            await act.Should().ThrowAsync<UploadRejectedException>();
        }

        [Theory]
        [InlineData("<script>")]
        [InlineData("")]
        public async Task Upload_HostileSuppliedNames_AreRejectedOrDefaultedAsync(string name)
        {
            await using var db = fx.GetDbContext();
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            if (name.Length > 0)
            {
                var act = async () => await service.UploadCsvAsync(name, "a.csv", "text/csv", Csv(SmallCsv()));
                await act.Should().ThrowAsync<UploadRejectedException>();

                return;
            }

            var dataset = await service.UploadCsvAsync(name, "My Data.csv", "text/csv", Csv(SmallCsv()));
            createdDatasetIds.Add(dataset.Id);
            dataset.Name.Should().StartWith("My Data");
        }

        [Fact]
        public async Task Upload_DuplicateSuppliedName_IsRejected_AndDerivedNamesAreDeduplicatedAsync()
        {
            await using var db = fx.GetDbContext();
            var first = await UploadAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var act = async () => await service.UploadCsvAsync(first.Name, "a.csv", "text/csv", Csv(SmallCsv()));
            await act.Should().ThrowAsync<UploadRejectedException>();

            var marker = Guid.NewGuid().ToString("N")[..8];

            var one = await service.UploadCsvAsync(null, $"{DatabaseFixture.Prefix}{marker}.csv", "text/csv",
                Csv(SmallCsv()));
            var two = await service.UploadCsvAsync(null, $"{DatabaseFixture.Prefix}{marker}.csv", "text/csv",
                Csv(SmallCsv()));

            createdDatasetIds.AddRange([one.Id, two.Id]);
            two.Name.Should().Be(one.Name + " (2)");
        }

        [Fact]
        public async Task Confirm_ValidMapping_MakesTheDatasetUsableAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var confirmed = await service.ConfirmAsync(DatasetKind.Csv, ValidMapping(uploaded));

            confirmed.StatusId.Should().Be(1);
            confirmed.Columns.Where(c => c.Include).Select(c => c.Header).Should().Equal("Amount", "Score");
            confirmed.Columns.First(c => c.Header == "Score").ProcessingTypeId.Should().Be(1);
            confirmed.Version.Should().Be(2);

            (await new ExhaustiveInstanceDatasetRepository(db, await TenantAsync(db,
                fx.Seed.UserWithPermission)).IsConfirmedAsync(DatasetKind.Csv, uploaded.Id)).Should().BeTrue();
        }

        [Fact]
        public async Task Confirm_InvalidMappings_AreRefusedAndTheDatasetStaysPendingAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var noVariables = ValidMapping(uploaded);

            foreach (var column in noVariables.Columns!)
            {
                column.Include = false;
            }

            var textIncluded = ValidMapping(uploaded);
            textIncluded.Columns!.First(c => c.Index == 1).Include = true;
            var badName = ValidMapping(uploaded);
            badName.Columns!.First(c => c.Index == 0).Name = "x; drop table";
            var badProcessingType = ValidMapping(uploaded);
            badProcessingType.Columns!.First(c => c.Index == 0).ProcessingTypeId = 99;

            foreach (var mapping in new[] { noVariables, textIncluded, badName, badProcessingType })
            {
                var act = async () => await service.ConfirmAsync(DatasetKind.Csv, mapping);
                await act.Should().ThrowAsync<DtoValidationException>();
            }

            (await service.GetByIdAsync(DatasetKind.Csv, uploaded.Id))!.StatusId.Should().Be(0);
        }

        [Fact]
        public async Task Confirm_ServerOwnedFieldsCannotBeInjectedThroughTheMappingAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);
            var mapping = ValidMapping(uploaded);
            mapping.Name = "Renamed " + Guid.NewGuid().ToString("N")[..10];

            var confirmed = await service.ConfirmAsync(DatasetKind.Csv, mapping);

            confirmed.Name.Should().Be(mapping.Name);
            confirmed.RowCount.Should().Be(30);
            confirmed.Guid.Should().Be(uploaded.Guid);
            confirmed.CreatedUser.Should().Be(uploaded.CreatedUser);
        }

        [Fact]
        public async Task Permissions_EveryOperationIsRefusedWithoutThePermissionAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithoutPermission);

            await FluentActions.Awaiting(() => service.GetAsync()).Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.GetByIdAsync(DatasetKind.Csv, uploaded.Id)).Should()
                .ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.UploadCsvAsync(Name("No"), "a.csv", "text/csv", Csv(SmallCsv())))
                .Should().ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.ConfirmAsync(DatasetKind.Csv, ValidMapping(uploaded))).Should()
                .ThrowAsync<ForbiddenException>();

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Csv, uploaded.Id)).Should()
                .ThrowAsync<ForbiddenException>();

            (await db.ExhaustiveInstanceCsv.CountAsync(w => w.Id == uploaded.Id && w.Deleted != 1))
                .Should().Be(1);
        }

        [Fact]
        public async Task Authentication_UnknownAndTenantlessUsersAreRefusedAsync()
        {
            await using var db = fx.GetDbContext();

            foreach (var user in new[] { null, "", "  ", fx.Seed.UnknownUser, fx.Seed.UserNoTenant })
            {
                // ReSharper disable once AccessToDisposedClosure
                var act = async () => await BuildAsync(db, user);
                await act.Should().ThrowAsync<NotAuthenticatedException>();
            }
        }

        [Fact]
        public async Task TenantIsolation_AnotherTenantSeesAndChangesNothingAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);
            var other = await BuildAsync(db, fx.Seed.UserTenantB);

            (await other.GetAsync()).Should().NotContain(d => d.Id == uploaded.Id);
            (await other.GetByIdAsync(DatasetKind.Csv, uploaded.Id)).Should().BeNull();

            await FluentActions.Awaiting(() => other.ConfirmAsync(DatasetKind.Csv, ValidMapping(uploaded))).Should()
                .ThrowAsync<NotFoundException>();

            await FluentActions.Awaiting(() => other.DeleteAsync(DatasetKind.Csv, uploaded.Id)).Should()
                .ThrowAsync<NotFoundException>();

            var owner = await BuildAsync(db, fx.Seed.UserWithPermission);
            (await owner.GetByIdAsync(DatasetKind.Csv, uploaded.Id))!.StatusId.Should().Be(0);

            (await db.ExhaustiveInstanceCsvRow.CountAsync(w =>
                w.DatasetId == uploaded.Id)).Should().Be(30);
        }

        [Fact]
        public async Task TenantIsolation_RepositoryNeverReturnsAnotherTenantsDatasetAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);
            var mapping = ValidMapping(uploaded);
            await (await BuildAsync(db, fx.Seed.UserWithPermission)).ConfirmAsync(DatasetKind.Csv, mapping);

            var tenantB = await TenantAsync(db, fx.Seed.UserTenantB);
            var repository = new ExhaustiveInstanceDatasetRepository(db, tenantB);

            (await repository.GetByIdAsync(DatasetKind.Csv, uploaded.Id)).Should().BeNull();
            (await repository.IsConfirmedAsync(DatasetKind.Csv, uploaded.Id)).Should().BeFalse();
            (await repository.GetAsync()).Should().NotContain(d => d.Id == uploaded.Id);
            (await repository.DeleteAsync(DatasetKind.Csv, uploaded.Id)).Should().BeFalse();
            (await new ExhaustiveInstanceDatasetRepository(db, 0).GetAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task Delete_RemovesRowsAndHidesTheDatasetAsync()
        {
            await using var db = fx.GetDbContext();
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            await service.DeleteAsync(DatasetKind.Csv, uploaded.Id);

            (await service.GetByIdAsync(DatasetKind.Csv, uploaded.Id)).Should().BeNull();

            (await db.ExhaustiveInstanceCsvRow.CountAsync(w =>
                w.DatasetId == uploaded.Id)).Should().Be(0);

            var row = await db.ExhaustiveInstanceCsv.FirstAsync(w => w.Id == uploaded.Id);
            row.Deleted.Should().Be(1);
            row.DeletedUser.Should().Be(fx.Seed.UserWithPermission);

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Csv, uploaded.Id)).Should()
                .ThrowAsync<NotFoundException>();
        }

        private static async Task<int> TenantAsync(DbContext db, string user)
        {
            return (await UserInTenantRepository.GetTenantRegistryIdAsync(db, user, CancellationToken.None))!.Value;
        }

        private async Task<(ExhaustiveInstanceDatasetDto Dataset, int ModelId)> ConfirmedDatasetAndModelAsync(
            DbContext db)
        {
            var uploaded = await UploadAsync(db, fx.Seed.UserWithPermission);

            var dataset =
                await (await BuildAsync(db, fx.Seed.UserWithPermission)).ConfirmAsync(DatasetKind.Csv,
                    ValidMapping(uploaded));

            var modelId = await ParentModelAsync(db, fx.Seed.UserWithPermission);

            return (dataset, modelId);
        }

        private static Task<ExhaustiveSearchInstanceService> InstanceServiceAsync(DbContext db, string user)
        {
            return ExhaustiveSearchInstanceService.CreateAsync(db, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        [Fact]
        public async Task Instance_UploadedDataSource_RequiresAConfirmedDatasetOfTheSameTenantAsync()
        {
            await using var db = fx.GetDbContext();
            var (dataset, modelId) = await ConfirmedDatasetAndModelAsync(db);
            var pending = await UploadAsync(db, fx.Seed.UserWithPermission);
            var service = await InstanceServiceAsync(db, fx.Seed.UserWithPermission);

            var fraudIndex = dataset.Columns.First(c => c.Header == "Fraud").Index;

            var dto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("I"), DataSourceId = 2,
                ExhaustiveInstanceCsvId = dataset.Id
            };

            ApplyDatasetFilter(dto, fraudIndex, "1");
            var saved = await service.InsertAsync(dto);
            createdInstanceIds.Add(saved.Id);
            saved.DataSourceId.Should().Be(2);
            saved.ExhaustiveInstanceCsvId.Should().Be(dataset.Id);
            (await service.GetByIdAsync(saved.Id))!.ExhaustiveInstanceCsvId.Should().Be(dataset.Id);

            foreach (var id in new int?[] { null, pending.Id, dataset.Id + 1_000_000, 0, -1 })
            {
                var act = async () => await service.InsertAsync(new ExhaustiveSearchInstanceDto
                {
                    EntityAnalysisModelId = modelId, Name = Name("I"), DataSourceId = 2,
                    ExhaustiveInstanceCsvId = id
                });

                await act.Should().ThrowAsync<DtoValidationException>($"dataset id {id} is not usable");
            }
        }

        [Fact]
        public async Task Instance_DatasetOfAnotherTenant_IsRejected_MassAssignmentProtectedAsync()
        {
            await using var db = fx.GetDbContext();
            var (dataset, _) = await ConfirmedDatasetAndModelAsync(db);
            var modelB = await ParentModelAsync(db, fx.Seed.UserTenantB);
            var service = await InstanceServiceAsync(db, fx.Seed.UserTenantB);

            var act = async () => await service.InsertAsync(new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelB, Name = Name("Foreign"), DataSourceId = 2,
                ExhaustiveInstanceCsvId = dataset.Id
            });

            var thrown = (await act.Should().ThrowAsync<DtoValidationException>()).Which;
            thrown.Result.Errors.Should().Contain(e => e.ErrorCode == "DatasetNotFound");
        }

        [Fact]
        public async Task Instance_DataSourceValues_AreKnownOnly_AndDatasetOnlyWithUploadedAsync()
        {
            await using var db = fx.GetDbContext();
            var (dataset, modelId) = await ConfirmedDatasetAndModelAsync(db);
            var service = await InstanceServiceAsync(db, fx.Seed.UserWithPermission);

            foreach (var bad in new int?[] { 3, 99, -1, 256, int.MaxValue })
            {
                var act = async () => await service.InsertAsync(new ExhaustiveSearchInstanceDto
                    { EntityAnalysisModelId = modelId, Name = Name("Bad"), DataSourceId = bad });

                await act.Should().ThrowAsync<DtoValidationException>();
            }

            foreach (var source in new int?[] { null, 0, 1 })
            {
                var act = async () => await service.InsertAsync(new ExhaustiveSearchInstanceDto
                {
                    EntityAnalysisModelId = modelId, Name = Name("Mix"), DataSourceId = source,
                    ExhaustiveInstanceCsvId = dataset.Id
                });

                await act.Should().ThrowAsync<DtoValidationException>();
            }

            var archive = await service.InsertAsync(new ExhaustiveSearchInstanceDto
                { EntityAnalysisModelId = modelId, Name = Name("Arch") });

            var demo = await service.InsertAsync(new ExhaustiveSearchInstanceDto
                { EntityAnalysisModelId = modelId, Name = Name("Demo"), DataSourceId = 1 });

            createdInstanceIds.AddRange([archive.Id, demo.Id]);
            archive.DataSourceId.Should().Be(0);
            demo.DataSourceId.Should().Be(1);
            demo.ExhaustiveInstanceCsvId.Should().BeNull();
        }

        [Fact]
        public async Task Instance_ValidatorWithoutADatasetRepository_FailsClosedAsync()
        {
            await using var db = fx.GetDbContext();
            var modelId = await ParentModelAsync(db, fx.Seed.UserWithPermission);
            var repository = new ExhaustiveSearchInstanceRepository(db, fx.Seed.UserWithPermission);

            var validator = new ExhaustiveSearchInstanceDtoValidator(
                repository, localizers.Create(typeof(ExhaustiveSearchInstanceResources)));

            var result = await validator.ValidateAsync(new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("Closed"), DataSourceId = 2,
                ExhaustiveInstanceCsvId = 1
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorCode == "DatasetNotFound");
        }

        [Fact]
        public async Task Delete_IsRefusedWhileAnyInstanceUsesItRegardlessOfStatusAsync()
        {
            await using var db = fx.GetDbContext();
            var (dataset, modelId) = await ConfirmedDatasetAndModelAsync(db);
            var instances = await InstanceServiceAsync(db, fx.Seed.UserWithPermission);

            var useDto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("Use"), DataSourceId = 2,
                ExhaustiveInstanceCsvId = dataset.Id
            };

            ApplyDatasetFilter(useDto, dataset.Columns.First(c => c.Header == "Fraud").Index, "1");
            var saved = await instances.InsertAsync(useDto);
            createdInstanceIds.Add(saved.Id);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Csv, dataset.Id)).Should()
                .ThrowAsync<DatasetInUseException>();

            await FluentActions.Awaiting(() => service.ConfirmAsync(DatasetKind.Csv, ValidMapping(dataset))).Should()
                .ThrowAsync<DatasetInUseException>();

            await db.ExhaustiveSearchInstance.Where(w => w.Id == saved.Id)
                .Set(s => s.StatusId, (byte)16).UpdateAsync();

            await FluentActions.Awaiting(() => service.DeleteAsync(DatasetKind.Csv, dataset.Id)).Should()
                .ThrowAsync<DatasetInUseException>(
                    "a finished (promoted) instance still depends on the dataset");

            await db.ExhaustiveSearchInstance.Where(w => w.Id == saved.Id)
                .Set(s => s.Deleted, (byte)1).UpdateAsync();

            await service.DeleteAsync(DatasetKind.Csv, dataset.Id);
            (await service.GetByIdAsync(DatasetKind.Csv, dataset.Id)).Should().BeNull();
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

            return new Training(LogManager.GetLogger(typeof(ExhaustiveInstanceDatasetServiceTests)),
                environment, new JsonSerializationHelper())
            {
                seeded = new Random(7), pollDelay = TimeSpan.FromMilliseconds(20)
            };
        }

        private static string SeparableCsv()
        {
            var random = new Random(11);
            var builder = new StringBuilder("Amount;Velocity;Age;Region;Noise;Fraud\n");

            for (var i = 0; i < 240; i++)
            {
                var fraud = i % 2;
                var amount = fraud == 1 ? 300 + random.NextDouble() * 200 : 20 + random.NextDouble() * 200;
                var velocity = fraud == 1 ? 6 + random.NextDouble() * 4 : 1 + random.NextDouble() * 4;
                var age = 18 + random.Next(60);
                var noise = random.NextDouble();

                builder.Append(amount.ToString("F2", CultureInfo.InvariantCulture)).Append(';')
                    .Append(velocity.ToString("F2", CultureInfo.InvariantCulture)).Append(';')
                    .Append(i % 40 == 0 ? "NA" : age.ToString()).Append(';')
                    .Append(i % 2 == 0 ? "North" : "South").Append(';')
                    .Append(noise.ToString("F3", CultureInfo.InvariantCulture)).Append(';')
                    .Append(fraud).Append('\n');
            }

            return builder.ToString();
        }

        [Fact]
        public async Task EndToEnd_UploadConfirmTrain_ReachesAPromotedTrialWithTheMappedVariablesAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var datasets = await BuildAsync(db, user);
            var uploaded = await datasets.UploadCsvAsync(Name("E2E"), "fraud.csv", "text/csv", Csv(SeparableCsv()));
            createdDatasetIds.Add(uploaded.Id);
            uploaded.Delimiter.Should().Be(';');
            uploaded.Warnings.Should().Contain(w => w.Contains("Region"));
            uploaded.Warnings.Should().Contain(w => w.Contains("Age") && w.Contains("missing"));

            var fraudIndex = uploaded.Columns.First(c => c.Header == "Fraud").Index;

            var mapping = new ExhaustiveInstanceDatasetMappingDto
            {
                Id = uploaded.Id,
                Columns =
                [
                    .. uploaded.Columns.Select(c => new ExhaustiveInstanceDatasetMappingColumnDto
                    {
                        Index = c.Index,
                        Include = c.Header is "Amount" or "Velocity" or "Age" or "Noise",
                        Name = c.Name,
                        ProcessingTypeId = c.Header == "Velocity" ? 1 : 5
                    })
                ]
            };

            var confirmed = await datasets.ConfirmAsync(DatasetKind.Csv, mapping);

            var modelId = await ParentModelAsync(db, user);
            var instances = await InstanceServiceAsync(db, user);

            var trainDto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("Train"), DataSourceId = 2,
                ExhaustiveInstanceCsvId = confirmed.Id
            };

            ApplyDatasetFilter(trainDto, fraudIndex, "1");
            var saved = await instances.InsertAsync(trainDto);
            createdInstanceIds.Add(saved.Id);
            var tenantId = await TenantAsync(db, user);

            var run = new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = saved.Id, EntityAnalysisModelId = modelId, TenantRegistryId = tenantId, DataSourceId = 2,
                ExhaustiveInstanceCsvId = confirmed.Id,
                Filter = trainDto.Filter, FilterJson = trainDto.FilterJson, FilterSql = trainDto.FilterSql,
                FilterTokens = trainDto.FilterTokens
            };

            var completed = await NewTraining().RunOneInstanceAsync(run, db, CancellationToken.None);

            completed.Should().BeTrue();
            var row = await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == saved.Id);
            row.StatusId.Should().Be(16);
            row.Models.Should().BeGreaterThan(0);

            var variables = await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).OrderBy(o => o.VariableSequence).ToListAsync();

            variables.Select(v => v.Name).Should()
                .Equal("Abstraction.Amount", "Payload.Velocity", "Abstraction.Age", "Abstraction.Noise");

            variables.Select(v => (int)v.ProcessingTypeId!.Value).Should().Equal(5, 1, 5, 5);

            var data = await db.GetTable<ExhaustiveSearchInstanceData>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).ToListAsync();
            data.Should().NotBeEmpty();
            data.Count(d => d.Dependent == 1).Should().Be(data.Count(d => d.Dependent == 0));
            data.Should().OnlyContain(d => d.Independent.Length == 4);

            var trials = await db.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).ToListAsync();

            var promoted = await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => trials.Select(t => t.Id).Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value))
                .ToListAsync();
            promoted.Should().NotBeEmpty();
            row.Score.Should().BeGreaterThan(0.5);
        }

        [Fact]
        public async Task Upload_CreditRiskFixture_FitsPurposeAsCategoricalAndOffersDependentAsAStringColumnAsync()
        {
            await using var db = fx.GetDbContext();

            var csv = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Exhaustive", "Datasets",
                "CreditRisk.csv"));

            var dataset = await UploadAsync(db, fx.Seed.UserWithPermission, csv, Name("Credit"));

            var dependent = dataset.Columns.First(c => c.Header == "Dependent");
            dependent.Kind.Should().Be("Text");
            dependent.DistinctValues.Select(v => v.Value).Should().BeEquivalentTo("Good", "Bad");
            dependent.IsCategorical.Should().BeTrue();

            dependent.Include.Should().BeFalse(
                "Dependent is a two-level column and is excluded from training by default, " +
                "remaining available only for the Class Definition filter");

            var purpose = dataset.Columns.First(c => c.Header == "Purpose");
            purpose.Kind.Should().Be("Text");
            purpose.IsCategorical.Should().BeTrue();
            purpose.Include.Should().BeTrue();

            var age = dataset.Columns.First(c => c.Header == "Age");
            age.Kind.Should().Be("Numeric");
            age.Include.Should().BeTrue();
        }

        private static string SeparableCategoricalCsv()
        {
            var random = new Random(13);
            var builder = new StringBuilder("Amount,Segment,Noise,Risk\n");

            for (var i = 0; i < 240; i++)
            {
                var bad = i % 2;
                var amount = bad == 1 ? 300 + random.NextDouble() * 200 : 20 + random.NextDouble() * 200;
                var noise = random.NextDouble();

                builder.Append(amount.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                    .Append(bad == 1 ? "High" : "Low").Append(',')
                    .Append(noise.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                    .Append(bad == 1 ? "Bad" : "Good").Append('\n');
            }

            return builder.ToString();
        }

        [Fact]
        public async Task EndToEnd_CategoricalColumnAndStringDependent_TrainsSuccessfullyAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var datasets = await BuildAsync(db, user);

            var uploaded = await datasets.UploadCsvAsync(Name("Cat"), "credit.csv", "text/csv",
                Csv(SeparableCategoricalCsv()));
            createdDatasetIds.Add(uploaded.Id);

            var segment = uploaded.Columns.First(c => c.Header == "Segment");
            segment.IsCategorical.Should().BeTrue();

            var riskIndex = uploaded.Columns.First(c => c.Header == "Risk").Index;

            var mapping = new ExhaustiveInstanceDatasetMappingDto
            {
                Id = uploaded.Id,
                Columns =
                [
                    .. uploaded.Columns.Select(c => new ExhaustiveInstanceDatasetMappingColumnDto
                    {
                        Index = c.Index,
                        Include = c.Header is "Amount" or "Segment",
                        Name = c.Name,
                        ProcessingTypeId = 5
                    })
                ]
            };

            var confirmed = await datasets.ConfirmAsync(DatasetKind.Csv, mapping);

            var modelId = await ParentModelAsync(db, user);
            var instances = await InstanceServiceAsync(db, user);

            var catTrainDto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("CatTrain"), DataSourceId = 2,
                ExhaustiveInstanceCsvId = confirmed.Id
            };

            ApplyDatasetFilter(catTrainDto, riskIndex, "Bad");
            var saved = await instances.InsertAsync(catTrainDto);
            createdInstanceIds.Add(saved.Id);
            var tenantId = await TenantAsync(db, user);

            var run = new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = saved.Id, EntityAnalysisModelId = modelId, TenantRegistryId = tenantId, DataSourceId = 2,
                ExhaustiveInstanceCsvId = confirmed.Id,
                Filter = catTrainDto.Filter, FilterJson = catTrainDto.FilterJson, FilterSql = catTrainDto.FilterSql,
                FilterTokens = catTrainDto.FilterTokens
            };

            var completed = await NewTraining().RunOneInstanceAsync(run, db, CancellationToken.None);

            completed.Should().BeTrue();
            var row = await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == saved.Id);
            row.StatusId.Should().Be(16);

            var variables = await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).OrderBy(o => o.VariableSequence).ToListAsync();

            variables.Select(v => v.Name).Should().Contain(v => v.StartsWith("Abstraction.Segment="));

            var data = await db.GetTable<ExhaustiveSearchInstanceData>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).ToListAsync();
            data.Should().NotBeEmpty();
            data.Count(d => d.Dependent == 1).Should().Be(data.Count(d => d.Dependent == 0));

            var trials = await db.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).ToListAsync();

            var promoted = await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => trials.Select(t => t.Id).Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value))
                .ToListAsync();
            promoted.Should().NotBeEmpty();
        }

        [Fact]
        public async Task EndToEnd_TheFilteredColumnIsNeverIncludedAsATrainingVariableEvenIfMappedIncludedAsync()
        {
            await using var db = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var datasets = await BuildAsync(db, user);

            var uploaded = await datasets.UploadCsvAsync(Name("Leak"), "credit.csv", "text/csv",
                Csv(SeparableCategoricalCsv()));
            createdDatasetIds.Add(uploaded.Id);

            var riskIndex = uploaded.Columns.First(c => c.Header == "Risk").Index;

            var mapping = new ExhaustiveInstanceDatasetMappingDto
            {
                Id = uploaded.Id,
                Columns =
                [
                    .. uploaded.Columns.Select(c => new ExhaustiveInstanceDatasetMappingColumnDto
                    {
                        Index = c.Index,
                        Include = true,
                        Name = c.Name,
                        ProcessingTypeId = 5
                    })
                ]
            };

            var confirmed = await datasets.ConfirmAsync(DatasetKind.Csv, mapping);

            var modelId = await ParentModelAsync(db, user);
            var instances = await InstanceServiceAsync(db, user);

            var leakDto = new ExhaustiveSearchInstanceDto
            {
                EntityAnalysisModelId = modelId, Name = Name("LeakTrain"), DataSourceId = 2,
                ExhaustiveInstanceCsvId = confirmed.Id
            };

            ApplyDatasetFilter(leakDto, riskIndex, "Bad");
            var saved = await instances.InsertAsync(leakDto);
            createdInstanceIds.Add(saved.Id);
            var tenantId = await TenantAsync(db, user);

            var run = new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = saved.Id, EntityAnalysisModelId = modelId, TenantRegistryId = tenantId, DataSourceId = 2,
                ExhaustiveInstanceCsvId = confirmed.Id,
                Filter = leakDto.Filter, FilterJson = leakDto.FilterJson, FilterSql = leakDto.FilterSql,
                FilterTokens = leakDto.FilterTokens
            };

            var completed = await NewTraining().RunOneInstanceAsync(run, db, CancellationToken.None);

            completed.Should().BeTrue();

            var variables = await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == saved.Id).ToListAsync();

            variables.Should().NotContain(v => v.Name.Contains("Risk"),
                "the column the Class Definition filters on must never leak into training as a feature");

            variables.Select(v => v.Name).Should().Contain(v => v.StartsWith("Abstraction.Amount"));
        }

        [Fact]
        public async Task Training_AnUnconfirmedOrForeignDataset_StopsAtStatusSeventeenWithoutReadingRowsAsync()
        {
            await using var db = fx.GetDbContext();
            var pending = await UploadAsync(db, fx.Seed.UserWithPermission);
            var modelId = await ParentModelAsync(db, fx.Seed.UserWithPermission);
            var tenantA = await TenantAsync(db, fx.Seed.UserWithPermission);
            var tenantB = await TenantAsync(db, fx.Seed.UserTenantB);
            var (confirmed, _) = await ConfirmedDatasetAndModelAsync(db);

            foreach (var (datasetId, tenant) in new[] { (pending.Id, tenantA), (confirmed.Id, tenantB) })
            {
                var instance = await new ExhaustiveSearchInstanceRepository(db, fx.Seed.UserWithPermission)
                    .InsertAsync(new Data.Poco.ExhaustiveSearchInstance
                    {
                        Name = Name("Stop"), Guid = Guid.NewGuid(), EntityAnalysisModelId = modelId, Active = 1,
                        Locked = 0, Deleted = 0, StatusId = 0, Anomaly = 0, Filter = 0, DataSourceId = 2,
                        ExhaustiveInstanceCsvId = datasetId
                    });
                createdInstanceIds.Add(instance.Id);

                var run = new GetNextExhaustiveSearchInstanceQueryDto
                {
                    Id = instance.Id, EntityAnalysisModelId = modelId, TenantRegistryId = tenant, DataSourceId = 2,
                    ExhaustiveInstanceCsvId = datasetId
                };

                var completed = await NewTraining().RunOneInstanceAsync(run, db, CancellationToken.None);

                completed.Should().BeFalse();
                (await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == instance.Id)).StatusId.Should().Be(17);
            }
        }

        private sealed class CancellingStream(byte[] data, CancellationTokenSource source, int cancelAfterBytes)
            : Stream
        {
            private int position;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => data.Length;

            public override long Position
            {
                get => position;
                set => throw new NotSupportedException();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = Math.Min(Math.Min(buffer.Length, 4096), data.Length - position);
                data.AsMemory(position, count).CopyTo(buffer);
                position += count;

                if (position >= cancelAfterBytes)
                {
                    await source.CancelAsync();
                }

                return count;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}