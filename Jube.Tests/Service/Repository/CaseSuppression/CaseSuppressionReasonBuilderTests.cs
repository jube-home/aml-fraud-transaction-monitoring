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
using Jube.Service.Repository.CaseSuppression;
using Jube.Service.Repository.CaseSuppression.Models;
using Xunit;

namespace Jube.Test.Service.Repository.CaseSuppression
{
    [Trait("Category", "Unit")]
    public sealed class CaseSuppressionReasonBuilderTests
    {
        private static CaseSuppressionReasonFacts Facts(
            string[]? targets = null, bool force = false, bool suppress = true, int count = 14,
            DateTime? first = null, DateTime? last = null, bool wider = true, int widerTransactions = 230,
            int widerKeys = 213, bool capped = false, DateTime? expiry = null) =>
            new(9041, "AccountId", "ACC-441", "IpAddress", "10.0.0.1", targets ?? ["Fraud model / Velocity 9d"], force,
                suppress, count, first ?? new DateTime(2026, 10, 2), last ?? new DateTime(2026, 10, 29), wider,
                widerTransactions, widerKeys, capped, expiry ?? new DateTime(2026, 12, 2));

        [Fact]
        public void TheReasonNamesWhatIsSuppressedTheKeyTheCaseAndTheExpiry()
        {
            CaseSuppressionReasonBuilder.Build(Facts()).Should().Be(
                "Suppress Fraud model / Velocity 9d for IpAddress 10.0.0.1: seen in 14 transactions on C-9041 " +
                "(AccountId ACC-441), 2 Oct 2026 to 29 Oct 2026; 212 other case keys carry it (230 transactions in " +
                "the model archive). Expires 2 Dec 2026.");
        }

        [Fact]
        public void AllForceSaysForceAndAMixSaysBoth()
        {
            CaseSuppressionReasonBuilder.Build(Facts(force: true, suppress: false)).Should().StartWith("Force ");
            CaseSuppressionReasonBuilder.Build(Facts(force: true, suppress: true)).Should().StartWith("Suppress or force ");
        }

        [Fact]
        public void OneTransactionOnOneDayAndOneOtherKeyReadInTheSingular()
        {
            var text = CaseSuppressionReasonBuilder.Build(Facts(count: 1, first: new DateTime(2026, 10, 2),
                last: new DateTime(2026, 10, 2), widerKeys: 2, widerTransactions: 1));

            text.Should().Contain("seen in 1 transaction on C-9041").And.Contain(", on 2 Oct 2026;")
                .And.Contain("1 other case key carries it (1 transaction in the model archive)");
        }

        [Fact]
        public void ACappedWideCountIsMarkedWithAPlus()
        {
            CaseSuppressionReasonBuilder.Build(Facts(widerTransactions: 1000, widerKeys: 401, capped: true))
                .Should().Contain("400+ other case keys carry it (1,000+ transactions in the model archive)");
        }

        [Fact]
        public void AnUnindexedKeySaysTheWiderImpactIsUnknown()
        {
            CaseSuppressionReasonBuilder.Build(Facts(wider: false)).Should()
                .Contain("the wider impact is unknown because the archive is not indexed for this key yet");
        }

        [Fact]
        public void ManyTargetsAreSummarisedAfterFive()
        {
            var targets = new[] { "A", "B", "C", "D", "E", "F", "G" };

            CaseSuppressionReasonBuilder.Build(Facts(targets)).Should().Contain("A, B, C, D, E and 2 more for");
        }

        [Fact]
        public void TheReasonIsBuiltFromFactsOnlyAndIsRepeatable()
        {
            var facts = Facts();

            CaseSuppressionReasonBuilder.Build(facts).Should().Be(CaseSuppressionReasonBuilder.Build(facts));
        }
    }
}
