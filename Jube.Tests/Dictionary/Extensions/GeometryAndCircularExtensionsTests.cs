using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class GeometryAndCircularExtensionsTests
    {
        private static readonly string[] square = ["0,0", "0,10", "10,10", "10,0"];

        [Theory]
        [InlineData(23d, 1d, 24d, 2d)]
        [InlineData(1d, 23d, 24d, 2d)]
        [InlineData(0d, 12d, 24d, 12d)]
        [InlineData(5d, 5d, 24d, 0d)]
        [InlineData(350d, 10d, 360d, 20d)]
        public void CircularDistanceFromIsTheShorterWayAroundTheScale(double value, double other, double period,
            double expected)
        {
            value.CircularDistanceFrom(other, period).Should().BeApproximately(expected, 1e-9);
        }

        [Theory]
        [InlineData(double.NaN, 1d, 24d)]
        [InlineData(1d, double.NaN, 24d)]
        [InlineData(1d, 2d, 0d)]
        [InlineData(1d, 2d, -24d)]
        public void CircularDistanceFromIsNaNWhenUndefined(double value, double other, double period)
        {
            double.IsNaN(value.CircularDistanceFrom(other, period)).Should().BeTrue();
        }

        [Fact]
        public void IsInsidePolygonIsTrueForAPointInsideAndFalseOutside()
        {
            5d.IsInsidePolygon(5d, square).Should().BeTrue();
            15d.IsInsidePolygon(5d, square).Should().BeFalse();
            5d.IsInsidePolygon(-1d, square).Should().BeFalse();
        }

        [Fact]
        public void IsInsidePolygonIsFalseForFewerThanThreeVertices()
        {
            5d.IsInsidePolygon(5d, ["0,0", "10,10"]).Should().BeFalse();
        }

        [Fact]
        public void IsInsidePolygonIsFalseWhenAVertexDoesNotParse()
        {
            5d.IsInsidePolygon(5d, ["0,0", "not-a-point", "10,10", "10,0"]).Should().BeFalse();
        }

        [Fact]
        public void IsInsidePolygonIsFalseForANullVertexList()
        {
            5d.IsInsidePolygon(5d, null).Should().BeFalse();
        }

        [Fact]
        public void IsWithinAnyRadiusKmMatchesAnyOfTheCentres()
        {
            const string london = "51.5074,-0.1278";
            const string newYork = "40.7128,-74.0060";

            51.5d.IsWithinAnyRadiusKm(-0.13d, [newYork, london], 5d).Should().BeTrue();
            51.5d.IsWithinAnyRadiusKm(-0.13d, [newYork], 5d).Should().BeFalse();
        }

        [Fact]
        public void IsWithinAnyRadiusKmIgnoresUnparseableCentres()
        {
            51.5d.IsWithinAnyRadiusKm(-0.13d, ["garbage", "51.5074,-0.1278"], 5d).Should().BeTrue();
            51.5d.IsWithinAnyRadiusKm(-0.13d, null, 5d).Should().BeFalse();
        }
    }
}