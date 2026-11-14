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

using System.Text.RegularExpressions;
using log4net;

namespace Jube.Case.Outbound
{
    public static class PayloadEndpointCall
    {
        private static readonly Regex tokenPattern = new(@"\[@(.*?)@\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        public static bool IsWellFormedTemplate(string? template)
        {
            return WebhookTarget.Resolve(template, text => tokenPattern.Replace(text, "x")).Refusal == null;
        }

        public static async Task<DispatchResult> SendAsync(DynamicEnvironment.DynamicEnvironment environment,
            string? template, Func<string, string> render, bool post, Func<string> body, ILog log,
            CancellationToken token = default)
        {
            var (url, refusal) = WebhookTarget.Resolve(template, render);

            if (url == null)
            {
                return DispatchResult.Refused(refusal ?? PolicyCode.NotAbsolute);
            }

            var guarded = GuardedHttpEndpoint.For(environment);

            return post
                ? await guarded.PostAsync(url, body(), log, token: token).ConfigureAwait(false)
                : await guarded.GetAsync(url, log, token: token).ConfigureAwait(false);
        }
    }
}
