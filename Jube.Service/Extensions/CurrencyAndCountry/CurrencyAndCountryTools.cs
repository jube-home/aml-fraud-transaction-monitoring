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
using Jube.Service.Extensions.CurrencyAndCountry.Models;

namespace Jube.Service.Extensions.CurrencyAndCountry
{
    [FunctionToolGroup("CurrencyAndCountry",
        "ISO 4217 currency and ISO 3166 country reference data with exact minor-unit money conversion, from tables embedded in the product.")]
    public static class CurrencyAndCountryTools
    {
        [FunctionTool("CurrencyMinorUnitDigits")]
        [Description(
            "Number of decimal places an ISO 4217 currency uses: USD 2, JPY 0, KWD 3, CLF 4. Use it before storing, rounding or comparing money in minor units.")]
        public static int CurrencyMinorUnitDigits(
            [Description("Three-letter ISO 4217 currency code, such as EUR.")]
            string currencyCode)
        {
            var exponent = FunctionToolInput.Required(currencyCode, nameof(currencyCode), 16)
                .CurrencyMinorUnitExponent();

            if (exponent < 0)
            {
                throw new ArgumentException($"'{currencyCode}' is not an active ISO 4217 currency code.",
                    nameof(currencyCode));
            }

            return exponent;
        }

        [FunctionTool("IsValidCurrencyCode")]
        [Description(
            "Whether a three-letter code is an active ISO 4217 currency, case-insensitive. Use it to check payment currency fields.")]
        public static bool IsValidCurrencyCode([Description("The code to check, such as GBP.")] string currencyCode)
        {
            return FunctionToolInput.Text(currencyCode, nameof(currencyCode), 16).IsValidCurrencyCode();
        }

        [FunctionTool("ToMinorUnits")]
        [Description(
            "Converts a money amount to whole minor units for a currency, exactly and rounding halves away from zero; 19.99 USD is 1999 cents and 1500 JPY is 1500.")]
        public static double ToMinorUnits(
            [Description("The amount in major units, such as 19.99.")]
            double amount,
            [Description("Three-letter ISO 4217 currency code.")]
            string currencyCode)
        {
            return FunctionToolInput.Defined(amount.ToMinorUnits(CurrencyMinorUnitDigits(currencyCode)),
                "the amount is too large to convert exactly.");
        }

        [FunctionTool("FromMinorUnits")]
        [Description(
            "Converts whole minor units back to a money amount for a currency; 1999 USD cents is 19.99 and 1234 KWD fils is 1.234.")]
        public static double FromMinorUnits(
            [Description("The amount in minor units, such as cents.")]
            double minorUnits,
            [Description("Three-letter ISO 4217 currency code.")]
            string currencyCode)
        {
            return FunctionToolInput.Defined(minorUnits.FromMinorUnits(CurrencyMinorUnitDigits(currencyCode)),
                "the amount is too large to convert exactly.");
        }

        [FunctionTool("RoundToCurrency")]
        [Description(
            "Rounds a money amount to the precision of its currency, halves away from zero, using exact decimal arithmetic; 1234.5678 JPY is 1235 and in BHD is 1234.568.")]
        public static double RoundToCurrency(
            [Description("The money amount to round.")]
            double amount,
            [Description("Three-letter ISO 4217 currency code.")]
            string currencyCode)
        {
            CurrencyMinorUnitDigits(currencyCode);

            return FunctionToolInput.Defined(amount.RoundToCurrency(currencyCode),
                "the amount is too large to round exactly.");
        }

        [FunctionTool("ConvertAtExchangeRate")]
        [Description(
            "Multiplies an amount by a supplied exchange rate; 100 at 1.25 is 125. This tool knows no live rates, so the rate must come from the conversation or data.")]
        public static double ConvertAtExchangeRate(
            [Description("The amount in the source currency.")]
            double amount,
            [Description("Units of target currency per one unit of source currency; must be positive.")]
            double rate)
        {
            return FunctionToolInput.Defined(amount.ConvertAtRate(rate), "the rate must be a positive finite number.");
        }

        [FunctionTool("IsValidCountryCode")]
        [Description(
            "Whether a two-letter code is an ISO 3166-1 alpha-2 country code, case-insensitive; GB is valid and UK is not. Use it to check country fields.")]
        public static bool IsValidCountryCode([Description("The two-letter code to check.")] string countryCode)
        {
            return FunctionToolInput.Text(countryCode, nameof(countryCode), 16).IsValidCountryCode();
        }

        [FunctionTool("DescribeCountry")]
        [Description(
            "Looks up an ISO 3166-1 country by alpha-2 or alpha-3 code and returns both codes and the English short name; DEU gives DE, DEU, Germany.")]
        public static CountryDetails DescribeCountry(
            [Description("An alpha-2 or alpha-3 country code.")]
            string countryCode)
        {
            var code = FunctionToolInput.Required(countryCode, nameof(countryCode), 16);

            return new CountryDetails(
                FunctionToolInput.Found(code.CountryAlpha2(), $"'{countryCode}' is not an ISO 3166-1 country code."),
                code.CountryAlpha3(),
                code.CountryName());
        }
    }
}