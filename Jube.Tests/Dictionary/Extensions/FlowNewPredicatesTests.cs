using System;
using System.Globalization;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowNewPredicatesTests
    {
        private static readonly string[] square = ["0,0", "0,10", "10,10", "10,0"];

        [Theory]
        [InlineData(5d, 5d, true)]
        [InlineData(15d, 5d, false)]
        public void InsidePolygonAppliesEveryStepKind(double latitude, double longitude, bool expected)
        {
            var start = latitude.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsInsidePolygon(longitude, square),
                start.RejectIsInsidePolygon(longitude, square),
                start.BreakIsInsidePolygon(longitude, square),
                start.RequireIsInsidePolygon(longitude, square),
                start.EnsureIsInsidePolygon(longitude, square));
        }

        [Fact]
        public void WithinAnyRadiusKmAppliesEveryStepKind()
        {
            var centres = new[] { "51.5074,-0.1278" };
            var start = 51.5d.Start();

            FlowAssert.Kinds(true,
                start.MatchIsWithinAnyRadiusKm(-0.13d, centres, 5d),
                start.RejectIsWithinAnyRadiusKm(-0.13d, centres, 5d),
                start.BreakIsWithinAnyRadiusKm(-0.13d, centres, 5d),
                start.RequireIsWithinAnyRadiusKm(-0.13d, centres, 5d),
                start.EnsureIsWithinAnyRadiusKm(-0.13d, centres, 5d));
        }

        [Fact]
        public void IsDelimitedMemberAppliesEveryStepKind()
        {
            var start = "GB".Start();

            FlowAssert.Kinds(true,
                start.MatchIsDelimitedMember("GB,US", ","),
                start.RejectIsDelimitedMember("GB,US", ","),
                start.BreakIsDelimitedMember("GB,US", ","),
                start.RequireIsDelimitedMember("GB,US", ","),
                start.EnsureIsDelimitedMember("GB,US", ","));
        }

        [Fact]
        public void IsInAnyCidrAppliesEveryStepKind()
        {
            var ranges = new[] { "10.0.0.0/8" };
            var start = "10.1.2.3".Start();

            FlowAssert.Kinds(true,
                start.MatchIsInAnyCidr(ranges),
                start.RejectIsInAnyCidr(ranges),
                start.BreakIsInAnyCidr(ranges),
                start.RequireIsInAnyCidr(ranges),
                start.EnsureIsInAnyCidr(ranges));
        }

        [Fact]
        public void IsCryptoWalletAddressAppliesEveryStepKind()
        {
            var start = "0x52908400098527886E0F7030069857D2E4169EE7".Start();

            FlowAssert.Kinds(true,
                start.MatchIsCryptoWalletAddress(),
                start.RejectIsCryptoWalletAddress(),
                start.BreakIsCryptoWalletAddress(),
                start.RequireIsCryptoWalletAddress(),
                start.EnsureIsCryptoWalletAddress());
        }

        [Fact]
        public void JsonTextArrayAnyInListAppliesEveryStepKind()
        {
            const string json = "{\"tags\":[\"mule\"]}";
            var list = new[] { "mule" };
            var start = json.Start();

            FlowAssert.Kinds(true,
                start.MatchJsonTextArrayAnyInList("$.tags", list),
                start.RejectJsonTextArrayAnyInList("$.tags", list),
                start.BreakJsonTextArrayAnyInList("$.tags", list),
                start.RequireJsonTextArrayAnyInList("$.tags", list),
                start.EnsureJsonTextArrayAnyInList("$.tags", list));
        }

        [Fact]
        public void NumberInTextWithinPercentOfAppliesEveryStepKind()
        {
            var start = "Paid 9800".Start();

            FlowAssert.Kinds(true,
                start.MatchNumberInTextWithinPercentOf(10000d, 5d),
                start.RejectNumberInTextWithinPercentOf(10000d, 5d),
                start.BreakNumberInTextWithinPercentOf(10000d, 5d),
                start.RequireNumberInTextWithinPercentOf(10000d, 5d),
                start.EnsureNumberInTextWithinPercentOf(10000d, 5d));
        }

        [Fact]
        public void DayOfMonthInAppliesEveryStepKind()
        {
            var value = DateTime.Parse("2026-10-15", CultureInfo.InvariantCulture);
            var start = value.Start();

            FlowAssert.Kinds(true,
                start.MatchDayOfMonthIn(1, 15, 28),
                start.RejectDayOfMonthIn(1, 15, 28),
                start.BreakDayOfMonthIn(1, 15, 28),
                start.RequireDayOfMonthIn(1, 15, 28),
                start.EnsureDayOfMonthIn(1, 15, 28));

            FlowAssert.Kinds(false,
                value.Start().MatchDayOfMonthIn(2, 3),
                value.Start().RejectDayOfMonthIn(2, 3),
                value.Start().BreakDayOfMonthIn(2, 3),
                value.Start().RequireDayOfMonthIn(2, 3),
                value.Start().EnsureDayOfMonthIn(2, 3));
        }
    }
}