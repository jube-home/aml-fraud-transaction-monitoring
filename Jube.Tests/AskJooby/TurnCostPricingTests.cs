/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    public class TurnCostPricingTests
    {
        private const byte BgeRerankerV2M3 = (byte)AskJoobyModelType.BgeRerankerV2M3;

        private static AskJoobyEndpointFee AllDayBand(double rateInput, double? rateCachedInput = null,
            double? rateOutput = null) => new()
        {
            Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, Saturday = true,
            Sunday = true, StartTimeOfDay = TimeSpan.Zero, EndTimeOfDay = new TimeSpan(23, 59, 59),
            RateInputPerMillionTokens = rateInput, RateCachedInputPerMillionTokens = rateCachedInput,
            RateOutputPerMillionTokens = rateOutput
        };

        private static readonly IReadOnlyDictionary<byte, IReadOnlyList<AskJoobyEndpointFee>> chatFees =
            new Dictionary<byte, IReadOnlyList<AskJoobyEndpointFee>>
            {
                [(byte)AskJoobyEndpointKind.Chat] = new List<AskJoobyEndpointFee>
                {
                    AllDayBand(10, rateCachedInput: 1, rateOutput: 20)
                }
            };

        private static readonly IReadOnlyDictionary<byte, IReadOnlyList<AskJoobyEndpointFee>> embeddingFees =
            new Dictionary<byte, IReadOnlyList<AskJoobyEndpointFee>>
            {
                [(byte)AskJoobyEndpointKind.Embedding] = new List<AskJoobyEndpointFee> { AllDayBand(2) }
            };

        private static readonly Dictionary<byte, AskJoobyRerankFee> rerankFees = new()
        {
            [BgeRerankerV2M3] = new AskJoobyRerankFee
            {
                ModelTypeId = BgeRerankerV2M3, RatePerDocumentCompared = 0.001
            }
        };

        private static readonly DateTime weekdayNoon = new(2026, 10, 7, 12, 0, 0);

        [Fact]
        public void PriceModelCalls_WithNoCachedTokens_ChargesAllInputAtTheFullRate()
        {
            var calls = new List<AskJoobyModelCall>
            {
                new() { InputTokens = 1_000_000, OutputTokens = 500_000, CreatedDate = weekdayNoon }
            };

            var cost = AskJoobyConversationService.PriceModelCalls(calls, chatFees);

            cost.Should().Be(20);
        }

        [Fact]
        public void PriceModelCalls_PricesTheCachedSubsetSeparately_NotOnTopOfInputTokens()
        {
            var calls = new List<AskJoobyModelCall>
            {
                new()
                {
                    InputTokens = 1_000_000, CachedInputTokens = 300_000, OutputTokens = 0,
                    CreatedDate = weekdayNoon
                }
            };

            var cost = AskJoobyConversationService.PriceModelCalls(calls, chatFees);

            cost.Should().BeApproximately(7.30, 0.0001);
        }

        [Fact]
        public void PriceModelCalls_WithNoCachedRateConfigured_FallsBackToTheFullInputRate()
        {
            var fees = new Dictionary<byte, IReadOnlyList<AskJoobyEndpointFee>>
            {
                [(byte)AskJoobyEndpointKind.Chat] = new List<AskJoobyEndpointFee> { AllDayBand(10, rateOutput: 20) }
            };

            var calls = new List<AskJoobyModelCall>
            {
                new() { InputTokens = 1_000_000, CachedInputTokens = 300_000, CreatedDate = weekdayNoon }
            };

            var cost = AskJoobyConversationService.PriceModelCalls(calls, fees);

            cost.Should().Be(10);
        }

        [Fact]
        public void PriceModelCalls_SumsAcrossDifferentTimesOfDayInTheSameTurn_EachAtItsOwnBandsRate()
        {
            var fees = new Dictionary<byte, IReadOnlyList<AskJoobyEndpointFee>>
            {
                [(byte)AskJoobyEndpointKind.Chat] = new List<AskJoobyEndpointFee>
                {
                    new()
                    {
                        Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true,
                        Saturday = true, Sunday = true, StartTimeOfDay = new TimeSpan(6, 0, 0),
                        EndTimeOfDay = new TimeSpan(21, 59, 59), RateInputPerMillionTokens = 10,
                        RateOutputPerMillionTokens = 20
                    },
                    new()
                    {
                        Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true,
                        Saturday = true, Sunday = true, StartTimeOfDay = new TimeSpan(22, 0, 0),
                        EndTimeOfDay = new TimeSpan(5, 59, 59), RateInputPerMillionTokens = 1,
                        RateOutputPerMillionTokens = 2
                    }
                }
            };

            var calls = new List<AskJoobyModelCall>
            {
                new() { InputTokens = 1_000_000, OutputTokens = 0, CreatedDate = new DateTime(2026, 10, 7, 12, 0, 0) },

                new() { InputTokens = 1_000_000, OutputTokens = 0, CreatedDate = new DateTime(2026, 10, 7, 23, 0, 0) }
            };

            var cost = AskJoobyConversationService.PriceModelCalls(calls, fees);

            cost.Should().Be(11);
        }

        [Fact]
        public void PriceModelCalls_WithATimeThatMatchesNoBand_ReturnsNullRatherThanAPartialFigure()
        {
            var calls = new List<AskJoobyModelCall>
            {
                new() { InputTokens = 1_000_000, CreatedDate = weekdayNoon },
                new() { InputTokens = 1_000_000, CreatedDate = null }
            };

            var cost = AskJoobyConversationService.PriceModelCalls(calls, chatFees);

            cost.Should().BeNull();
        }

        [Fact]
        public void PriceModelCalls_WithABandRestrictedToWeekdays_DoesNotMatchASaturdayCall()
        {
            var fees = new Dictionary<byte, IReadOnlyList<AskJoobyEndpointFee>>
            {
                [(byte)AskJoobyEndpointKind.Chat] = new List<AskJoobyEndpointFee>
                {
                    new()
                    {
                        Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true,
                        Saturday = false, Sunday = false, StartTimeOfDay = TimeSpan.Zero,
                        EndTimeOfDay = new TimeSpan(23, 59, 59), RateInputPerMillionTokens = 10
                    }
                }
            };

            var calls = new List<AskJoobyModelCall>
            {
                new() { InputTokens = 1_000_000, CreatedDate = new DateTime(2026, 10, 10, 12, 0, 0) }
            };

            var cost = AskJoobyConversationService.PriceModelCalls(calls, fees);

            cost.Should().BeNull();
        }

        [Fact]
        public void PriceModelCalls_WithAMidnightWrappingBand_MatchesBothSidesOfMidnightButNotTheMiddleOfTheDay()
        {
            var offPeak = new AskJoobyEndpointFee
            {
                Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, Saturday = true,
                Sunday = true, StartTimeOfDay = new TimeSpan(22, 0, 0), EndTimeOfDay = new TimeSpan(6, 0, 0),
                RateInputPerMillionTokens = 1
            };

            var fees = new Dictionary<byte, IReadOnlyList<AskJoobyEndpointFee>>
            {
                [(byte)AskJoobyEndpointKind.Chat] = new List<AskJoobyEndpointFee> { offPeak }
            };

            var late = AskJoobyConversationService.PriceModelCalls(
                new List<AskJoobyModelCall>
                    { new() { InputTokens = 1_000_000, CreatedDate = new DateTime(2026, 10, 7, 23, 0, 0) } },
                fees);
            var early = AskJoobyConversationService.PriceModelCalls(
                new List<AskJoobyModelCall>
                    { new() { InputTokens = 1_000_000, CreatedDate = new DateTime(2026, 10, 7, 2, 0, 0) } },
                fees);
            var midday = AskJoobyConversationService.PriceModelCalls(
                new List<AskJoobyModelCall>
                    { new() { InputTokens = 1_000_000, CreatedDate = new DateTime(2026, 10, 7, 12, 0, 0) } },
                fees);

            late.Should().Be(1);
            early.Should().Be(1);
            midday.Should().BeNull();
        }

        [Fact]
        public void PriceEmbeddingCalls_PricesByInputTokensOnly()
        {
            var calls = new List<AskJoobyEmbeddingCall>
            {
                new() { InputTokens = 500_000, CreatedDate = weekdayNoon }
            };

            var cost = AskJoobyConversationService.PriceEmbeddingCalls(calls, embeddingFees);

            cost.Should().Be(1);
        }

        [Fact]
        public void PriceRerankCalls_WithNoTokensCompared_FallsBackToDocumentsCompared()
        {
            var calls = new List<AskJoobyRerankCall>
            {
                new() { ModelTypeId = BgeRerankerV2M3, DocumentsCompared = 40 }
            };

            var cost = AskJoobyConversationService.PriceRerankCalls(calls, rerankFees);

            cost.Should().BeApproximately(0.04, 0.0001);
        }

        [Fact]
        public void PriceRerankCalls_PrefersTokensComparedWhenBothTheCallAndTheFeeHaveIt()
        {
            var fees = new Dictionary<byte, AskJoobyRerankFee>
            {
                [BgeRerankerV2M3] = new AskJoobyRerankFee
                {
                    ModelTypeId = BgeRerankerV2M3, RatePerDocumentCompared = 1_000, RatePerMillionTokensCompared = 0.01
                }
            };

            var calls = new List<AskJoobyRerankCall>
            {
                new() { ModelTypeId = BgeRerankerV2M3, DocumentsCompared = 40, TokensCompared = 5_000_000 }
            };

            var cost = AskJoobyConversationService.PriceRerankCalls(calls, fees);

            cost.Should().Be(0.05);
        }

        [Fact]
        public void PriceRerankCalls_WithAnUnpricedModel_ReturnsNull()
        {
            var calls = new List<AskJoobyRerankCall>
            {
                new() { ModelTypeId = (byte)AskJoobyModelType.BgeM3, DocumentsCompared = 10 }
            };

            var cost = AskJoobyConversationService.PriceRerankCalls(calls, rerankFees);

            cost.Should().BeNull();
        }

        [Fact]
        public void PriceRerankCalls_WithNeitherAUsableTokenCountNorADocumentRateInCommon_ReturnsNull()
        {
            var fees = new Dictionary<byte, AskJoobyRerankFee>
            {
                [BgeRerankerV2M3] = new AskJoobyRerankFee
                {
                    ModelTypeId = BgeRerankerV2M3, RatePerMillionTokensCompared = 0.01
                }
            };

            var calls = new List<AskJoobyRerankCall>
            {
                new() { ModelTypeId = BgeRerankerV2M3, DocumentsCompared = 10 }
            };

            var cost = AskJoobyConversationService.PriceRerankCalls(calls, fees);

            cost.Should().BeNull();
        }
    }
}