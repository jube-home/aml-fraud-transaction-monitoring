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
using Xunit;

namespace Jube.Test.Dictionary
{
    [Trait("Category", "Unit")]
    public sealed class PooledDictionaryTests
    {
        [Fact]
        public void ANewDictionaryWithTheDefaultConstructorIsEmpty()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            using var dictionary = new PooledDictionary<string, int>();

            dictionary.Count.Should().Be(0);
            dictionary.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void AddThenTryGetValueRoundTripsTheValue()
        {
            using var dictionary = new PooledDictionary<string, int>();

            dictionary.Add("A", 1);

            dictionary.TryGetValue("A", out var value).Should().BeTrue();
            value.Should().Be(1);
        }

        [Fact]
        public void AddThrowsForANullKey()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            using var dictionary = new PooledDictionary<string, int>();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => dictionary.Add(null!, 1);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddThrowsWhenTheKeyAlreadyExists()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);

            // ReSharper disable once AccessToDisposedClosure
            var act = () => dictionary.Add("A", 2);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryAddReturnsTrueForANewKeyAndFalseWithoutThrowingForAnExistingOne()
        {
            using var dictionary = new PooledDictionary<string, int>();

            dictionary.TryAdd("A", 1).Should().BeTrue();
            dictionary.TryAdd("A", 2).Should().BeFalse();

            dictionary["A"].Should().Be(1, "TryAdd must not overwrite an existing value on failure");
        }

        [Fact]
        public void TryAddThrowsForANullKeyJustLikeAdd()
        {
            using var dictionary = new PooledDictionary<string, int>();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => dictionary.TryAdd(null!, 1);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ContainsKeyDistinguishesPresentFromAbsent()
        {
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);

            dictionary.ContainsKey("A").Should().BeTrue();
            dictionary.ContainsKey("B").Should().BeFalse();
        }

        [Fact]
        public void TheIndexerGetterReturnsTheDefaultValueForAMissingKeyRatherThanThrowing()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            using var dictionary = new PooledDictionary<string, int>();

            dictionary["missing"].Should().Be(0);
        }

        [Fact]
        public void TheIndexerSetterAddsWhenTheKeyIsAbsentAndOverwritesWhenPresent()
        {
            using var dictionary = new PooledDictionary<string, int>();

            dictionary["A"] = 1;
            dictionary["A"].Should().Be(1);

            dictionary["A"] = 2;
            dictionary["A"].Should().Be(2);
            dictionary.Count.Should().Be(1);
        }

        [Fact]
        public void GetValueOrThrowReturnsTheValueWhenPresent()
        {
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);

            dictionary.GetValueOrThrow("A").Should().Be(1);
        }

        [Fact]
        public void GetValueOrThrowThrowsKeyNotFoundWhenAbsent()
        {
            using var dictionary = new PooledDictionary<string, int>();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => dictionary.GetValueOrThrow("missing");

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void RemoveDeletesAnExistingEntryAndReturnsTrue()
        {
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);

            dictionary.Remove("A").Should().BeTrue();

            dictionary.Count.Should().Be(0);
            dictionary.ContainsKey("A").Should().BeFalse();
        }

        [Fact]
        public void RemoveOfAMissingKeyReturnsFalse()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            using var dictionary = new PooledDictionary<string, int>();

            dictionary.Remove("missing").Should().BeFalse();
        }

        [Fact]
        public void ClearEmptiesTheDictionaryCompletely()
        {
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            dictionary.Clear();

            dictionary.Count.Should().Be(0);
            dictionary.ContainsKey("A").Should().BeFalse();
            dictionary.Keys.Should().BeEmpty();
        }

        [Fact]
        public void KeysAndValuesOnlyReflectOccupiedSlots()
        {
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);
            dictionary.Remove("A");

            dictionary.Keys.Should().BeEquivalentTo("B");
            dictionary.Values.Should().BeEquivalentTo([2]);
        }

        [Fact]
        public void TheGenericEnumeratorYieldsEveryStoredPair()
        {
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            dictionary.Should().BeEquivalentTo(new[]
            {
                new KeyValuePair<string, int>("A", 1),
                new KeyValuePair<string, int>("B", 2)
            });
        }

        [Fact]
        public void TheNonGenericEnumeratorIsAlsoUsable()
        {
            using var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);

            IEnumerable nonGeneric = dictionary;
            var count = nonGeneric.Cast<object>().Count();

            count.Should().Be(1);
        }

        [Fact]
        public void TheExplicitCollectionAddContainsRemoveAndCopyToWorkViaTheInterface()
        {
            ICollection<KeyValuePair<string, int>> dictionary = new PooledDictionary<string, int>();
            dictionary.Add(new KeyValuePair<string, int>("A", 1));

            dictionary.Contains(new KeyValuePair<string, int>("A", 1)).Should().BeTrue();
            dictionary.Contains(new KeyValuePair<string, int>("A", 2)).Should().BeFalse();

            var array = new KeyValuePair<string, int>[1];
            dictionary.CopyTo(array, 0);
            array[0].Key.Should().Be("A");

            dictionary.Remove(new KeyValuePair<string, int>("A", 1)).Should().BeTrue();
            dictionary.Should().BeEmpty();

            ((IDisposable)dictionary).Dispose();
        }

        [Fact]
        public void TheDictionaryGrowsBeyondItsDefaultInitialCapacityOfSixteen()
        {
            using var dictionary = new PooledDictionary<string, int>();

            for (var i = 0; i < 100; i++)
            {
                dictionary.Add($"key{i}", i);
            }

            dictionary.Count.Should().Be(100);
            for (var i = 0; i < 100; i++)
            {
                dictionary[$"key{i}"].Should().Be(i);
            }
        }

        [Fact]
        public void AnExplicitInitialCapacityOfAtLeastEightGrowsByOneSlotAtATimeButStaysCorrect()
        {
            using var dictionary = new PooledDictionary<string, int>(8);

            for (var i = 0; i < 50; i++)
            {
                dictionary.Add($"key{i}", i);
            }

            dictionary.Count.Should().Be(50);
            for (var i = 0; i < 50; i++)
            {
                dictionary[$"key{i}"].Should().Be(i);
            }
        }

        [Fact]
        public void AnExplicitInitialCapacityBelowEightDoublesOnResizeLikeTheDefaultConstructor()
        {
            using var dictionary = new PooledDictionary<string, int>(2);

            for (var i = 0; i < 20; i++)
            {
                dictionary.Add($"key{i}", i);
            }

            dictionary.Count.Should().Be(20);
            dictionary["key19"].Should().Be(19);
        }

        [Fact]
        public void AnExplicitInitialCapacityOfZeroIsClampedToOneAndTheDictionaryGrowsNormally()
        {
            using var dictionary = new PooledDictionary<string, int>(0);

            dictionary.Add("A", 1);
            dictionary.Add("B", 2);

            dictionary.Count.Should().Be(2);
            dictionary["A"].Should().Be(1);
            dictionary["B"].Should().Be(2);
        }

        [Fact]
        public void HashCodesThatCollideWithTheEmptySlotSentinelAreStillFoundAfterBeingAdded()
        {
            using var dictionary = new PooledDictionary<AlwaysZeroHashKey, string>();
            var key = new AlwaysZeroHashKey("only-key");

            dictionary.Add(key, "value");

            dictionary.Count.Should().Be(1);
            dictionary.ContainsKey(key).Should().BeTrue();
            dictionary.TryGetValue(key, out var value).Should().BeTrue();
            value.Should().Be("value");
        }

        [Fact]
        public void TwoDistinctKeysThatBothCollideWithTheEmptySlotSentinelAreBothFoundIndependently()
        {
            using var dictionary = new PooledDictionary<AlwaysZeroHashKey, string>();
            var zeroHashKey = new AlwaysZeroHashKey("zero");

            dictionary.Add(zeroHashKey, "zero-value");
            dictionary.Add(new AlwaysZeroHashKey("also-zero"), "also-zero-value");

            dictionary.Count.Should().Be(2);
            dictionary[zeroHashKey].Should().Be("zero-value");
            dictionary[new AlwaysZeroHashKey("also-zero")].Should().Be("also-zero-value");
        }

        [Fact]
        public void DisposeReturnsTheRentedArraysWithoutThrowing()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            var dictionary = new PooledDictionary<string, int>();
            dictionary.Add("A", 1);

            var act = dictionary.Dispose;

            act.Should().NotThrow();
        }

        [Fact]
        public void ANonStringKeyTypeWorksJustAsWellAsAStringKey()
        {
            using var dictionary = new PooledDictionary<int, string>();

            dictionary.Add(1, "one");
            dictionary.Add(2, "two");

            dictionary[1].Should().Be("one");
            dictionary.Remove(2).Should().BeTrue();
            dictionary.Count.Should().Be(1);
        }

        private sealed class AlwaysZeroHashKey(string label) : IEquatable<AlwaysZeroHashKey>
        {
            private readonly string label = label;

            public bool Equals(AlwaysZeroHashKey? other)
            {
                return other is not null && label == other.label;
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as AlwaysZeroHashKey);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }
    }
}