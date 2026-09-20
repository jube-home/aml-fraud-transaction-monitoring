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

using System.Collections.Generic;
using System.Linq;

namespace Jube.Test.Mocks
{
    public static class MocksCases
    {
        public const string ValidKey = "SomethingSecretForTheClientKeyHeader";
        private const string Json = "application/json";

        private static readonly string[] routes =
        [
            "BareNumber", "BareNumberArrayWrapped", "Minimal", "Glm", "RandomForestCalibrated",
            "RandomForestUncalibrated", "C5", "XgBoost", "SvmLinear", "SvmRadial", "SvmDecisionValueMode",
            "BayesianNetworkBootstrapStrength", "BayesianNetworkArcStrength", "NeuralNetwork", "ExpertRule",
            "StaleCalibration", "Suppressed", "LegacyError", "MalformedJson", "EmptyBody", "ServerError"
        ];

        private static string Otp(string value) =>
            "{\"clientId\":\"c\",\"subjectName\":\"s\",\"subjectCredentials\":[{\"methodId\":\"SECURID\"," +
            $"\"collectedInputs\":[{{\"name\":\"SECURID\",\"value\":\"{value}\"}}]}}],\"context\":{{}}}}";

        public static IEnumerable<MockRequest> All => HttpAdaptation.Concat(Mfa);

        public static IEnumerable<MockRequest> HttpAdaptation =>
            new[] { new MockRequest("index", "GET", "/api/MockHttpAdaptation") }
                .Concat(routes.Select(r => new MockRequest(r, "POST", $"/api/MockHttpAdaptation/{r}")))
                .Concat(
                [
                    new MockRequest("index-post-405", "POST", "/api/MockHttpAdaptation"),
                    new MockRequest("route-get-405", "GET", "/api/MockHttpAdaptation/BareNumber"),
                    new MockRequest("route-lowercase", "POST", "/api/mockhttpadaptation/barenumber"),
                    new MockRequest("route-trailing-slash", "POST", "/api/MockHttpAdaptation/Minimal/"),
                    new MockRequest("route-with-body", "POST", "/api/MockHttpAdaptation/Glm", Json, "{\"a\":1}"),
                    new MockRequest("route-unknown-404", "POST", "/api/MockHttpAdaptation/Nope"),
                    new MockRequest("index-head", "HEAD", "/api/MockHttpAdaptation")
                ]);

        public static IEnumerable<MockRequest> Mfa =>
        [
            new("mfa-success", "POST", "/api/Mfa", Json, Otp("12345678"), [ValidKey]),
            new("mfa-success-charset", "POST", "/api/Mfa", "application/json; charset=utf-8", Otp("12345678"),
                [ValidKey]),
            new("mfa-success-upper-content-type", "POST", "/api/Mfa", "APPLICATION/JSON", Otp("12345678"), [ValidKey]),
            new("mfa-success-json-suffix", "POST", "/api/Mfa", "application/json-patch+json", Otp("12345678"),
                [ValidKey]),
            new("mfa-wrong-otp", "POST", "/api/Mfa", Json, Otp("00000000"), [ValidKey]),
            new("mfa-null-otp", "POST", "/api/Mfa", Json,
                "{\"subjectCredentials\":[{\"collectedInputs\":[{\"name\":\"SECURID\"}]}]}", [ValidKey]),
            new("mfa-pascal-case-body", "POST", "/api/Mfa", Json,
                "{\"SubjectCredentials\":[{\"CollectedInputs\":[{\"Value\":\"12345678\"}]}]}", [ValidKey]),
            new("mfa-empty-credentials", "POST", "/api/Mfa", Json, "{\"subjectCredentials\":[]}", [ValidKey]),
            new("mfa-empty-inputs", "POST", "/api/Mfa", Json,
                "{\"subjectCredentials\":[{\"collectedInputs\":[]}]}", [ValidKey]),
            new("mfa-credential-without-inputs-list", "POST", "/api/Mfa", Json,
                "{\"subjectCredentials\":[{}]}", [ValidKey]),
            new("mfa-empty-object-body", "POST", "/api/Mfa", Json, "{}", [ValidKey]),
            new("mfa-null-literal-body", "POST", "/api/Mfa", Json, "null", [ValidKey]),
            new("mfa-empty-body", "POST", "/api/Mfa", Json, "", [ValidKey]),
            new("mfa-malformed-body", "POST", "/api/Mfa", Json, "{ not json", [ValidKey]),
            new("mfa-wrong-shape-body", "POST", "/api/Mfa", Json, "[1,2]", [ValidKey]),
            new("mfa-string-typed-number-value", "POST", "/api/Mfa", Json,
                "{\"subjectCredentials\":[{\"collectedInputs\":[{\"value\":12345678}]}]}", [ValidKey]),
            new("mfa-missing-key", "POST", "/api/Mfa", Json, Otp("12345678")),
            new("mfa-wrong-key", "POST", "/api/Mfa", Json, Otp("12345678"), ["nope"]),
            new("mfa-key-case-sensitive", "POST", "/api/Mfa", Json, Otp("12345678"), [ValidKey.ToLowerInvariant()]),
            new("mfa-two-key-lines", "POST", "/api/Mfa", Json, Otp("12345678"), [ValidKey, ValidKey]),
            new("mfa-no-content-type", "POST", "/api/Mfa", null, Otp("12345678"), [ValidKey]),
            new("mfa-text-plain", "POST", "/api/Mfa", "text/plain", Otp("12345678"), [ValidKey]),
            new("mfa-text-json", "POST", "/api/Mfa", "text/json", Otp("12345678"), [ValidKey]),
            new("mfa-missing-key-and-bad-type", "POST", "/api/Mfa", "text/plain", "x"),
            new("mfa-no-content-type-empty-body", "POST", "/api/Mfa", null, "", [ValidKey]),
            new("mfa-text-plain-empty-body", "POST", "/api/Mfa", "text/plain", "", [ValidKey]),
            new("mfa-xml-with-body", "POST", "/api/Mfa", "application/xml", "<a/>", [ValidKey]),
            new("mfa-vendor-json-suffix", "POST", "/api/Mfa", "application/vnd.rsa+json", Otp("12345678"), [ValidKey]),
            new("mfa-json-trailing-garbage", "POST", "/api/Mfa", Json, Otp("12345678") + " garbage", [ValidKey]),
            new("mfa-nested-wrong-type", "POST", "/api/Mfa", Json,
                "{\"subjectCredentials\":[{\"collectedInputs\":[{\"value\":{\"a\":1}}]}]}", [ValidKey]),
            new("mfa-unknown-members", "POST", "/api/Mfa", Json,
                "{\"extra\":1,\"subjectCredentials\":[{\"collectedInputs\":[{\"value\":\"12345678\"}]}]}", [ValidKey]),
            new("mfa-non-ascii-value", "POST", "/api/Mfa", Json,
                "{\"subjectCredentials\":[{\"collectedInputs\":[{\"value\":\"caf\u00e9\"}]}]}", [ValidKey]),
            new("mfa-get-405", "GET", "/api/Mfa")
        ];
    }
}