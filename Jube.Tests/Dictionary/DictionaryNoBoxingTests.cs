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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Dictionary.Interfaces;
using Jube.Dictionary.Models;
using Xunit;

namespace Jube.Test.Dictionary
{
    [Trait("Category", "Unit")]
    public sealed class DictionaryNoBoxingTests
    {
        [Fact]
        public void ANewDictionaryIsEmpty()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            dictionary.Count.Should().Be(0);
            dictionary.EstimatedSizeBytes().Should().Be(0);
        }

        [Fact]
        public void TheIndexerReturnsADefaultInternalValueForAMissingKeyRatherThanThrowing()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            var value = dictionary["missing"];

            value.Type.Should().Be(InternalValue.ValueType.None);
        }

        [Fact]
        public void TheIndexerSetterAddsANewKeyAndTheGetterReadsItBack()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            dictionary["Amount"] = new InternalValue(100.5d);

            dictionary["Amount"].AsDouble().Should().Be(100.5d);
            dictionary.Count.Should().Be(1);
        }

        [Fact]
        public void TheIndexerSetterUpdatesAnExistingKeyInPlaceWithoutChangingCount()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary["Amount"] = new InternalValue(100.5d);

            dictionary["Amount"] = new InternalValue(200.5d);

            dictionary.Count.Should().Be(1);
            dictionary["Amount"].AsDouble().Should().Be(200.5d);
        }

        [Fact]
        public void TheIndexerSetterAdjustsTheEstimatedSizeWhenReplacingAValue()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary["Name"] = new InternalValue("short");
            var afterShort = dictionary.EstimatedSizeBytes();

            dictionary["Name"] = new InternalValue("a much longer replacement value");

            dictionary.EstimatedSizeBytes().Should().BeGreaterThan(afterShort);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryAddOnlySucceedsTheFirstTimeForAGivenKey(bool useSameValueTwice)
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            var first = dictionary.TryAdd("Country", "AE");
            var second = dictionary.TryAdd("Country", useSameValueTwice ? "AE" : "GB");

            first.Should().BeTrue();
            second.Should().BeFalse("TryAdd must not overwrite an existing key");
            dictionary["Country"].AsString().Should().Be("AE");
            dictionary.Count.Should().Be(1);
        }

        [Fact]
        public void TryAddSupportsEveryPrimitiveOverload()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            var dateTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var guid = Guid.NewGuid();

            dictionary.TryAdd("s", "value").Should().BeTrue();
            dictionary.TryAdd("d", 3.14).Should().BeTrue();
            dictionary.TryAdd("b", true).Should().BeTrue();
            dictionary.TryAdd("dt", dateTime).Should().BeTrue();
            dictionary.TryAdd("i", 42).Should().BeTrue();
            dictionary.TryAdd("g", guid).Should().BeTrue();

            dictionary["s"].AsString().Should().Be("value");
            dictionary["d"].AsDouble().Should().Be(3.14);
            dictionary["b"].AsBool().Should().BeTrue();
            dictionary["dt"].AsDateTime().Should().Be(dateTime);
            dictionary["i"].AsInt().Should().Be(42);
            dictionary["g"].AsGuid().Should().Be(guid);
            dictionary.Count.Should().Be(6);
        }

        [Fact]
        public void TryAddInternsNonLiteralStringKeysSoRepeatedLookupsShareOneStringInstance()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            var built = new string(['K', 'e', 'y', 'A']);
            string.IsInterned(built).Should()
                .BeNull("the runtime should not already have this freshly built string interned");

            dictionary.TryAdd(built, "value").Should().BeTrue();

            string.IsInterned(built).Should()
                .NotBeNull("TryAdd interns the key so future comparisons are fast reference comparisons");
        }

        [Fact]
        public void AddDoesNotInternTheKeyUnlikeTryAdd()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            var built = new string(['K', 'e', 'y', 'B']);
            string.IsInterned(built).Should().BeNull();

            dictionary.Add(built, "value");

            string.IsInterned(built).Should().BeNull("the plain Add path does not intern keys, only TryAdd does");
        }

        [Fact]
        public void ContainsKeyReflectsPresenceOfTheKeyOnly()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("Country", "AE");

            dictionary.ContainsKey("Country").Should().BeTrue();
            dictionary.ContainsKey("Missing").Should().BeFalse();
        }

        [Fact]
        public void RemoveDeletesAnExistingKeyAndReturnsTrue()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            var removed = dictionary.Remove("A");

            removed.Should().BeTrue();
            dictionary.Count.Should().Be(1);
            dictionary.ContainsKey("A").Should().BeFalse();
            dictionary["B"].AsInt().Should().Be(2);
        }

        [Fact]
        public void RemoveOfAMissingKeyReturnsFalseAndLeavesTheDictionaryUnchanged()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);

            var removed = dictionary.Remove("Missing");

            removed.Should().BeFalse();
            dictionary.Count.Should().Be(1);
        }

        [Fact]
        public void RemoveFromTheMiddlePreservesTheRelativeOrderOfTheRemainingEntries()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Add("C", 3);

            dictionary.Remove("B");

            dictionary.Select(kv => kv.Key).Should().Equal("A", "C");
        }

        [Fact]
        public void RemoveReducesTheEstimatedSizeByTheRemovedEntriesContribution()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", "some text value");
            var sizeWithEntry = dictionary.EstimatedSizeBytes();

            dictionary.Remove("A");

            dictionary.EstimatedSizeBytes().Should().BeLessThan(sizeWithEntry);
            dictionary.EstimatedSizeBytes().Should().Be(0);
        }

        [Fact]
        public void TryGetValueReturnsTrueAndTheValueWhenPresent()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("Amount", 42.5d);

            var found = dictionary.TryGetValue("Amount", out var value);

            found.Should().BeTrue();
            value.AsDouble().Should().Be(42.5d);
        }

        [Fact]
        public void TryGetValueReturnsFalseAndADefaultValueWhenAbsent()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            var found = dictionary.TryGetValue("Missing", out var value);

            found.Should().BeFalse();
            value.Type.Should().Be(InternalValue.ValueType.None);
        }

        [Fact]
        public void ClearRemovesAllEntriesAndResetsTheEstimatedSize()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", "some text");
            dictionary.Add("B", 2);

            dictionary.Clear();

            dictionary.Count.Should().Be(0);
            dictionary.EstimatedSizeBytes().Should().Be(0);
            dictionary.ContainsKey("A").Should().BeFalse();
        }

        [Fact]
        public void ClearOnAnAlreadyEmptyDictionaryIsANoOp()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            dictionary.Clear();

            dictionary.Count.Should().Be(0);
        }

        [Fact]
        public void AddSilentlyIgnoresADuplicateKeyRatherThanThrowingOrOverwriting()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("Country", "AE");

            dictionary.Add("Country", "GB");

            dictionary.Count.Should().Be(1);
            dictionary["Country"].AsString().Should().Be("AE", "Add must not overwrite an existing key");
        }

        [Fact]
        public void AddSupportsEveryPrimitiveOverloadAndTheRawInternalValueOverload()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            var dateTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            dictionary.Add("raw", new InternalValue("x"));
            dictionary.Add("s", "value");
            dictionary.Add("d", 3.14);
            dictionary.Add("b", true);
            dictionary.Add("dt", dateTime);
            dictionary.Add("i", 42);

            dictionary.Count.Should().Be(6);
            dictionary["raw"].AsString().Should().Be("x");
            dictionary["s"].AsString().Should().Be("value");
            dictionary["d"].AsDouble().Should().Be(3.14);
            dictionary["b"].AsBool().Should().BeTrue();
            dictionary["dt"].AsDateTime().Should().Be(dateTime);
            dictionary["i"].AsInt().Should().Be(42);
        }

        [Fact]
        public void AddUncheckedAppendsEvenWhenTheKeyAlreadyExistsProducingADuplicateEntry()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("Country", "AE");

            dictionary.AddUnchecked("Country", "GB");

            dictionary.Count.Should()
                .Be(2, "AddUnchecked bypasses the duplicate-key check that Add and TryAdd enforce");
            dictionary.Select(kv => kv.Value.AsString()).Should().Equal("AE", "GB");
        }

        [Fact]
        public void AddUncheckedLookupsReturnTheFirstMatchingEntryWhenDuplicatesExist()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.AddUnchecked("Country", "AE");
            dictionary.AddUnchecked("Country", "GB");

            dictionary["Country"].AsString().Should().Be("AE");
        }

        [Fact]
        public void AddUncheckedSupportsEveryPrimitiveOverload()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            var dateTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            dictionary.AddUnchecked("raw", new InternalValue(1));
            dictionary.AddUnchecked("s", "value");
            dictionary.AddUnchecked("d", 3.14);
            dictionary.AddUnchecked("b", true);
            dictionary.AddUnchecked("dt", dateTime);
            dictionary.AddUnchecked("i", 42);

            dictionary.Count.Should().Be(6);
        }

        [Fact]
        public void TheDictionaryGrowsAutomaticallyBeyondItsDefaultInitialCapacity()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            for (var i = 0; i < 100; i++)
            {
                dictionary.Add($"key{i}", i);
            }

            dictionary.Count.Should().Be(100);
            for (var i = 0; i < 100; i++)
            {
                dictionary[$"key{i}"].AsInt().Should().Be(i);
            }
        }

        [Fact]
        public void AnExplicitInitialCapacityIsHonouredAndStillGrowsWhenExceeded()
        {
            using var dictionary = new DictionaryNoBoxing<string>(2);

            dictionary.Add("a", 1);
            dictionary.Add("b", 2);
            dictionary.Add("c", 3);

            dictionary.Count.Should().Be(3);
            dictionary["c"].AsInt().Should().Be(3);
        }

        [Fact]
        public void ForeachEnumeratesEveryEntryInInsertionOrder()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Add("C", 3);

            var seen = new List<string>();
            foreach (var kv in dictionary)
            {
                seen.Add($"{kv.Key}={kv.Value.AsInt()}");
            }

            seen.Should().Equal("A=1", "B=2", "C=3");
        }

        [Fact]
        public void TheGenericIEnumerableInterfaceIsUsableViaLinq()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            IEnumerable<KeyValuePair<string, InternalValue>> asEnumerable = dictionary;

            asEnumerable.Select(kv => kv.Key).Should().Equal("A", "B");
        }

        [Fact]
        public void TheNonGenericIEnumerableInterfaceIsAlsoImplemented()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);

            IEnumerable nonGeneric = dictionary;
            var enumerator = nonGeneric.GetEnumerator();
            using var enumerator1 = enumerator as IDisposable;

            enumerator.MoveNext().Should().BeTrue();
            var current = (KeyValuePair<string, InternalValue>)enumerator.Current!;
            current.Key.Should().Be("A");
        }

        [Fact]
        public void TheStructEnumeratorSupportsManualMoveNextCurrentAndReset()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            var enumerator = dictionary.GetEnumerator();

            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Key.Should().Be("A");
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Key.Should().Be("B");
            enumerator.MoveNext().Should().BeFalse();

            enumerator.Reset();

            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Key.Should().Be("A");
            enumerator.Dispose();
        }

        [Fact]
        public void EnumeratingAnEmptyDictionaryYieldsNoElements()
        {
            using var dictionary = new DictionaryNoBoxing<string>();

            dictionary.Should().BeEmpty();
        }

        [Fact]
        public void DisposeClearsTheDictionarySoItBehavesAsEmptyAfterwards()
        {
            var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("A", 1);

            dictionary.Dispose();

            dictionary.Count.Should().Be(0);
        }

        [Fact]
        public void EstimatedSizeBytesGrowsWithLongerStringValues()
        {
            using var shortDictionary = new DictionaryNoBoxing<string>();
            using var longDictionary = new DictionaryNoBoxing<string>();
            shortDictionary.Add("k", "a");
            longDictionary.Add("k", "a much longer piece of text than the other one");

            longDictionary.EstimatedSizeBytes().Should().BeGreaterThan(shortDictionary.EstimatedSizeBytes());
        }

        [Fact]
        public void EstimatedSizeBytesIsPositiveForEveryPrimitiveValueType()
        {
            using var dictionary = new DictionaryNoBoxing<string>();
            dictionary.Add("i", 1);
            dictionary.Add("d", 1.0d);
            dictionary.Add("b", true);
            dictionary.Add("dt", DateTime.UtcNow);

            dictionary.EstimatedSizeBytes().Should().BeGreaterThan(0);
        }

        [Fact]
        public void AnIntKeyedDictionaryWorksIdenticallyToAStringKeyedOne()
        {
            using var dictionary = new DictionaryNoBoxing<int>();

            dictionary.Add(1, "one");
            dictionary.Add(2, "two");

            dictionary[1].AsString().Should().Be("one");
            dictionary.ContainsKey(2).Should().BeTrue();
            dictionary.Remove(1).Should().BeTrue();
            dictionary.Count.Should().Be(1);
        }

        [Fact]
        public void TheDictionarySatisfiesItsPublicInterfaceContract()
        {
            IDictionaryNoBoxing<string> dictionary = new DictionaryNoBoxing<string>();

            dictionary.TryAdd("A", 1).Should().BeTrue();
            dictionary.Count.Should().Be(1);
            dictionary.ContainsKey("A").Should().BeTrue();
            dictionary.TryGetValue("A", out var value).Should().BeTrue();
            value.AsInt().Should().Be(1);
            dictionary.Remove("A").Should().BeTrue();
            dictionary.Clear();

            if (dictionary is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [Fact]
        public void TheDictionaryImplementsISizedForCacheAccounting()
        {
            ISized sized = new DictionaryNoBoxing<string>();

            sized.EstimatedSizeBytes().Should().Be(0);
        }
    }
}