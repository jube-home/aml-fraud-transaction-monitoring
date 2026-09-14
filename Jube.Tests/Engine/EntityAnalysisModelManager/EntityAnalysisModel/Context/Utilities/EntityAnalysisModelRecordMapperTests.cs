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
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Cryptography;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Xunit;
using EngineContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;
using EngineModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using PocoModel = Jube.Data.Poco.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelRecordMapperTests
    {
        private static PocoModel NewRecord()
        {
            return new PocoModel
            {
                Id = 1,
                Active = 1,
                TenantRegistryId = 7,
                Guid = Guid.NewGuid()
            };
        }

        private static EngineModel NewModel(int id = 1)
        {
            var model = new EngineModel
            {
                Instance =
                {
                    Id = id
                }
            };
            return model;
        }

        private static void Map(PocoModel record, EngineModel model)
        {
            EntityAnalysisModelRecordMapper.MapRecordFields(record, model, TestLog.NoOp);
        }

        private static EngineContext NewContext()
        {
            var context = new EngineContext
            {
                JsonSerializationHelper = new JsonSerializationHelper(),
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = TestDynamicEnvironment.Create()
                },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new Dictionary<int, EngineModel>(),
                    EntityAnalysisModelSuppressionModels = new Dictionary<string, List<string>>(),
                    EntityAnalysisInstanceGuid = Guid.NewGuid()
                }
            };
            return context;
        }

        [Fact]
        public void NameNullOnTheRecordDefaultsToAnEmptyString()
        {
            var record = NewRecord();
            record.Name = null;
            var model = NewModel();

            Map(record, model);

            model.Instance.Name.Should().Be("");
        }

        [Fact]
        public void NameWithSpacesHasSpacesReplacedWithUnderscores()
        {
            var record = NewRecord();
            record.Name = "My Model Name";
            var model = NewModel();

            Map(record, model);

            model.Instance.Name.Should().Be("My_Model_Name");
        }

        [Fact]
        public void EntryXPathNullOnTheRecordDefaultsToAnEmptyString()
        {
            var record = NewRecord();
            record.EntryXPath = null;
            var model = NewModel();

            Map(record, model);

            model.References.EntryXPath.Should().Be("");
        }

        [Fact]
        public void EntryXPathIsCopiedVerbatimWhenPresent()
        {
            var record = NewRecord();
            record.EntryXPath = "$.TxnId";
            var model = NewModel();

            Map(record, model);

            model.References.EntryXPath.Should().Be("$.TxnId");
        }

        [Fact]
        public void ReferenceDateXPathNullOnTheRecordDefaultsToAnEmptyString()
        {
            var record = NewRecord();
            record.ReferenceDateXPath = null;
            var model = NewModel();

            Map(record, model);

            model.References.ReferenceDateXpath.Should().Be("");
        }

        [Fact]
        public void ReferenceDateXPathIsCopiedVerbatimWhenPresent()
        {
            var record = NewRecord();
            record.ReferenceDateXPath = "$.TxnDateTime";
            var model = NewModel();

            Map(record, model);

            model.References.ReferenceDateXpath.Should().Be("$.TxnDateTime");
        }

        [Fact]
        public void EntryNameNullOnTheRecordDefaultsToAnEmptyString()
        {
            var record = NewRecord();
            record.EntryName = null;
            var model = NewModel();

            Map(record, model);

            model.References.EntryName.Should().Be("");
        }

        [Fact]
        public void EntryNameIsCopiedVerbatimWhenPresent()
        {
            var record = NewRecord();
            record.EntryName = "TxnId";
            var model = NewModel();

            Map(record, model);

            model.References.EntryName.Should().Be("TxnId");
        }

        [Fact]
        public void ReferenceDateNameNullOnTheRecordDefaultsToAnEmptyString()
        {
            var record = NewRecord();
            record.ReferenceDateName = null;
            var model = NewModel();

            Map(record, model);

            model.References.ReferenceDateName.Should().Be("");
        }

        [Fact]
        public void ReferenceDateNameIsCopiedVerbatimWhenPresent()
        {
            var record = NewRecord();
            record.ReferenceDateName = "TxnDateTime";
            var model = NewModel();

            Map(record, model);

            model.References.ReferenceDateName.Should().Be("TxnDateTime");
        }

        public static IEnumerable<object[]> BooleanFlagFields()
        {
            yield return
            [
                "EnableCache", (Action<PocoModel, byte?>)((r, v) => r.EnableCache = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableCache), false
            ];
            yield return
            [
                "EnableActivationWatcher", (Action<PocoModel, byte?>)((r, v) => r.EnableActivationWatcher = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableActivationWatcher), false
            ];
            yield return
            [
                "EnableResponseElevationLimit",
                (Action<PocoModel, byte?>)((r, v) => r.EnableResponseElevationLimit = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableResponseElevationLimit), false
            ];
            yield return
            [
                "EnableImplicitAsync", (Action<PocoModel, byte?>)((r, v) => r.EnableImplicitAsync = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableImplicitAsync), false
            ];
            yield return
            [
                "EnableTtlCounter", (Action<PocoModel, byte?>)((r, v) => r.EnableTtlCounter = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableTtlCounter), false
            ];
            yield return
            [
                "EnableSanctionCache", (Action<PocoModel, byte?>)((r, v) => r.EnableSanctionCache = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableSanctionCache), false
            ];
            yield return
            [
                "EnableActivationArchive", (Action<PocoModel, byte?>)((r, v) => r.EnableActivationArchive = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableActivationArchive), false
            ];
            yield return
            [
                "EnableRdbmsArchive", (Action<PocoModel, byte?>)((r, v) => r.EnableRdbmsArchive = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableRdbmsArchive), true
            ];
            yield return
            [
                "EnableTrace", (Action<PocoModel, byte?>)((r, v) => r.EnableTrace = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableTrace), false
            ];
            yield return
            [
                "EnableLogs", (Action<PocoModel, byte?>)((r, v) => r.EnableLogs = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableLogs), false
            ];
            yield return
            [
                "EnableLogsInfo", (Action<PocoModel, byte?>)((r, v) => r.EnableLogsInfo = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableLogsInfo), false
            ];
            yield return
            [
                "EnableLogsWarnThreshold", (Action<PocoModel, byte?>)((r, v) => r.EnableLogsWarnThreshold = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableLogsWarnThreshold), false
            ];
            yield return
            [
                "EnableLogsInResponse", (Action<PocoModel, byte?>)((r, v) => r.EnableLogsInResponse = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableLogsInResponse), false
            ];
            yield return
            [
                "EnableSampling", (Action<PocoModel, byte?>)((r, v) => r.EnableSampling = v),
                (Func<EngineModel, bool>)(m => m.Flags.EnableSampling), false
            ];
        }

        [Theory]
        [MemberData(nameof(BooleanFlagFields))]
        public void BooleanFlagIsTrueOnlyWhenTheRecordValueIsExactlyOne(string fieldName,
            Action<PocoModel, byte?> setValue, Func<EngineModel, bool> getFlag, bool defaultWhenNull)
        {
            foreach (var (input, expected) in new[]
                     {
                         (null, defaultWhenNull), ((byte?)1, true), ((byte?)0, false), ((byte?)2, false)
                     })
            {
                var record = NewRecord();
                setValue(record, input);
                var model = NewModel();

                Map(record, model);

                getFlag(model).Should().Be(expected, $"{fieldName} was set to {input?.ToString() ?? "null"}");
            }
        }

        public static IEnumerable<object[]> CharDefaultFields()
        {
            yield return
            [
                "MaxResponseElevationInterval",
                (Action<PocoModel, char?>)((r, v) => r.MaxResponseElevationInterval = v),
                (Func<EngineModel, char>)(m => m.Counters.MaxResponseElevationInterval), 'n', 'w'
            ];
            yield return
            [
                "CacheTtlInterval", (Action<PocoModel, char?>)((r, v) => r.CacheTtlInterval = v),
                (Func<EngineModel, char>)(m => m.Cache.CacheTtlInterval), 'd', 'h'
            ];
            yield return
            [
                "MaxActivationWatcherInterval",
                (Action<PocoModel, char?>)((r, v) => r.MaxActivationWatcherInterval = v),
                (Func<EngineModel, char>)(m => m.Counters.MaxActivationWatcherInterval), 'n', 'd'
            ];
        }

        [Theory]
        [MemberData(nameof(CharDefaultFields))]
        public void CharFieldFallsBackToItsDocumentedDefaultWhenNull(string fieldName,
            Action<PocoModel, char?> setValue, Func<EngineModel, char> getValue, char expectedDefault,
            char sampleValue)
        {
            var recordNull = NewRecord();
            setValue(recordNull, null);
            var modelNull = NewModel();
            Map(recordNull, modelNull);
            getValue(modelNull).Should().Be(expectedDefault, $"{fieldName} should default when null");

            var recordValue = NewRecord();
            setValue(recordValue, sampleValue);
            var modelValue = NewModel();
            Map(recordValue, modelValue);
            getValue(modelValue).Should().Be(sampleValue, $"{fieldName} should be copied when present");
        }

        public static IEnumerable<object[]> IntDefaultFields()
        {
            yield return
            [
                "ReferenceDatePayloadLocationTypeId",
                (Action<PocoModel, int?>)((r, v) => r.ReferenceDatePayloadLocationTypeId = (byte?)v),
                (Func<EngineModel, int>)(m => m.References.ReferenceDatePayloadLocationTypeId), 1, 5
            ];
            yield return
            [
                "CacheFetchLimit", (Action<PocoModel, int?>)((r, v) => r.CacheFetchLimit = v),
                (Func<EngineModel, int>)(m => m.Cache.CacheTtlLimit), 100, 250
            ];
            yield return
            [
                "MaxResponseElevationValue", (Action<PocoModel, int?>)((r, v) => r.MaxResponseElevationValue = v),
                (Func<EngineModel, int>)(m => m.Counters.MaxResponseElevationValue), 0, 9
            ];
            yield return
            [
                "CacheTtlIntervalValue", (Action<PocoModel, int?>)((r, v) => r.CacheTtlIntervalValue = v),
                (Func<EngineModel, int>)(m => m.Cache.CacheTtlIntervalValue), 3, 12
            ];
            yield return
            [
                "MaxResponseElevationThreshold",
                (Action<PocoModel, int?>)((r, v) => r.MaxResponseElevationThreshold = v),
                (Func<EngineModel, int>)(m => m.Counters.MaxResponseElevationThreshold), 0, 42
            ];
        }

        [Theory]
        [MemberData(nameof(IntDefaultFields))]
        public void IntFieldFallsBackToItsDocumentedDefaultWhenNull(string fieldName,
            Action<PocoModel, int?> setValue, Func<EngineModel, int> getValue, int expectedDefault, int sampleValue)
        {
            var recordNull = NewRecord();
            setValue(recordNull, null);
            var modelNull = NewModel();
            Map(recordNull, modelNull);
            getValue(modelNull).Should().Be(expectedDefault, $"{fieldName} should default when null");

            var recordValue = NewRecord();
            setValue(recordValue, sampleValue);
            var modelValue = NewModel();
            Map(recordValue, modelValue);
            getValue(modelValue).Should().Be(sampleValue, $"{fieldName} should be copied when present");
        }

        public static IEnumerable<object[]> DoubleDefaultFields()
        {
            yield return
            [
                "MaxResponseElevation", (Action<PocoModel, double?>)((r, v) => r.MaxResponseElevation = v),
                (Func<EngineModel, double>)(m => m.Counters.MaxResponseElevation), 0d, 3.5d
            ];
            yield return
            [
                "ActivationWatcherSample", (Action<PocoModel, double?>)((r, v) => r.ActivationWatcherSample = v),
                (Func<EngineModel, double>)(m => m.Counters.ActivationWatcherSample), 0d, 0.75d
            ];
            yield return
            [
                "MaxActivationWatcherThreshold",
                (Action<PocoModel, double?>)((r, v) => r.MaxActivationWatcherThreshold = (int?)v),
                (Func<EngineModel, double>)(m => m.Counters.MaxActivationWatcherThreshold), 0d, 17d
            ];
        }

        [Theory]
        [MemberData(nameof(DoubleDefaultFields))]
        public void DoubleFieldFallsBackToItsDocumentedDefaultWhenNull(string fieldName,
            Action<PocoModel, double?> setValue, Func<EngineModel, double> getValue, double expectedDefault,
            double sampleValue)
        {
            var recordNull = NewRecord();
            setValue(recordNull, null);
            var modelNull = NewModel();
            Map(recordNull, modelNull);
            getValue(modelNull).Should().Be(expectedDefault, $"{fieldName} should default when null");

            var recordValue = NewRecord();
            setValue(recordValue, sampleValue);
            var modelValue = NewModel();
            Map(recordValue, modelValue);
            getValue(modelValue).Should().Be(sampleValue, $"{fieldName} should be copied when present");
        }

        [Fact]
        public void ImplicitAsyncTimeoutMillisecondsPassesThroughIncludingNull()
        {
            var recordWithValue = NewRecord();
            recordWithValue.ImplicitAsyncTimeoutMilliseconds = 5000;
            var modelWithValue = NewModel();
            Map(recordWithValue, modelWithValue);
            modelWithValue.Flags.ImplicitAsyncTimeoutMilliseconds.Should().Be(5000);

            var recordWithNull = NewRecord();
            recordWithNull.ImplicitAsyncTimeoutMilliseconds = null;
            var modelWithNull = NewModel();
            Map(recordWithNull, modelWithNull);
            modelWithNull.Flags.ImplicitAsyncTimeoutMilliseconds.Should().BeNull();
        }

        [Fact]
        public void LogsWarnThresholdMillisecondsPassesThroughIncludingNull()
        {
            var recordWithValue = NewRecord();
            recordWithValue.LogsWarnThresholdMilliseconds = 250;
            var modelWithValue = NewModel();
            Map(recordWithValue, modelWithValue);
            modelWithValue.Flags.LogsWarnThresholdMilliseconds.Should().Be(250);

            var recordWithNull = NewRecord();
            recordWithNull.LogsWarnThresholdMilliseconds = null;
            var modelWithNull = NewModel();
            Map(recordWithNull, modelWithNull);
            modelWithNull.Flags.LogsWarnThresholdMilliseconds.Should().BeNull();
        }

        [Fact]
        public void SamplePercentagePassesThroughIncludingNull()
        {
            var recordWithValue = NewRecord();
            recordWithValue.SamplePercentage = 42.5;
            var modelWithValue = NewModel();
            Map(recordWithValue, modelWithValue);
            modelWithValue.Flags.SamplePercentage.Should().Be(42.5);

            var recordWithNull = NewRecord();
            recordWithNull.SamplePercentage = null;
            var modelWithNull = NewModel();
            Map(recordWithNull, modelWithNull);
            modelWithNull.Flags.SamplePercentage.Should().BeNull();
        }

        [Fact]
        public void GuidIsAssignedWhenTheRecordGuidIsNotEmpty()
        {
            var record = NewRecord();
            var expected = Guid.NewGuid();
            record.Guid = expected;
            var model = NewModel();

            Map(record, model);

            model.Instance.Guid.Should().Be(expected);
        }

        [Fact]
        public void GuidIsLeftUnchangedWhenTheRecordGuidIsEmpty()
        {
            var record = NewRecord();
            record.Guid = Guid.Empty;
            var model = NewModel();
            var preExisting = Guid.NewGuid();
            model.Instance.Guid = preExisting;

            Map(record, model);

            model.Instance.Guid.Should().Be(preExisting,
                "an empty Guid on the record must not clobber a previously synced Guid");
        }

        [Fact]
        public void TenantRegistryIdIsAssignedWhenPresent()
        {
            var record = NewRecord();
            record.TenantRegistryId = 99;
            var model = NewModel();

            Map(record, model);

            model.Instance.TenantRegistryId.Should().Be(99);
        }

        [Fact]
        public void TenantRegistryIdIsLeftUnchangedWhenTheRecordValueIsNull()
        {
            var record = NewRecord();
            record.TenantRegistryId = null;
            var model = NewModel();
            model.Instance.TenantRegistryId = 55;

            Map(record, model);

            model.Instance.TenantRegistryId.Should().Be(55,
                "there is no safe default tenant to fall back to, so a null value must leave the model's " +
                "previously synced TenantRegistryId untouched rather than resetting it");
        }

        [Fact]
        public void TenantRegistryIdBeingNullIsLoggedAsAnErrorNotJustADebugMessage()
        {
            var record = NewRecord();
            record.TenantRegistryId = null;
            var model = NewModel();
            var log = new TestLog();

            EntityAnalysisModelRecordMapper.MapRecordFields(record, model, log);

            log.Entries.Should().Contain(e =>
                e.Level == "ERROR" && e.Message.Contains("Tenant Registry ID is empty"));
        }

        [Fact]
        public void MaxActivationWatcherValueResetsToZeroWhenTheRecordValueIsNull()
        {
            var record = NewRecord();
            record.MaxActivationWatcherValue = null;
            var model = NewModel();
            model.Counters.MaxActivationWatcherValue = 123;

            Map(record, model);

            model.Counters.MaxActivationWatcherValue.Should().Be(0);
        }

        [Fact]
        public void MaxActivationWatcherValueIsReplacedNotAccumulatedAcrossRepeatedSyncs()
        {
            var record = NewRecord();
            record.MaxActivationWatcherValue = 10;
            var model = NewModel();

            Map(record, model);
            model.Counters.MaxActivationWatcherValue.Should().Be(10);

            Map(record, model);

            model.Counters.MaxActivationWatcherValue.Should().Be(10,
                "a second sync with the SAME configured value of 10 must leave the counter at 10, matching " +
                "the DB, not accumulate it further");
        }

        [Fact]
        public void MaxActivationWatcherValueTracksTheLatestDatabaseValueAcrossSyncsRatherThanTheFirst()
        {
            var model = NewModel();

            var first = NewRecord();
            first.MaxActivationWatcherValue = 10;
            Map(first, model);
            model.Counters.MaxActivationWatcherValue.Should().Be(10);

            var second = NewRecord();
            second.MaxActivationWatcherValue = 4;
            Map(second, model);

            model.Counters.MaxActivationWatcherValue.Should().Be(4,
                "a later sync with a changed configured value must replace the counter, not add to it");
        }

        [Fact]
        public void FullyPopulatedRecordMapsEveryFieldOntoItsCorrespondingContextProperty()
        {
            var guid = Guid.NewGuid();
            var record = new PocoModel
            {
                Id = 5,
                Active = 1,
                Name = "My Model",
                EntryXPath = "$.entry",
                ReferenceDateXPath = "$.date",
                EntryName = "EntryFieldName",
                ReferenceDateName = "ReferenceDateFieldName",
                ReferenceDatePayloadLocationTypeId = 2,
                EnableCache = 1,
                EnableActivationWatcher = 1,
                EnableResponseElevationLimit = 1,
                EnableImplicitAsync = 1,
                ImplicitAsyncTimeoutMilliseconds = 1500,
                EnableTrace = 1,
                EnableLogs = 1,
                EnableLogsInfo = 1,
                EnableLogsWarnThreshold = 1,
                LogsWarnThresholdMilliseconds = 750,
                EnableLogsInResponse = 1,
                EnableSampling = 1,
                SamplePercentage = 33.3,
                EnableTtlCounter = 1,
                EnableSanctionCache = 1,
                CacheFetchLimit = 200,
                MaxResponseElevation = 12.5,
                Guid = guid,
                TenantRegistryId = 9,
                MaxResponseElevationInterval = 'h',
                MaxResponseElevationValue = 4,
                CacheTtlInterval = 'w',
                CacheTtlIntervalValue = 6,
                MaxResponseElevationThreshold = 8,
                MaxActivationWatcherInterval = 'm',
                MaxActivationWatcherValue = 11,
                MaxActivationWatcherThreshold = 13,
                ActivationWatcherSample = 0.9,
                EnableActivationArchive = 1,
                EnableRdbmsArchive = 0
            };
            var model = NewModel(record.Id);

            Map(record, model);

            using var scope = new AssertionScope();
            model.Instance.Name.Should().Be("My_Model");
            model.References.EntryXPath.Should().Be("$.entry");
            model.References.ReferenceDateXpath.Should().Be("$.date");
            model.References.EntryName.Should().Be("EntryFieldName");
            model.References.ReferenceDateName.Should().Be("ReferenceDateFieldName");
            model.References.ReferenceDatePayloadLocationTypeId.Should().Be(2);
            model.Flags.EnableCache.Should().BeTrue();
            model.Flags.EnableActivationWatcher.Should().BeTrue();
            model.Flags.EnableResponseElevationLimit.Should().BeTrue();
            model.Flags.EnableImplicitAsync.Should().BeTrue();
            model.Flags.ImplicitAsyncTimeoutMilliseconds.Should().Be(1500);
            model.Flags.EnableTrace.Should().BeTrue();
            model.Flags.EnableLogs.Should().BeTrue();
            model.Flags.EnableLogsInfo.Should().BeTrue();
            model.Flags.EnableLogsWarnThreshold.Should().BeTrue();
            model.Flags.LogsWarnThresholdMilliseconds.Should().Be(750);
            model.Flags.EnableLogsInResponse.Should().BeTrue();
            model.Flags.EnableSampling.Should().BeTrue();
            model.Flags.SamplePercentage.Should().Be(33.3);
            model.Flags.EnableTtlCounter.Should().BeTrue();
            model.Flags.EnableSanctionCache.Should().BeTrue();
            model.Cache.CacheTtlLimit.Should().Be(200);
            model.Counters.MaxResponseElevation.Should().Be(12.5);
            model.Instance.Guid.Should().Be(guid);
            model.Instance.TenantRegistryId.Should().Be(9);
            model.Counters.MaxResponseElevationInterval.Should().Be('h');
            model.Counters.MaxResponseElevationValue.Should().Be(4);
            model.Cache.CacheTtlInterval.Should().Be('w');
            model.Cache.CacheTtlIntervalValue.Should().Be(6);
            model.Counters.MaxResponseElevationThreshold.Should().Be(8);
            model.Counters.MaxActivationWatcherInterval.Should().Be('m');
            model.Counters.MaxActivationWatcherValue.Should().Be(11);
            model.Counters.MaxActivationWatcherThreshold.Should().Be(13);
            model.Counters.ActivationWatcherSample.Should().Be(0.9);
            model.Flags.EnableActivationArchive.Should().BeTrue();
            model.Flags.EnableRdbmsArchive.Should().BeFalse();
        }

        [Fact]
        public void CreateEntityAnalysisModelWiresIdentityFromRecordAndContext()
        {
            var context = NewContext();
            var record = NewRecord();
            record.Id = 42;

            var model = EntityAnalysisModelRecordMapper.CreateEntityAnalysisModel(context, record);

            model.Instance.Id.Should().Be(42);
            model.Instance.EntityAnalysisInstanceGuid.Should()
                .Be(context.EntityAnalysisModels.EntityAnalysisInstanceGuid);
            model.JsonSerializationHelper.Should().BeSameAs(context.JsonSerializationHelper);
        }

        [Fact]
        public void CreateEntityAnalysisModelWiresSharedServicesByReference()
        {
            var context = NewContext();
            var record = NewRecord();

            var model = EntityAnalysisModelRecordMapper.CreateEntityAnalysisModel(context, record);

            model.Services.Log.Should().BeSameAs(context.Services.Log);
            model.Services.JubeEnvironment.Should().BeSameAs(context.Services.DynamicEnvironment);
            model.Services.AesEncryption.Should().NotBeNull();
            model.Services.AesEncryption.IvMode.Should().Be(IvMode.Deterministic);
        }

        [Fact]
        public void CreateEntityAnalysisModelWiresSharedContextCollectionsByReferenceSoLaterUpdatesArePropagated()
        {
            var context = NewContext();
            var record = NewRecord();

            var model = EntityAnalysisModelRecordMapper.CreateEntityAnalysisModel(context, record);

            using var scope = new AssertionScope();
            model.Dependencies.ActiveEntityAnalysisModels.Should()
                .BeSameAs(context.EntityAnalysisModels.ActiveEntityAnalysisModels);
            model.Dependencies.SanctionsEntries.Should().BeSameAs(context.EntityAnalysisModels.SanctionsEntries);
            model.Dependencies.SanctionsStopTokens.Should()
                .BeSameAs(context.EntityAnalysisModels.SanctionsStopTokens);
            model.Dependencies.EntityAnalysisModelLists.Should()
                .BeSameAs(context.EntityAnalysisModels.EntityAnalysisModelLists);
            model.Dependencies.KvpDictionaries.Should().BeSameAs(context.EntityAnalysisModels.KvpDictionaries);
            model.Dependencies.EntityAnalysisModelSuppressionModels.Should()
                .BeSameAs(context.EntityAnalysisModels.EntityAnalysisModelSuppressionModels);
            model.Dependencies.EntityAnalysisModelSuppressionRules.Should()
                .BeSameAs(context.EntityAnalysisModels.EntityAnalysisModelSuppressionRules);

            model.ConcurrentQueues.PersistToActivationWatcherAsync.Should()
                .BeSameAs(context.ConcurrentQueues.PersistToActivationWatcherAsync);
            model.ConcurrentQueues.PendingTagging.Should().BeSameAs(context.ConcurrentQueues.PendingTagging);
            model.ConcurrentQueues.PendingNotifications.Should()
                .BeSameAs(context.ConcurrentQueues.PendingNotifications);
            model.ConcurrentQueues.Callbacks.Should().BeSameAs(context.ConcurrentQueues.Callbacks);
            model.ConcurrentQueues.PendingEntityInvoke.Should()
                .BeSameAs(context.ConcurrentQueues.PendingEntityInvoke);
        }

        [Fact]
        public void CreateEntityAnalysisModelPopulatesArchiverBuffersPerArchiverPersistThreadsSetting()
        {
            var context = NewContext();
            var record = NewRecord();

            var model = EntityAnalysisModelRecordMapper.CreateEntityAnalysisModel(context, record);

            var expectedThreads = int.Parse(context.Services.DynamicEnvironment.AppSettings("ArchiverPersistThreads"));
            model.Dependencies.BulkInsertMessageBuffers.Should().HaveCount(expectedThreads);
        }

        [Fact]
        public void CreateEntityAnalysisModelReturnsAFreshInstanceForEachCall()
        {
            var context = NewContext();

            var first = EntityAnalysisModelRecordMapper.CreateEntityAnalysisModel(context, NewRecord());
            var second = EntityAnalysisModelRecordMapper.CreateEntityAnalysisModel(context, NewRecord());

            first.Should().NotBeSameAs(second);
        }
    }
}