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

using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Xunit;
using EngineModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Models
{
    [Trait("Category", "Unit")]
    public class SnapshotsTests
    {
        [Fact]
        public void OneBuilderIsHandedOutPerModelForTheWholeCycle()
        {
            var snapshots = new Snapshots();
            var model = new EngineModel();

            var first = snapshots.Builder(1, model);
            var second = snapshots.Builder(1, model);

            second.Should().BeSameAs(first);
        }

        [Fact]
        public void EachModelGetsItsOwnBuilder()
        {
            var snapshots = new Snapshots();

            var first = snapshots.Builder(1, new EngineModel());
            var second = snapshots.Builder(2, new EngineModel());

            second.Should().NotBeSameAs(first);
        }

        [Fact]
        public void NothingReachesAModelUntilTheCyclePublishes()
        {
            var snapshots = new Snapshots();
            var model = new EngineModel();

            snapshots.Builder(1, model).ModelActivationRules =
                [new EntityAnalysisModelActivationRule { Id = 1, Name = "Rule" }];

            using var scope = new AssertionScope();
            model.Snapshot.ModelActivationRules.Should().BeEmpty();
            model.Snapshot.Generation.Should().Be(0);

            snapshots.Publish();

            model.Snapshot.ModelActivationRules.Should().HaveCount(1);
            model.Snapshot.Generation.Should().Be(1);
        }

        [Fact]
        public void EveryModelTouchedInTheCycleIsPublishedTogether()
        {
            var snapshots = new Snapshots();
            var first = new EngineModel();
            var second = new EngineModel();

            snapshots.Builder(1, first).EntityAnalysisModelLists =
                new Dictionary<string, List<string>> { ["Deny"] = ["1.2.3.4"] };
            snapshots.Builder(2, second).ModelActivationRules =
                [new EntityAnalysisModelActivationRule { Id = 9, Name = "Rule" }];

            snapshots.Publish();

            using var scope = new AssertionScope();
            first.Snapshot.EntityAnalysisModelLists.Should().ContainKey("Deny");
            first.Snapshot.Generation.Should().Be(1);
            second.Snapshot.ModelActivationRules.Should().HaveCount(1);
            second.Snapshot.Generation.Should().Be(1);
        }

        [Fact]
        public void AModelNotTouchedInTheCycleKeepsItsSnapshot()
        {
            var snapshots = new Snapshots();
            var touched = new EngineModel();
            var untouched = new EngineModel();
            var before = untouched.Snapshot;

            snapshots.Builder(1, touched).Users = ["caller"];
            snapshots.Publish();

            untouched.Snapshot.Should().BeSameAs(before);
        }

        [Fact]
        public void PublishingClearsTheCycleSoTheNextOneStartsFromWhatWasPublished()
        {
            var snapshots = new Snapshots();
            var model = new EngineModel();

            snapshots.Builder(1, model).Users = ["first"];
            snapshots.Publish();

            snapshots.PendingEntityAnalysisModelIds.Should().BeEmpty();

            snapshots.Builder(1, model).ModelActivationRules =
                [new EntityAnalysisModelActivationRule { Id = 1, Name = "Rule" }];
            snapshots.Publish();

            using var scope = new AssertionScope();
            model.Snapshot.Generation.Should().Be(2);
            model.Snapshot.Users.Should().BeEquivalentTo(["first"]);
            model.Snapshot.ModelActivationRules.Should().HaveCount(1);
        }

        [Fact]
        public void PublishingTwiceWithoutAnotherCycleDoesNotAdvanceTheGeneration()
        {
            var snapshots = new Snapshots();
            var model = new EngineModel();

            snapshots.Builder(1, model).Users = ["caller"];
            snapshots.Publish();
            snapshots.Publish();

            model.Snapshot.Generation.Should().Be(1);
        }
    }
}