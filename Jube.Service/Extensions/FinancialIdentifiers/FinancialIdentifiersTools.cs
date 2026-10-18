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
using Jube.Service.Extensions.FinancialIdentifiers.Models;

namespace Jube.Service.Extensions.FinancialIdentifiers
{
    [FunctionToolGroup("FinancialIdentifiers",
        "Validates and dissects payment and securities identifiers: IBAN, BIC/SWIFT, card numbers and expiry, Luhn check digits, ABA routing numbers, ISIN, LEI, CUSIP and SEDOL.")]
    public static class FinancialIdentifiersTools
    {
        [FunctionTool("ValidateIban")]
        [Description(
            "Checks an IBAN's structure and ISO 7064 mod-97 check digits (spaces allowed) and, when valid, returns its country, check digits, basic bank account number and print format in groups of four. A valid checksum does not prove the account exists.")]
        public static IbanDetails ValidateIban([Description("The IBAN, with or without spaces.")] string iban)
        {
            var value = FunctionToolInput.Text(iban, nameof(iban), 64);

            return new IbanDetails(value.IsValidIban(), value.IbanCountryCode(), value.IbanCheckDigits(),
                value.IbanBasicBankAccountNumber(), value.IbanPrintFormat());
        }

        [FunctionTool("ValidateBic")]
        [Description(
            "Checks the format of a BIC (SWIFT code, 8 or 11 characters) and, when valid, splits it into institution, country, location and branch codes; an 8-character BIC is the primary office, branch XXX.")]
        public static BicDetails ValidateBic([Description("The BIC or SWIFT code.")] string bic)
        {
            var value = FunctionToolInput.Text(bic, nameof(bic), 32).Trim();

            return new BicDetails(value.IsValidBic(), value.BicInstitutionCode(), value.BicCountryCode(),
                value.BicLocationCode(),
                value.BicBranchCode());
        }

        [FunctionTool("ValidateCardNumber")]
        [Description(
            "Checks a payment card number with the Luhn algorithm (spaces and hyphens allowed) and reports the card network from its prefix, the 6 and 8 digit issuer identification numbers and a masked form showing only the last four digits.")]
        public static CardNumberDetails ValidateCardNumber([Description("The card number (PAN).")] string cardNumber)
        {
            var digits = new string(FunctionToolInput.Text(cardNumber, nameof(cardNumber), 64)
                .Where(c => c is not (' ' or '-')).ToArray());

            return new CardNumberDetails(digits.Length is >= 12 and <= 19 && digits.IsValidLuhn(), digits.CardNetwork(),
                digits.CardIssuerPrefix(6), digits.CardIssuerPrefix(8), digits.MaskExceptLast(4));
        }

        [FunctionTool("LuhnCheckDigit")]
        [Description(
            "The Luhn check digit that would complete a digit string; 7992739871 needs 3. Use it to generate or explain card, IMEI and account check digits.")]
        public static int LuhnCheckDigit([Description("The digits without their check digit.")] string digits)
        {
            var digit = FunctionToolInput.Required(digits, nameof(digits), 64).LuhnCheckDigit();

            if (digit < 0)
            {
                throw new ArgumentException("digits must contain only 0-9 and spaces.", nameof(digits));
            }

            return digit;
        }

        [FunctionTool("IsCardExpiryValid")]
        [Description(
            "Whether a card expiry written MM/YY or MM/YYYY is still valid at a given date; a card is valid through the last day of its expiry month. Use CurrentUtcTime for today.")]
        public static bool IsCardExpiryValid(
            [Description("The expiry, such as 12/26 or 12/2026.")]
            string expiry,
            [Description("The date to check validity at, in ISO 8601 form.")]
            DateTime asOf)
        {
            return FunctionToolInput.Text(expiry, nameof(expiry), 16).IsValidCardExpiryAsOf(asOf);
        }

        [FunctionTool("IsValidAbaRoutingNumber")]
        [Description(
            "Whether a nine-digit US ABA routing transit number passes its 3-7-1 weighted checksum; 021000021 does.")]
        public static bool IsValidAbaRoutingNumber([Description("The nine-digit routing number.")] string routingNumber)
        {
            return FunctionToolInput.Text(routingNumber, nameof(routingNumber), 32).IsValidAbaRoutingNumber();
        }

        [FunctionTool("IsValidIsin")]
        [Description(
            "Whether a 12-character ISIN (International Securities Identification Number) has a valid country prefix shape and Luhn check digit; US0378331005 does.")]
        public static bool IsValidIsin([Description("The ISIN to check.")] string isin)
        {
            return FunctionToolInput.Text(isin, nameof(isin), 32).IsValidIsin();
        }

        [FunctionTool("IsValidLei")]
        [Description(
            "Whether a 20-character LEI (Legal Entity Identifier) passes its ISO 17442 mod-97 check digits. A valid checksum does not prove the entity is registered.")]
        public static bool IsValidLei([Description("The LEI to check.")] string lei)
        {
            return FunctionToolInput.Text(lei, nameof(lei), 32).IsValidLei();
        }

        [FunctionTool("IsValidCusip")]
        [Description(
            "Whether a nine-character CUSIP (North American security identifier) has a valid check digit; 037833100 does.")]
        public static bool IsValidCusip([Description("The CUSIP to check.")] string cusip)
        {
            return FunctionToolInput.Text(cusip, nameof(cusip), 32).IsValidCusip();
        }

        [FunctionTool("IsValidSedol")]
        [Description(
            "Whether a seven-character SEDOL (London Stock Exchange security identifier) has a valid weighted check digit and no vowels; 0263494 does.")]
        public static bool IsValidSedol([Description("The SEDOL to check.")] string sedol)
        {
            return FunctionToolInput.Text(sedol, nameof(sedol), 32).IsValidSedol();
        }
    }
}