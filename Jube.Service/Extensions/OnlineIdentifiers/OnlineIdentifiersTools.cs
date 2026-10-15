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

using System.ComponentModel;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Extensions.OnlineIdentifiers.Models;

namespace Jube.Service.Extensions.OnlineIdentifiers
{
    [FunctionToolGroup("OnlineIdentifiers",
        "Validates and dissects digital identifiers: email addresses and plus-aliases, phone numbers, domain names, URLs, IPv4 and IPv6 addresses and CIDR network ranges. Pure parsing; nothing is looked up on the network.")]
    public static class OnlineIdentifiersTools
    {
        [FunctionTool("ValidateEmailAddress")]
        [Description(
            "Checks an email address's syntax and splits it into local part, domain and top-level domain, plus the plus-alias-normalised address (ann+promo@x.com becomes ann@x.com), which exposes one person registering many aliases. Syntax only; no mailbox lookup.")]
        public static EmailAddressDetails ValidateEmailAddress([Description("The email address.")] string email)
        {
            var value = FunctionToolInput.Text(email, nameof(email), 320).Trim();

            return new EmailAddressDetails(value.IsValidEmail(), value.EmailLocalPart(),
                value.EmailDomain().ToLowerInvariant(),
                value.TopLevelDomain(), value.NormalizeEmailAlias());
        }

        [FunctionTool("EmailAddressesMatchIgnoringAliases")]
        [Description(
            "Whether two email addresses reach the same mailbox once case and plus-aliases are ignored; ann+1@x.com and ANN@x.com match.")]
        public static bool EmailAddressesMatchIgnoringAliases(
            [Description("The first email address.")]
            string first,
            [Description("The second email address.")]
            string second)
        {
            return string.Equals(FunctionToolInput.Text(first, nameof(first), 320).NormalizeEmailAlias(),
                FunctionToolInput.Text(second, nameof(second), 320).NormalizeEmailAlias(), StringComparison.Ordinal);
        }

        [FunctionTool("ValidatePhoneNumber")]
        [Description(
            "Reduces a phone number to digits (keeping a leading + and turning a 00 prefix into +) and reports whether that is a valid E.164 international number (+ then up to 15 digits, not starting 0). Format only; no carrier lookup.")]
        public static PhoneNumberDetails ValidatePhoneNumber(
            [Description("The phone number as written.")]
            string phoneNumber)
        {
            var digits = FunctionToolInput.Text(phoneNumber, nameof(phoneNumber), 64).PhoneNumberDigits();

            return new PhoneNumberDetails(digits, digits.IsValidE164PhoneNumber());
        }

        [FunctionTool("DescribeUrl")]
        [Description(
            "Parses an absolute URL and returns its lowercase scheme and host (internationalised hosts in punycode) and its top-level domain; file and network-share URLs are refused. Nothing is fetched.")]
        public static UrlDetails DescribeUrl([Description("The absolute URL.")] string url)
        {
            var value = FunctionToolInput.Text(url, nameof(url), 8000);
            var host = value.UrlHost();

            return new UrlDetails(host.Length > 0, value.UrlScheme(), host, host.TopLevelDomain());
        }

        [FunctionTool("IsValidDomainName")]
        [Description(
            "Whether a text is a syntactically valid internet domain name of at least two labels, each 1 to 63 letters, digits or hyphens, not starting or ending with a hyphen. No DNS lookup.")]
        public static bool IsValidDomainName([Description("The domain name, such as example.co.uk.")] string domainName)
        {
            return FunctionToolInput.Text(domainName, nameof(domainName), 300).IsValidDomainName();
        }

        [FunctionTool("DescribeIpAddress")]
        [Description(
            "Parses an IPv4 or IPv6 address and classifies it: canonical form, family, private (RFC 1918, carrier-grade NAT, unique local), loopback, reserved (link-local, multicast, documentation and similar) or public, and the IPv4 address as a number. IPv4-mapped IPv6 addresses are treated as IPv4.")]
        public static IpAddressDetails DescribeIpAddress([Description("The IP address text.")] string ipAddress)
        {
            var value = FunctionToolInput.Text(ipAddress, nameof(ipAddress), 100);
            var normalised = value.NormaliseIpAddress();

            if (normalised.Length == 0)
            {
                return new IpAddressDetails(false, "", "", false, false, false, false, -1);
            }

            return new IpAddressDetails(true, normalised, normalised.IsIPv4Address() ? "IPv4" : "IPv6",
                value.IsPrivateIpAddress(),
                value.IsLoopbackIpAddress(), value.IsReservedIpAddress(), value.IsPublicIpAddress(),
                normalised.IPv4AddressToNumber());
        }

        [FunctionTool("IsIpAddressInRange")]
        [Description(
            "Whether an IP address falls inside a CIDR network range such as 10.0.0.0/8 or 2001:db8::/32. Addresses and ranges of different families never match.")]
        public static bool IsIpAddressInRange(
            [Description("The IP address to test.")]
            string ipAddress,
            [Description("The network range in CIDR notation.")]
            string cidr)
        {
            return FunctionToolInput.Text(ipAddress, nameof(ipAddress), 100)
                .IsInCidr(FunctionToolInput.Text(cidr, nameof(cidr), 100));
        }

        [FunctionTool("DescribeIpRange")]
        [Description(
            "The first and last address and the number of addresses in a CIDR network range; 192.168.1.77/24 spans 192.168.1.0 to 192.168.1.255, 256 addresses.")]
        public static IpRangeDetails DescribeIpRange([Description("The network range in CIDR notation.")] string cidr)
        {
            var value = FunctionToolInput.Text(cidr, nameof(cidr), 100);
            var first = FunctionToolInput.Found(value.CidrFirstAddress(), $"'{cidr}' is not a valid CIDR range.");

            return new IpRangeDetails(first, value.CidrLastAddress(), value.CidrAddressCount());
        }

        [FunctionTool("NumberToIPv4Address")]
        [Description(
            "Writes a number from 0 to 4294967295 as a dotted IPv4 address; 3232235777 is 192.168.1.1. The reverse is in DescribeIpAddress.")]
        public static string NumberToIPv4Address([Description("The address as an unsigned 32-bit number.")] long number)
        {
            return FunctionToolInput.Found(number.NumberToIPv4Address(), "the number must be from 0 to 4294967295.");
        }
    }
}