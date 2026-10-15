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
using Jube.Service.Extensions.EncodingAndHashing.Models;

namespace Jube.Service.Extensions.EncodingAndHashing
{
    [FunctionToolGroup("EncodingAndHashing",
        "Encodes and decodes text (Base64, Base64Url, hex, URL, HTML, XML, JavaScript) and computes digests, HMACs and checksums over UTF-8 text.")]
    public static class EncodingAndHashingTools
    {
        [FunctionTool("EncodeText")]
        [Description(
            "Encodes UTF-8 text in a scheme: Base64, Base64Url (URL-safe, no padding), Hex (lowercase), Url (form encoding, spaces as +), Html, XmlEscape or JavaScript (string-literal escaping). Use DecodeText to reverse.")]
        public static string EncodeText(
            [Description("The text to encode.")] string text,
            [Description("The scheme: Base64, Base64Url, Hex, Url, Html, XmlEscape or JavaScript.")]
            string scheme)
        {
            var value = FunctionToolInput.Text(text, nameof(text));

            return FunctionToolInput.Choice<TextEncodingScheme>(scheme, nameof(scheme)) switch
            {
                TextEncodingScheme.Base64 => value.EncodeBase64(),
                TextEncodingScheme.Base64Url => value.EncodeBase64Url(),
                TextEncodingScheme.Hex => value.ToHexUtf8(),
                TextEncodingScheme.Url => value.UrlEncode(),
                TextEncodingScheme.Html => value.HtmlEncode(),
                TextEncodingScheme.XmlEscape => value.EscapeXml(),
                _ => value.JavaScriptStringEncode()
            };
        }

        [FunctionTool("DecodeText")]
        [Description(
            "Decodes text from a scheme back to UTF-8 text: Base64, Base64Url, Hex (an optional 0x prefix is allowed), Url or Html. Malformed input is refused with the reason.")]
        public static string DecodeText(
            [Description("The encoded text.")] string text,
            [Description("The scheme: Base64, Base64Url, Hex, Url or Html.")]
            string scheme)
        {
            var value = FunctionToolInput.Text(text, nameof(text));

            try
            {
                return FunctionToolInput.Choice<TextEncodingScheme>(scheme, nameof(scheme)) switch
                {
                    TextEncodingScheme.Base64 => value.DecodeBase64(),
                    TextEncodingScheme.Base64Url => value.DecodeBase64Url(),
                    TextEncodingScheme.Hex => value.FromHexUtf8(),
                    TextEncodingScheme.Url => value.UrlDecode(),
                    TextEncodingScheme.Html => value.HtmlDecode(),
                    _ => throw new ArgumentException("XmlEscape and JavaScript cannot be decoded here.", nameof(scheme))
                };
            }
            catch (FormatException)
            {
                throw new ArgumentException($"The text is not valid {scheme}.", nameof(text));
            }
        }

        [FunctionTool("HashText")]
        [Description(
            "Lowercase hex digest of the UTF-8 bytes of a text with Sha256 (default), Sha384, Sha512, Sha1 or Md5. Sha1 and Md5 are for matching legacy values only, never for security. Use it to compare or pseudonymise values without revealing them.")]
        public static string HashText(
            [Description("The text to hash.")] string text,
            [Description("The algorithm: Sha256, Sha384, Sha512, Sha1 or Md5.")]
            string algorithm = "Sha256")
        {
            var value = FunctionToolInput.Text(text, nameof(text));

            return FunctionToolInput.Choice<TextHashAlgorithm>(algorithm, nameof(algorithm)) switch
            {
                TextHashAlgorithm.Sha256 => value.Sha256Hex(),
                TextHashAlgorithm.Sha384 => value.Sha384Hex(),
                TextHashAlgorithm.Sha512 => value.Sha512Hex(),
                TextHashAlgorithm.Sha1 => value.Sha1Hex(),
                _ => value.Md5Hex()
            };
        }

        [FunctionTool("HmacText")]
        [Description(
            "Lowercase hex HMAC of the UTF-8 bytes of a text under a secret key, with Sha256 (default) or Sha512. Use it to verify a signed payload or to pseudonymise with a key. Never put a production secret into a conversation.")]
        public static string HmacText(
            [Description("The message text.")] string text,
            [Description("The secret key as UTF-8 text.")]
            string key,
            [Description("The algorithm: Sha256 or Sha512.")]
            string algorithm = "Sha256")
        {
            var value = FunctionToolInput.Text(text, nameof(text));
            var secret = FunctionToolInput.Text(key, nameof(key), 10_000);

            return FunctionToolInput.Choice<TextHmacAlgorithm>(algorithm, nameof(algorithm)) == TextHmacAlgorithm.Sha512
                ? value.HmacSha512Hex(secret)
                : value.HmacSha256Hex(secret);
        }

        [FunctionTool("Crc32Checksum")]
        [Description(
            "CRC-32 (IEEE) checksum of the UTF-8 bytes of a text as eight lowercase hex digits; '123456789' gives cbf43926. For detecting accidental change, not tampering.")]
        public static string Crc32Checksum([Description("The text to checksum.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).Crc32Hex();
        }

        [FunctionTool("HashesMatch")]
        [Description(
            "Whether a text hashes to a given hex digest with an algorithm, comparing case-insensitively. Use it to check a value against a stored fingerprint without ever handling the original.")]
        public static bool HashesMatch(
            [Description("The candidate text.")] string text,
            [Description("The expected hex digest.")]
            string expectedHex,
            [Description("The algorithm: Sha256, Sha384, Sha512, Sha1 or Md5.")]
            string algorithm = "Sha256")
        {
            return string.Equals(HashText(text, algorithm),
                FunctionToolInput.Text(expectedHex, nameof(expectedHex), 256).Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        [FunctionTool("DescribeUuid")]
        [Description(
            "Whether a text is a UUID (GUID) and, if so, its canonical lowercase form and version number; version 4 is random and version 7 is time-ordered. SWIFT UETRs are version 4 UUIDs.")]
        public static UuidDetails DescribeUuid([Description("The text to inspect.")] string text)
        {
            var value = FunctionToolInput.Text(text, nameof(text), 100);

            return value.IsValidUuid()
                ? new UuidDetails(true, Guid.Parse(value.Trim()).ToString(), value.UuidVersion())
                : new UuidDetails(false, "", -1);
        }
    }
}