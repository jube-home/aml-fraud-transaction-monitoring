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

using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.FormModel
{
    [Trait("Category", "Unit")]
    public sealed class RequestLocalisationTests
    {
        private static async Task<(string Culture, string UiCulture)> Run(string? acceptLanguage)
        {
            var captured = ("", "");
            var middleware = new RequestLocalizationMiddleware(_ =>
            {
                captured = (CultureInfo.CurrentCulture.Name, CultureInfo.CurrentUICulture.Name);

                return Task.CompletedTask;
            }, Options.Create(RequestLocalisation.Options()), NullLoggerFactory.Instance);
            var context = new DefaultHttpContext();

            if (acceptLanguage != null)
            {
                context.Request.Headers.AcceptLanguage = acceptLanguage;
            }

            await middleware.Invoke(context);

            return captured;
        }

        [Theory]
        [InlineData("pl", "pl")]
        [InlineData("pl-PL,pl;q=0.9,en;q=0.8", "pl")]
        [InlineData("ar", "ar")]
        [InlineData("en", "en")]
        [InlineData("en-GB", "en")]
        [InlineData("fr", "en")]
        [InlineData("de-DE", "en")]
        public async Task TheLanguageTheClientAsksForPicksTheResourceCultureAndAnUnsupportedOneFallsBackToEnglish(
            string header, string expected)
        {
            var (_, ui) = await Run(header);

            ui.Should().Be(expected);
        }

        [Theory]
        [InlineData("pl")]
        [InlineData("ar")]
        [InlineData("fr")]
        public async Task TheFormattingCultureStaysEnglishWhateverTheClientAsksFor(string header)
        {
            var (culture, _) = await Run(header);

            culture.Should().Be("en");
        }

        [Fact]
        public async Task NoLanguageHeaderGivesEnglish()
        {
            var (culture, ui) = await Run(null);

            (culture, ui).Should().Be(("en", "en"));
        }
    }
}
