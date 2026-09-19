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

// ReSharper disable once RedundantUsingDirective

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public static class Payloads
    {
        public static PayloadBuilder Example()
        {
            return new PayloadBuilder(exampleFields);
        }

        private static readonly (string Name, string Value)[] exampleFields =
        [
            ("AccountId", "Test1"),
            ("TxnId", "0987654321"),
            ("TxnDateTime", "2018-08-19T21:41:37.247"),
            ("Currency", "826"),
            ("ResponseCode", "0"),
            ("CurrencyAmount", "123.45"),
            ("SettlementAmount", "100000"),
            ("AccountCurrency", "566"),
            ("IP", "123.456.789.200"),
            ("DeviceId", "OlaRoseGoldPhone6"),
            ("ChannelId", "1"),
            ("AppVersionCode", "12.34"),
            ("ServiceCode", "DID"),
            ("System", "Android"),
            ("Brand", "ZTE"),
            ("Model", "Barby"),
            ("AccountLongitude", "36.1408"),
            ("AccountLatitude", "5.3536"),
            ("OS", "Lollypop"),
            ("Resolution", "720*1280"),
            ("DebuggerAttached", "True"),
            ("SimulatorAttached", "True"),
            ("Jailbreak", "True"),
            ("MAC", "94:23:44f:2:d3"),
            ("ToAccountId", "MTN"),
            ("ToAccountExternalRef", "ChurchmanR"),
            ("TwoFATypeId", "SMS"),
            ("TwoFAResponseId", "1"),
            ("TransactionExternalResponseId", "0"),
            ("Storage", "True"),
            ("FingerprintHash", "jhjkhjkhsjh2hjhjkhj2k"),
            ("IndustryName", "GAMING"),
            ("BusinessModel", "Sports betting"),
            ("AmountEUR", "100.0000"),
            ("AmountEURRate", "1.0000000000"),
            ("AmountUSD", "113.0550"),
            ("AmountUSDRate", "1.1305502954"),
            ("AmountGBP", "86.5866"),
            ("AmountGBPRate", "0.8658658602"),
            ("Is3D", "False"),
            ("OriginalAmount", "100.0000"),
            ("OriginalCurrency", "EUR"),
            ("Email", "please@hash.me"),
            ("CreditCardHash", "0xDA39A3EE5E6B4B0JJJ1890AFD80709"),
            ("AcquirerBankName", "Caixa"),
            ("ActionDate", "2019-04-17 01:18:15Z"),
            ("APMAccountId", ""),
            ("BankId", "57"),
            ("BillingAddress", "Address Line 1"),
            ("BillingCity", "Address Line 2"),
            ("BillingCountry", "DE"),
            ("BillingFirstName", "Robert"),
            ("BillingLastName", "Mugabe"),
            ("BillingPhone", "1234567890"),
            ("BillingState", ""),
            ("BillingZip", "123456"),
            ("IsAPM", "True"),
            ("IsCascaded", "False"),
            ("IsCredited", "False"),
            ("IsCurrencyConverted", "False"),
            ("IsModification", "False"),
            ("IsModified", "False"),
            ("IsRebill", "False"),
            ("OrderId", "10607324128"),
            ("TransactionTypeId", "1000"),
            ("TransactionResultId", "1006"),
        ];
    }
}