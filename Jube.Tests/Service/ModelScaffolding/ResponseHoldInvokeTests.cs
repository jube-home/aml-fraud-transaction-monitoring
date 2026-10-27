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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Jube.Data.Repository.Models.Hold;
using Jube.Dto.Hold;
using Jube.Dto.Options;
using Jube.Test.Infrastructure.ModelScaffolding;
using Npgsql;
using Xunit;

namespace Jube.Test.Service.ModelScaffolding
{
    [Collection("Database")]
    public sealed class ResponseHoldInvokeTests(ModelScaffoldFixture fixture) : IClassFixture<ModelScaffoldFixture>
    {
        private const string Secret = "JUBE_HOOK_ENGINETEST";

        private static async Task ExecuteAsync(string sql, int modelId, params (string Name, object Value)[] values)
        {
            await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("model", modelId);

            foreach (var (name, value) in values)
            {
                command.Parameters.AddWithValue(name, value);
            }

            await command.ExecuteNonQueryAsync();
        }

        private static async Task<string?> ArchiveJsonAsync(string guid)
        {
            await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT \"Json\"::text FROM \"Archive\" WHERE \"EntityAnalysisModelInstanceEntryGuid\" = @guid",
                connection);
            command.Parameters.AddWithValue("guid", Guid.Parse(guid));

            return await command.ExecuteScalarAsync() as string;
        }

        private static async Task<ResponseHold?> WaitForHoldAsync(string guid, Func<ResponseHold, bool> ready)
        {
            var deadline = DateTime.UtcNow.AddSeconds(90);

            while (DateTime.UtcNow < deadline)
            {
                var json = await ArchiveJsonAsync(guid);
                var hold = ResponseHoldJson.Read(json);

                if (hold != null && ready(hold))
                {
                    return hold;
                }

                await Task.Delay(500);
            }

            return ResponseHoldJson.Read(await ArchiveJsonAsync(guid));
        }

        private static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return port;
        }

        private static (HttpListener Listener, ConcurrentQueue<(string Path, string Header, string Body)> Received) Receiver(
            int port, int status)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();

            var received = new ConcurrentQueue<(string, string, string)>();

            _ = Task.Run(async () =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
                        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);

                        received.Enqueue((context.Request.Url!.AbsolutePath,
                            context.Request.Headers["X-Test"] ?? string.Empty, await reader.ReadToEndAsync()));

                        context.Response.StatusCode = status;
                        context.Response.Close();
                    }
                    catch (Exception) when (!listener.IsListening)
                    {
                        return;
                    }
                }
            });

            return (listener, received);
        }

        private async Task ConfigureRangesAsync(ModelScaffold scaffold, bool hold, bool decline, int port)
        {
            await ExecuteAsync("""
                               UPDATE "EntityAnalysisModelResponseElevationRange"
                               SET "Hold" = @hold, "Decline" = @decline, "Active" = 1, "Name" = 'Review hold',
                                   "EnableHttpEndpoint" = 1,
                                   "HttpEndpoint" = @url, "HttpEndpointTypeId" = 1,
                                   "HttpEndpointHeaderName" = 'X-Test', "HttpEndpointCredentialVariable" = 'JUBE_HOOK_ENGINETEST'
                               WHERE "EntityAnalysisModelId" = @model AND coalesce("Deleted",0) = 0;
                               """, scaffold.ModelId, ("hold", (short)(hold ? 1 : 0)),
                ("decline", (short)(decline ? 1 : 0)),
                ("url", $"http://127.0.0.1:{port}/hook/[@Payload.AccountId@]?name=[@Hold.Name@]"));

            await ModelSync.SyncAsync(fixture.Engine, scaffold);
        }

        [Fact]
        public async Task AHoldRangeStampsTheHoldNamesTheElevationAndSendsTheWebHookWithItsCredentialAsync()
        {
            Environment.SetEnvironmentVariable(Secret, "token-123");
            var port = FreePort();
            var (listener, received) = Receiver(port, 200);

            try
            {
                await using var scaffold = await fixture.CreateIsolatedAsync();
                await ConfigureRangesAsync(scaffold, true, false, port);

                var accountId = "ZzTestHold" + Guid.NewGuid().ToString("N");

                var response = await fixture.Api.InvokeAsync(scaffold, Payloads.Example().With("AccountId", accountId));

                Assert.Equal(200, response.Status);
                Assert.True(response.ResponseElevationValue > 0);
                Assert.Equal("Review hold", response.String("ResponseElevation", "Name"));

                var guid = response.String("EntityAnalysisModelInstanceEntryGuid")!;

                var hold = await WaitForHoldAsync(guid, h => h.Legs.Any(a => a.OutcomeId == (int)ResponseHoldLegOutcome.Sent))
                           ?? throw new InvalidOperationException("The hold was never archived.");

                Assert.Equal((int)ResponseHoldState.Held, hold.StateId);
                Assert.Equal("Review hold", hold.RangeName);
                Assert.Equal((int)response.ResponseElevationValue, hold.Elevation);
                Assert.Contains(hold.Legs, l => l.KindId == (int)ResponseHoldLegKind.Webhook
                                                && l.OutcomeId == (int)ResponseHoldLegOutcome.Queued);

                var sent = hold.Legs.Single(l => l.OutcomeId == (int)ResponseHoldLegOutcome.Sent);
                Assert.Equal(200, sent.HttpStatus);

                var call = Assert.Single(received);
                Assert.Equal("/hook/" + accountId, call.Path);
                Assert.Equal("token-123", call.Header);
                Assert.Contains(guid, call.Body);

                Assert.DoesNotContain("token-123", await ArchiveJsonAsync(guid) ?? string.Empty);
            }
            finally
            {
                listener.Stop();
                Environment.SetEnvironmentVariable(Secret, null);
            }
        }

        [Fact]
        public async Task ADeclineRangeIsStampedDeclinedNeverHeldAsync()
        {
            var port = FreePort();
            var (listener, _) = Receiver(port, 200);

            try
            {
                await using var scaffold = await fixture.CreateIsolatedAsync();
                await ConfigureRangesAsync(scaffold, true, true, port);

                var response = await fixture.Api.InvokeAsync(scaffold,
                    Payloads.Example().With("AccountId", "ZzTestDecline" + Guid.NewGuid().ToString("N")));

                var guid = response.String("EntityAnalysisModelInstanceEntryGuid")!;
                var hold = await WaitForHoldAsync(guid, _ => true)
                           ?? throw new InvalidOperationException("The hold was never archived.");

                Assert.Equal((int)ResponseHoldState.Declined, hold.StateId);
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ARangeWithNeitherOutcomeNamesTheElevationButStampsNoHoldAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();

            await ExecuteAsync("""
                               UPDATE "EntityAnalysisModelResponseElevationRange"
                               SET "Hold" = 0, "Decline" = 0, "Name" = 'Plain', "EnableHttpEndpoint" = 0, "EnableNotification" = 0
                               WHERE "EntityAnalysisModelId" = @model AND coalesce("Deleted",0) = 0;
                               """, scaffold.ModelId);

            await ModelSync.SyncAsync(fixture.Engine, scaffold);

            var response = await fixture.Api.InvokeAsync(scaffold,
                Payloads.Example().With("AccountId", "ZzTestPlain" + Guid.NewGuid().ToString("N")));

            Assert.Equal("Plain", response.String("ResponseElevation", "Name"));

            var guid = response.String("EntityAnalysisModelInstanceEntryGuid")!;
            var deadline = DateTime.UtcNow.AddSeconds(60);
            string? json = null;

            while (DateTime.UtcNow < deadline && json == null)
            {
                json = await ArchiveJsonAsync(guid);
                await Task.Delay(500);
            }

            Assert.NotNull(json);
            Assert.Null(ResponseHoldJson.Read(json));
        }

        [Fact]
        public async Task AWebHookWhoseHostIsNotLiteralIsRefusedAndRecordedNotSentAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();

            await ExecuteAsync("""
                               UPDATE "EntityAnalysisModelResponseElevationRange"
                               SET "Hold" = 1, "Decline" = 0, "Name" = 'Review hold', "EnableHttpEndpoint" = 1,
                                   "HttpEndpoint" = 'http://[@Payload.AccountId@]/hook', "HttpEndpointTypeId" = 1
                               WHERE "EntityAnalysisModelId" = @model AND coalesce("Deleted",0) = 0;
                               """, scaffold.ModelId);

            await ModelSync.SyncAsync(fixture.Engine, scaffold);

            var response = await fixture.Api.InvokeAsync(scaffold,
                Payloads.Example().With("AccountId", "ZzTestRefused" + Guid.NewGuid().ToString("N")));

            var hold = await WaitForHoldAsync(response.String("EntityAnalysisModelInstanceEntryGuid")!, _ => true)
                       ?? throw new InvalidOperationException("The hold was never archived.");

            var leg = Assert.Single(hold.Legs);
            Assert.Equal((int)ResponseHoldLegOutcome.Refused, leg.OutcomeId);
            Assert.Equal((int)ResponseHoldLegKind.Webhook, leg.KindId);
        }
    }
}
