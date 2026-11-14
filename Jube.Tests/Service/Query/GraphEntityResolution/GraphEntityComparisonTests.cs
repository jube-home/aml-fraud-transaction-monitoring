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

using FluentAssertions;
using Jube.Service.Query.Graph.Resolution;
using Xunit;

namespace Jube.Test.Service.Query.GraphEntityResolution
{
    [Trait("Category", "Unit")]
    public sealed class GraphEntityComparisonTests
    {
        [Fact]
        public void FoldingIgnoresCasePunctuationAndAccents()
        {
            GraphEntityResolutionJob.Fold("  José  A. Smith-Jones ").Should()
                .Be(GraphEntityResolutionJob.Fold("JOSE A SMITH JONES"));
        }

        [Fact]
        public void ValuesThatFoldTheSameAreAnExactAgreementWeightedByHowCommonTheyAre()
        {
            var rare = GraphEntityResolutionJob.Compare("john smith", "john smith", 0.0001, 0.92)!.Value;
            var common = GraphEntityResolutionJob.Compare("john smith", "john smith", 0.05, 0.92)!.Value;

            rare.Level.Should().Be("Exact");
            rare.Similarity.Should().Be(1);
            rare.Weight.Should().BeGreaterThan(common.Weight);
        }

        [Fact]
        public void NearValuesAreAClosenessAgreementAndFarOnesAreNotACandidate()
        {
            var close = GraphEntityResolutionJob.Compare("john smith", "jon smith", 0.0001, 0.92);
            var reordered = GraphEntityResolutionJob.Compare("smith john", "john smith", 0.0001, 0.92);

            close!.Value.Level.Should().Be("Close");
            reordered.Should().NotBeNull();
            GraphEntityResolutionJob.Compare("john smith", "maria garcia", 0.0001, 0.92).Should().BeNull();
            GraphEntityResolutionJob.Compare("", "john", 0.0001, 0.92).Should().BeNull();
        }

        [Fact]
        public void AShareOfEvidenceFromOneNameAloneIsNeverCertain()
        {
            var exact = GraphEntityResolutionJob.Compare("john smith", "john smith", 0.0001, 0.92)!.Value;
            var posterior = FellegiSunter.Posterior(1e-4, [exact.Weight]);

            posterior.Should().BeLessThan(0.99);
            GraphEntityResolutionJob.HardPosterior.Should().BeGreaterThan(posterior);
        }
    }
}
