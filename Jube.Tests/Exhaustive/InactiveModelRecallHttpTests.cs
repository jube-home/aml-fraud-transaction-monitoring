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

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class InactiveModelRecallHttpTests(ModelScaffoldFixture fixture)
        : IClassFixture<ModelScaffoldFixture>
    {
        private const string Json = "application/json";
        private const string Boundary = "----InactiveModelRecallHttpTestsBoundary";
        private const string Body = "{\"Payload.Field0\":1,\"Payload.Field1\":1}";

        private static DbContext OpenDb()
        {
            return DataConnectionDbContext.GetResilientDbContextDataConnection(ModelScaffold.ConnectionString,
                TestLog.NoOp);
        }

        private static byte[] MultipartCsv(string csv)
        {
            var body = "--" + Boundary + "\r\n" +
                       "Content-Disposition: form-data; name=\"file\"; filename=\"rows.csv\"\r\n" +
                       "Content-Type: text/csv\r\n\r\n" +
                       csv + "\r\n" +
                       "--" + Boundary + "--\r\n";

            return Encoding.UTF8.GetBytes(body);
        }

        [Fact]
        public async Task AnInactiveInstanceStillRecallsThroughTheDirectInvokePathAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold, 0);

            try
            {
                var response = await fixture.Api.SendAsync("POST",
                    "/api/Invoke/ExhaustiveSearchInstance/" + seed.InstanceGuid, scaffold.UserName,
                    Encoding.UTF8.GetBytes(Body), Json);

                Assert.Equal(200, response.Status);

                var score = response.Json.GetProperty("score").GetDouble();
                Assert.InRange(score, 0, 1);

                var relevance = response.Json.GetProperty("relevance").EnumerateArray().ToArray();
                Assert.Equal(2, relevance.Length);
                Assert.Equal("Payload.Field0", relevance[0].GetProperty("name").GetString());
                Assert.Equal("Payload.Field1", relevance[1].GetProperty("name").GetString());
            }
            finally
            {
                await seed.DeleteAsync();
            }
        }

        [Fact]
        public async Task AnInactiveInstanceStillScoresThroughTheBatchInvokePathAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold, 0);

            try
            {
                var response = await fixture.Api.SendAsync("POST",
                    "/api/Invoke/ExhaustiveSearchInstance/" + seed.InstanceGuid + "/Batch", scaffold.UserName,
                    MultipartCsv("Payload.Field0,Payload.Field1\n1,1\n0,2\n"),
                    "multipart/form-data; boundary=" + Boundary);

                Assert.Equal(200, response.Status);

                var rows = response.Json.GetProperty("rows").EnumerateArray().ToArray();
                Assert.Equal(2, rows.Length);
                Assert.All(rows, r => Assert.InRange(r.GetProperty("score").GetDouble(), 0, 1));
            }
            finally
            {
                await seed.DeleteAsync();
            }
        }

        [Fact]
        public async Task ATenantForeignUserIsForbiddenOnTheDirectInvokePathForAnInactiveInstanceAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var foreign = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold, 0);

            try
            {
                var response = await fixture.Api.SendAsync("POST",
                    "/api/Invoke/ExhaustiveSearchInstance/" + seed.InstanceGuid, foreign.UserName,
                    Encoding.UTF8.GetBytes(Body), Json);

                Assert.Equal(403, response.Status);
            }
            finally
            {
                await seed.DeleteAsync();
            }
        }

        [Fact]
        public async Task ATenantUserWithoutTheModelRoleOrPermissionIsForbiddenForAnInactiveInstanceAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold, 0);

            try
            {
                var response = await fixture.Api.SendAsync("POST",
                    "/api/Invoke/ExhaustiveSearchInstance/" + seed.InstanceGuid, scaffold.OtherUserName,
                    Encoding.UTF8.GetBytes(Body), Json);

                Assert.Equal(403, response.Status);
            }
            finally
            {
                await seed.DeleteAsync();
            }
        }
    }
}