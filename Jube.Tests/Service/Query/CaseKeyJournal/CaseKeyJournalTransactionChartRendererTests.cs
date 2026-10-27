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
using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class CaseKeyJournalTransactionChartRendererTests
    {
        private static JournalAdaptationDto Adaptation(bool exact, int hidden = 0)
        {
            var items = new List<JournalContributionItemDto>
            {
                new() { Name = "Amount", Weight = 2.2, Signed = 2.2 },
                new() { Name = "Volume", HumanLabel = "Cash volume", Weight = 0.5, Direction = -1, Signed = -0.5 },
                new() { Name = "Age", Weight = 0.1, Signed = 0.1 }
            };

            items.AddRange(Enumerable.Range(0, hidden).Select(_ => new JournalContributionItemDto { Hidden = true }));

            return new JournalAdaptationDto
            {
                Source = "exhaustive",
                Name = "StructNet",
                Value = 0.9,
                Contribution = new JournalContributionDto
                {
                    Space = "LogOdds", Method = "TreePathContribution", Exact = exact,
                    BaseValue = exact ? -3.18 : null, Items = items, HiddenItems = hidden
                },
                Calibration = new JournalCalibrationDto
                {
                    Sample = 212,
                    Bands =
                    [
                        new JournalCalibrationBandDto { Expected = 0.2, Observed = 0.18, Count = 100 },
                        new JournalCalibrationBandDto { Expected = 0.8, Observed = 0.7, Count = 112 }
                    ]
                }
            };
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ContributionsDrawAsAWaterfallWhenExactAndAsBarsWhenRelative(bool exact)
        {
            var svg = CaseKeyJournalTransactionChartRenderer.Render(CaseKeyJournalTransactionChart.Contribution,
                Adaptation(exact), false);

            svg.Should().StartWith("<svg");
            svg.Should().Contain("Cash volume");
            svg.Should().Contain(exact ? "what moved the score" : "what pushed the score");
        }

        [Fact]
        public void HiddenInputsAreNeverDrawnAndTheChartSaysHowManyWereHidden()
        {
            var svg = CaseKeyJournalTransactionChartRenderer.Render(CaseKeyJournalTransactionChart.Contribution,
                Adaptation(true, hidden: 2), true);

            svg.Should().Contain("2 inputs hidden");
            svg.Should().Contain("Visible inputs");
        }

        [Fact]
        public void OnlyTheLargestFifteenInputsAreDrawn()
        {
            var adaptation = Adaptation(false);

            adaptation.Contribution!.Items = Enumerable.Range(1, 40).Select(i => new JournalContributionItemDto
            {
                Name = $"Input{i:00}", Signed = i
            }).ToList();

            var svg = CaseKeyJournalTransactionChartRenderer.Render(CaseKeyJournalTransactionChart.Contribution,
                adaptation, false);

            svg.Should().Contain("Input40");
            svg.Should().NotContain("Input20");
        }

        [Fact]
        public void CalibrationDrawsTheReliabilityCurveInBothThemes()
        {
            var adaptation = Adaptation(true);

            CaseKeyJournalTransactionChartRenderer.Render(CaseKeyJournalTransactionChart.Calibration, adaptation,
                false).Should().StartWith("<svg");
            CaseKeyJournalTransactionChartRenderer.Render(CaseKeyJournalTransactionChart.Calibration, adaptation,
                true).Should().StartWith("<svg");
        }

        [Theory]
        [InlineData(CaseKeyJournalTransactionChart.Contribution)]
        [InlineData(CaseKeyJournalTransactionChart.Calibration)]
        public void WithNothingToDrawTheChartSaysSo(CaseKeyJournalTransactionChart kind)
        {
            var act = () => CaseKeyJournalTransactionChartRenderer.Render(kind,
                new JournalAdaptationDto { Name = "None" }, false);

            act.Should().Throw<ArgumentException>().WithMessage("There is no * to chart.");
        }

        [Fact]
        public void AHiddenInputIsNeverDrawnEvenIfSomethingLeftAFigureOnIt()
        {
            var adaptation = Adaptation(false);

            adaptation.Contribution!.Items.Add(new JournalContributionItemDto
            {
                Hidden = true, Name = "LeakedName", Signed = 99
            });

            CaseKeyJournalTransactionChartRenderer.Render(CaseKeyJournalTransactionChart.Contribution, adaptation,
                false).Should().NotContain("LeakedName");
        }

        [Fact]
        public void AContributionOfOnlyHiddenInputsHasNothingToDraw()
        {
            var adaptation = new JournalAdaptationDto
            {
                Name = "Hidden",
                Contribution = new JournalContributionDto
                {
                    Items = [new JournalContributionItemDto { Hidden = true }], HiddenItems = 1
                }
            };

            var act = () => CaseKeyJournalTransactionChartRenderer.Render(
                CaseKeyJournalTransactionChart.Contribution, adaptation, false);

            act.Should().Throw<ArgumentException>();
        }
    }
}
