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
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class DictionaryKvpExtensionsTests
    {
        private static EntityAnalysisModelInstanceEntryPayload NewPayload()
        {
            return new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = new DictionaryNoBoxing<string>(),
                Dictionary = new PooledDictionary<string, double>(),
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
            };
        }

        private static EntityAnalysisModelDictionary NewKvpDictionary(string name, string dataName,
            params (string Key, double Value)[] entries)
        {
            var dictionary = new EntityAnalysisModelDictionary
            {
                Name = name,
                DataName = dataName
            };
            foreach (var (key, value) in entries)
            {
                dictionary.KvPs[key] = value;
            }

            return dictionary;
        }

        [Fact]
        public void ResolvesTheConfiguredValueFromThePayloadIntoTheDictionaryCache()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Dependencies.KvpDictionaries[1] =
                NewKvpDictionary("CountryRisk", "Country", ("AE", 10), ("GB", 1));
            var payload = NewPayload();
            payload.Payload.Add("Country", "AE");

            entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "Country");

            payload.Dictionary["CountryRisk"].Should().Be(10);
        }

        [Fact]
        public void FieldNamesAreMatchedAgainstDataNameNotTheDictionarysOwnName()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Dependencies.KvpDictionaries[1] =
                NewKvpDictionary("CountryRisk", "Country", ("AE", 10));
            var payload = NewPayload();
            payload.Payload.Add("Country", "AE");

            entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "CountryRisk");

            payload.Dictionary.ContainsKey("CountryRisk").Should().BeFalse();
        }

        [Fact]
        public void AKeyNotPresentInTheLookupTableResolvesToZero()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Dependencies.KvpDictionaries[1] =
                NewKvpDictionary("CountryRisk", "Country", ("AE", 10));
            var payload = NewPayload();
            payload.Payload.Add("Country", "ZZ");

            entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "Country");

            payload.Dictionary["CountryRisk"].Should().Be(0);
        }

        [Fact]
        public void AFieldMissingFromThePayloadResolvesToZero()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Dependencies.KvpDictionaries[1] =
                NewKvpDictionary("CountryRisk", "Country", ("AE", 10));
            var payload = NewPayload();

            entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "Country");

            payload.Dictionary["CountryRisk"].Should().Be(0);
        }

        [Fact]
        public void DoesNotOverwriteAnAlreadyResolvedValueForTheSameDictionaryName()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Dependencies.KvpDictionaries[1] =
                NewKvpDictionary("CountryRisk", "Country", ("AE", 10));
            var payload = NewPayload();
            payload.Payload.Add("Country", "AE");
            payload.Dictionary.Add("CountryRisk", 999);

            entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "Country");

            payload.Dictionary["CountryRisk"].Should().Be(999);
        }

        [Fact]
        public void MultipleDictionariesMappedToTheSameFieldAreAllResolvedIndependently()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Dependencies.KvpDictionaries[1] =
                NewKvpDictionary("CountryRisk", "Country", ("AE", 10));
            entityAnalysisModel.Dependencies.KvpDictionaries[2] =
                NewKvpDictionary("CountryRegion", "Country", ("AE", 2));
            var payload = NewPayload();
            payload.Payload.Add("Country", "AE");

            entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "Country");

            payload.Dictionary["CountryRisk"].Should().Be(10);
            payload.Dictionary["CountryRegion"].Should().Be(2);
        }

        [Fact]
        public void ADictionaryMappedToADifferentFieldIsUnaffectedByAnUnrelatedFieldResolution()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Dependencies.KvpDictionaries[1] =
                NewKvpDictionary("CountryRisk", "Country", ("AE", 10));
            var payload = NewPayload();
            payload.Payload.Add("Country", "AE");

            entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "UnrelatedField");

            payload.Dictionary.Should().BeEmpty();
        }

        [Fact]
        public void WithNoKvpDictionariesConfiguredNothingThrowsAndTheDictionaryStaysEmpty()
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            var payload = NewPayload();
            payload.Payload.Add("Country", "AE");

            var act = () => entityAnalysisModel.ResolveDictionaryValueForField(payload, TestLog.NoOp, "Country");

            act.Should().NotThrow();
            payload.Dictionary.Should().BeEmpty();
        }
    }
}