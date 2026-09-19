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

using System;
using System.Threading;
using System.Threading.Tasks;
using Jube.Mfa;
using Jube.Mfa.Rsa;
using Jube.Service.Authentication;
using log4net;

namespace Jube.App.Code
{
    public sealed class RsaMfaVerifier(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log) : IMfaVerifier
    {
        public async Task<bool> VerifyAsync(string userName, string code, CancellationToken token = default)
        {
            var outboundHttpRsaAmCertificateBypass =
                (dynamicEnvironment.AppSettings("OutboundHttpRsaAmCertificateBypass") ?? "False")
                .Equals("True", StringComparison.CurrentCultureIgnoreCase);

            var options = new RsaSecurIdOptions
            {
                Endpoint = dynamicEnvironment.AppSettings("MultifactorAuthenticationEndpoint"),
                ClientId = dynamicEnvironment.AppSettings("MultifactorAuthenticationApplicationId"),
                ClientKey = dynamicEnvironment.AppSettings("MultifactorAuthenticationClientKey"),
                OutboundHttpCertificateBypass = outboundHttpRsaAmCertificateBypass,
                OutboundHttpCertificateThumbprint =
                    dynamicEnvironment.AppSettings("OutboundHttpRsaAmCertificateThumbprint")
            };

            IMfaProvider mfaProvider = new RsaSecurIdMfaProvider(options, log);

            var result = await mfaProvider.VerifyAsync(new MfaVerificationRequest
            {
                SubjectName = userName,
                Factors = [new MfaFactor { MethodId = "SECURID", Value = code }]
            }, token).ConfigureAwait(false);

            return result.IsSuccessful;
        }
    }
}