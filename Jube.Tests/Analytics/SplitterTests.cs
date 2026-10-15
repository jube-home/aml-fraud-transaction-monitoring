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
using System.Linq;
using FluentAssertions;
using Jube.Analytics;
using Jube.Analytics.Models;
using Xunit;

namespace Jube.Test.Analytics
{
    public class SplitterTests
    {
        private static void AssertPartition(SplitFold[] folds, int n)
        {
            folds.SelectMany(f => f.Test).Should().OnlyHaveUniqueItems();
            folds.SelectMany(f => f.Test).OrderBy(i => i).Should().Equal(Enumerable.Range(0, n));

            foreach (var fold in folds)
            {
                fold.Train.Intersect(fold.Test).Should().BeEmpty();
                fold.Train.Concat(fold.Test).OrderBy(i => i).Should().Equal(Enumerable.Range(0, n));
            }
        }

        [Fact]
        public void ForwardChaining_KnownAnswer()
        {
            var folds = TimeSplit.ForwardChaining([.. Enumerable.Range(0, 10).Select(i => (double)i)], 3);
            folds.Length.Should().Be(3);
            folds[0].Train.Should().Equal(0, 1, 2, 3);
            folds[0].Test.Should().Equal(4, 5);
            folds[1].Train.Should().Equal(0, 1, 2, 3, 4, 5);
            folds[1].Test.Should().Equal(6, 7);
            folds[2].Train.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7);
            folds[2].Test.Should().Equal(8, 9);
        }

        [Fact]
        public void ForwardChaining_PreservesTimeOrderOnShuffledInput()
        {
            var rng = new Lcg(3);
            var times = Enumerable.Range(0, 200).Select(_ => Math.Floor(rng.Next() * 1000)).ToArray();
            var folds = TimeSplit.ForwardChaining(times, 4, 2);

            foreach (var fold in folds)
            {
                fold.Train.Intersect(fold.Test).Should().BeEmpty();
                fold.Train.Select(i => times[i]).Should().BeInAscendingOrder();
                fold.Test.Select(i => times[i]).Should().BeInAscendingOrder();
                fold.Train.Max(i => times[i]).Should().BeLessThanOrEqualTo(fold.Test.Min(i => times[i]));
            }

            for (var f = 1; f < folds.Length; f++)
            {
                folds[f].Train.Length.Should().BeGreaterThan(folds[f - 1].Train.Length);
                folds[f].Test.Min(i => times[i]).Should().BeGreaterThanOrEqualTo(folds[f - 1].Test.Max(i => times[i]));
            }

            folds.SelectMany(f => f.Test).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void ForwardChaining_GapLeavesRowsBetweenTrainAndTest()
        {
            var folds = TimeSplit.ForwardChaining([.. Enumerable.Range(0, 10).Select(i => (double)i)], 3, 1);
            folds[0].Train.Should().Equal(0, 1, 2);
            folds[0].Test.Should().Equal(4, 5);
        }

        [Fact]
        public void ForwardChaining_InvalidArgumentsThrow()
        {
            Action act = () => TimeSplit.ForwardChaining(null!, 2);
            act.Should().Throw<ArgumentNullException>();
            act = () => TimeSplit.ForwardChaining([1, 2, 3], 0);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => TimeSplit.ForwardChaining([1, 2, 3], 1, -1);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => TimeSplit.ForwardChaining([1, double.NaN, 3, 4], 1);
            act.Should().Throw<ArgumentException>();
            act = () => TimeSplit.ForwardChaining([1, 2], 3);
            act.Should().Throw<ArgumentException>();
            act = () => TimeSplit.ForwardChaining([1, 2, 3, 4], 1, 5);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GroupKFold_NoEntityInTwoFolds_AndIsAPartition()
        {
            var rng = new Lcg(11);
            var groups = Enumerable.Range(0, 500).Select(_ => "entity" + (int)(rng.Next() * 60)).ToArray();
            var folds = GroupSplit.KFold(groups, 5, 42);
            folds.Length.Should().Be(5);
            AssertPartition(folds, groups.Length);

            foreach (var fold in folds)
            {
                var testGroups = fold.Test.Select(i => groups[i]).ToHashSet();
                var trainGroups = fold.Train.Select(i => groups[i]).ToHashSet();
                testGroups.Overlaps(trainGroups).Should().BeFalse();
            }

            var owner = new Dictionary<string, int>();

            for (var f = 0; f < folds.Length; f++)
            {
                foreach (var i in folds[f].Test)
                {
                    owner.TryAdd(groups[i], f);
                    owner[groups[i]].Should().Be(f);
                }
            }
        }

        [Fact]
        public void GroupKFold_FoldSizesAreReasonablyBalanced()
        {
            var groups = Enumerable.Range(0, 1000).Select(i => i / 5).ToArray();
            var sizes = GroupSplit.KFold(groups, 4, 1).Select(f => f.Test.Length).ToArray();
            sizes.Max().Should().BeLessThanOrEqualTo(sizes.Min() + 5);
        }

        [Fact]
        public void GroupKFold_DeterministicBySeed_AndSeedChangesAssignment()
        {
            var groups = Enumerable.Range(0, 300).Select(i => i % 30).ToArray();
            var a = GroupSplit.KFold(groups, 3, 7);
            var b = GroupSplit.KFold(groups, 3, 7);
            var c = GroupSplit.KFold(groups, 3, 8);

            for (var f = 0; f < 3; f++)
            {
                a[f].Test.Should().Equal(b[f].Test);
            }

            a.Select(f => f.Test).SelectMany(t => t).Should().NotEqual(c.Select(f => f.Test).SelectMany(t => t));
        }

        [Fact]
        public void GroupKFold_InvalidArgumentsThrow()
        {
            Action act = () => GroupSplit.KFold<int>(null!, 3, 1);
            act.Should().Throw<ArgumentNullException>();
            act = () => GroupSplit.KFold([1, 2, 3], 1, 1);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => GroupSplit.KFold([1, 1, 2, 2], 3, 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GroupSplit_TrainValidationTest_NeverSharesAnEntity()
        {
            var rng = new Lcg(21);
            var groups = Enumerable.Range(0, 2000).Select(_ => (int)(rng.Next() * 200)).ToArray();
            var split = GroupSplit.Split(groups, 0.6, 0.2, 0.2, 5);

            split.Train.Concat(split.Validation).Concat(split.Test).OrderBy(i => i)
                .Should().Equal(Enumerable.Range(0, groups.Length));

            var train = split.Train.Select(i => groups[i]).ToHashSet();
            var validation = split.Validation.Select(i => groups[i]).ToHashSet();
            var test = split.Test.Select(i => groups[i]).ToHashSet();
            train.Overlaps(validation).Should().BeFalse();
            train.Overlaps(test).Should().BeFalse();
            validation.Overlaps(test).Should().BeFalse();
            (split.Train.Length / (double)groups.Length).Should().BeApproximately(0.6, 0.05);
            (split.Validation.Length / (double)groups.Length).Should().BeApproximately(0.2, 0.05);
            (split.Test.Length / (double)groups.Length).Should().BeApproximately(0.2, 0.05);
        }

        [Fact]
        public void GroupSplit_DeterministicBySeed_AndValidatesFractions()
        {
            var groups = Enumerable.Range(0, 100).Select(i => i % 17).ToArray();

            GroupSplit.Split(groups, 0.6, 0.2, 0.2, 3).Train.Should()
                .Equal(GroupSplit.Split(groups, 0.6, 0.2, 0.2, 3).Train);

            Action act = () => GroupSplit.Split(groups, 0.7, 0.2, 0.2, 3);
            act.Should().Throw<ArgumentException>();
            act = () => GroupSplit.Split(groups, 0, 0, 0, 3);
            act.Should().Throw<ArgumentException>();
            act = () => GroupSplit.Split<int>(null!, 0.5, 0.2, 0.2, 3);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void StratifiedKFold_PartitionsAndPreservesClassRatioPerFold()
        {
            var labels = Enumerable.Range(0, 1000).Select(i => i % 10 == 0 ? 1d : 0d).ToArray();
            var folds = KFoldSplit.Stratified(labels, 5, 9);
            AssertPartition(folds, 1000);

            foreach (var fold in folds)
            {
                fold.Test.Length.Should().Be(200);
                fold.Test.Count(i => labels[i] == 1).Should().Be(20);
            }
        }

        [Fact]
        public void StratifiedKFold_UnevenClassesStayBalancedWithinOne()
        {
            var labels = Enumerable.Range(0, 103).Select(i => i < 13 ? 1d : 0d).ToArray();
            var folds = KFoldSplit.Stratified(labels, 5, 2);
            AssertPartition(folds, 103);
            var positives = folds.Select(f => f.Test.Count(i => labels[i] == 1)).ToArray();
            (positives.Max() - positives.Min()).Should().BeLessThanOrEqualTo(1);
            var sizes = folds.Select(f => f.Test.Length).ToArray();
            (sizes.Max() - sizes.Min()).Should().BeLessThanOrEqualTo(1);
        }

        [Fact]
        public void KFold_SeededDeterministicAndPartitions()
        {
            var a = KFoldSplit.Split(101, 4, 5);
            var b = KFoldSplit.Split(101, 4, 5);
            var c = KFoldSplit.Split(101, 4, 6);
            AssertPartition(a, 101);

            for (var f = 0; f < 4; f++)
            {
                a[f].Test.Should().Equal(b[f].Test);
            }

            a[0].Test.Should().NotEqual(c[0].Test);
        }

        [Fact]
        public void KFold_InvalidArgumentsThrow()
        {
            Action act = () => KFoldSplit.Split(10, 1, 1);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => KFoldSplit.Split(3, 5, 1);
            act.Should().Throw<ArgumentException>();
            act = () => KFoldSplit.Split(-1, 2, 1);
            act.Should().Throw<ArgumentOutOfRangeException>();
            act = () => KFoldSplit.Stratified(null!, 3, 1);
            act.Should().Throw<ArgumentNullException>();
            act = () => KFoldSplit.Stratified([1d, 0d], 3, 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LeakageGuard_DeliberatelyLeakedIndexIsDetected()
        {
            var folds = KFoldSplit.Split(50, 5, 1);
            var leakedTrain = folds[0].Train.Append(folds[0].Test[0]).ToArray();
            leakedTrain.Intersect(folds[0].Test).Should().NotBeEmpty();
        }
    }
}