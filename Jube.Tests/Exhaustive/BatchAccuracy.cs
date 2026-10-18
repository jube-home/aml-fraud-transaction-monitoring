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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Exhaustive
{
    public class Accuracy(ITestOutputHelper testOutputHelper)
    {
        internal static double AssertAccuracy(int countCorrect, int countRows, double passThreshold)
        {
            countRows.Should().BeGreaterThan(0, "no rows were scored");
            var percentageCorrect = (double)countCorrect / countRows;
            percentageCorrect.Should().BeGreaterThan(passThreshold);

            return percentageCorrect;
        }

        [Fact]
        public void AssertAccuracy_BelowThreshold_Fails()
        {
            var act = () => AssertAccuracy(70, 100, 0.8);
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void AssertAccuracy_NoRows_Fails()
        {
            var act = () => AssertAccuracy(0, 0, 0.8);
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void AssertAccuracy_AboveThreshold_Passes()
        {
            AssertAccuracy(85, 100, 0.8).Should().BeApproximately(0.85, 1e-9);
        }

        [RequiresLiveAppTheory]
        [InlineData("https://localhost:5001/api/Invoke/ExhaustiveSearchInstance/111c17f5-01d8-46a6-a940-273660a93a17",
            10000000, 0.8)]
        public async Task BatchAccuracyAsync(string uriString, int httpTimeout, double passThreshold)
        {
            var uri = new Uri(uriString);

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.MaxConnectionsPerServer = 30;
            httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            using var client = new HttpClient(httpClientHandler);
            client.Timeout = TimeSpan.FromMilliseconds(httpTimeout);

            await using var fileStream = File.OpenRead("Exhaustive/Mock.csv");
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true);

            var fields = (await streamReader.ReadLineAsync())!.Split(",").Select(split => "Abstraction." + split)
                .ToList();

            var countRows = 0;
            var countCorrect = 0;

            while (await streamReader.ReadLineAsync() is { } line)
            {
                var splits = line.Split(",");
                var model = new Dictionary<string, double>();

                for (var j = 0; j < fields.Count; j++)
                {
                    model.Add(fields[j], double.Parse(splits[j]));
                }

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8,
                        "application/json")
                };

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();

                var recall = double.Parse(await response.Content.ReadAsStringAsync());

                if ((recall > 0.5 ? 1 : 0) == (int)model["Abstraction.Dependent"])
                {
                    countCorrect += 1;
                }

                countRows += 1;
            }

            testOutputHelper.WriteLine($"{countCorrect} of {countRows} correct.");
            AssertAccuracy(countCorrect, countRows, passThreshold);
        }
    }
}