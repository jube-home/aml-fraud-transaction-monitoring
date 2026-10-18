using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class TextNetworkAndWalletExtensionsTests
    {
        [Theory]
        [InlineData("Cr3d1t", "credit")]
        [InlineData("B I T C O I N", "bitcoin")]
        [InlineData("s.a.n.c.t.i.o.n", "sanction")]
        [InlineData("аmаzоn", "amazon")]
        [InlineData("PLAIN", "plain")]
        public void FoldedForMatchingReducesEvasionToTheUnderlyingWord(string input, string expected)
        {
            input.FoldedForMatching().Should().Be(expected);
        }

        [Fact]
        public void FoldedForMatchingOfNullIsEmpty()
        {
            ((string?)null).FoldedForMatching().Should().Be(string.Empty);
        }

        [Theory]
        [InlineData("Москва", "moskva")]
        [InlineData("Αθήνα", "athina")]
        [InlineData("Ёлка", "yolka")]
        public void TransliterateToLatinMapsCyrillicAndGreek(string input, string expected)
        {
            input.TransliterateToLatin().Should().Be(expected);
        }

        [Fact]
        public void CanonicalPairKeyIsTheSameForBothDirections()
        {
            "acct-9".CanonicalPairKey("acct-2").Should().Be("acct-2\u001Facct-9");
            "acct-2".CanonicalPairKey("acct-9").Should().Be("acct-2\u001Facct-9");
        }

        [Fact]
        public void DirectionSignReportsWhichSideSortsFirst()
        {
            "a".DirectionSign("b").Should().Be(1d);
            "b".DirectionSign("a").Should().Be(-1d);
        }

        [Theory]
        [InlineData("Invoice ref 9800 for goods", 10000d, 5d, true)]
        [InlineData("Invoice ref 9000 for goods", 10000d, 5d, false)]
        [InlineData("No amount here", 10000d, 5d, false)]
        public void NumberInTextWithinPercentOfChecksEveryNumberInTheText(string text, double target,
            double tolerance, bool expected)
        {
            text.NumberInTextWithinPercentOf(target, tolerance).Should().Be(expected);
        }

        [Theory]
        [InlineData("GB", "GB,US,FR", ",", true)]
        [InlineData("US", "GB, US ,FR", ",", true)]
        [InlineData("DE", "GB,US,FR", ",", false)]
        [InlineData("GB", "GB,US,FR", "", false)]
        [InlineData("GB", null, ",", false)]
        public void IsDelimitedMemberMatchesOneToken(string value, string? list, string? separator, bool expected)
        {
            value.IsDelimitedMember(list, separator).Should().Be(expected);
        }

        [Fact]
        public void IsInAnyCidrMatchesAnyOfTheRanges()
        {
            var ranges = new[] { "192.168.0.0/16", "10.0.0.0/8" };

            "10.1.2.3".IsInAnyCidr(ranges).Should().BeTrue();
            "8.8.8.8".IsInAnyCidr(ranges).Should().BeFalse();
            "not-an-ip".IsInAnyCidr(ranges).Should().BeFalse();
            "10.1.2.3".IsInAnyCidr(null).Should().BeFalse();
        }

        [Theory]
        [InlineData("1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2", true)]
        [InlineData("1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN3", false)]
        [InlineData("bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq", true)]
        [InlineData("bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdr", false)]
        [InlineData("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", true)]
        [InlineData("0x52908400098527886E0F7030069857D2E4169EE7", true)]
        [InlineData("0x5290840009852788", false)]
        [InlineData("hello", false)]
        public void IsCryptoWalletAddressAcceptsOnlyWellFormedAddresses(string address, bool expected)
        {
            address.IsCryptoWalletAddress().Should().Be(expected);
        }

        [Fact]
        public void IsCryptoWalletAddressIsFalseForNull()
        {
            ((string?)null).IsCryptoWalletAddress().Should().BeFalse();
        }

        [Fact]
        public void JsonTextArrayAnyInListMatchesAMemberOfTheArray()
        {
            const string json = "{\"customer\":{\"tags\":[\"mule\",\"new\"]}}";

            json.JsonTextArrayAnyInList("$.customer.tags", ["mule", "fraud"]).Should().BeTrue();
            json.JsonTextArrayAnyInList("$.customer.tags", ["fraud"]).Should().BeFalse();
            json.JsonTextArrayAnyInList("$.customer.name", ["mule"]).Should().BeFalse();
            json.JsonTextArrayAnyInList("$.customer.tags", null).Should().BeFalse();
        }
    }
}