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
using System.Text.Json;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Service.Query.CaseTriage;
using Xunit;

namespace Jube.Test.Service.Query.CaseTriage
{
    [Trait("Category", "Unit")]
    public sealed class CaseTriageReplayBundleTests
    {
        private static readonly DateTime asOf = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

        private static ReplayCaseCandidate Candidate() => new()
        {
            CaseId = 1, CaseKey = "Account", CaseKeyValue = "A1", CreatedDate = asOf.AddMinutes(-60), HumanOutcomeId = 1
        };

        [Fact]
        public void TheBundleCarriesOnlyWhatItWasGivenAndNeverTheHumanOutcome()
        {
            var json = CaseTriageReplayBundle.Build(Candidate(), asOf,
                [new ReplayArchiveRow { ReferenceDate = asOf.AddMinutes(-5), Json = "{\"amount\":10}" }],
                [new ReplayNote { CreatedDate = asOf.AddMinutes(-2), Note = "Called." }],
                [new ReplayPriorOutcome { OutcomeId = 1, Cases = 2 }, new ReplayPriorOutcome { OutcomeId = 2, Cases = 5 }]);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            root.GetProperty("caseKeyValue").GetString().Should().Be("A1");
            root.GetProperty("transactions").GetArrayLength().Should().Be(1);
            root.GetProperty("notes")[0].GetProperty("text").GetString().Should().Be("Called.");
            root.GetProperty("earlierClosedCases").GetProperty("confirmed").GetInt32().Should().Be(2);
            root.GetProperty("earlierClosedCases").GetProperty("notConfirmed").GetInt32().Should().Be(5);
            json.Should().NotContainEquivalentOf("humanOutcome").And.NotContainEquivalentOf("closedDate");
        }

        [Fact]
        public void LongPayloadsAndNotesAreClipped()
        {
            var json = CaseTriageReplayBundle.Build(Candidate(), asOf,
                [new ReplayArchiveRow { ReferenceDate = asOf, Json = new string('x', 10000) }],
                [new ReplayNote { CreatedDate = asOf, Note = new string('y', 5000) }], []);

            using var document = JsonDocument.Parse(json);
            document.RootElement.GetProperty("transactions")[0].GetProperty("payload").GetString()!.Length
                .Should().Be(CaseTriageReplayBundle.MaximumArchiveCharacters);
            document.RootElement.GetProperty("notes")[0].GetProperty("text").GetString()!.Length
                .Should().Be(CaseTriageReplayBundle.MaximumNoteCharacters);
        }
    }
}
