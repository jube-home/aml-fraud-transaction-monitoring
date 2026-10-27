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
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using Npgsql;
using RabbitMQ.Client;
using Xunit;

namespace Jube.Test.Service.ModelScaffolding
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ResponseHoldAmqpTests(AmqpModelScaffoldFixture fixture)
        : IClassFixture<AmqpModelScaffoldFixture>
    {
        private const string Credential = "JUBE_HOOK_AMQPTEST";

        private static async Task ExecuteAsync(string sql, int modelId)
        {
            await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("model", modelId);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task<ResponseHold?> WaitForHoldAsync(string guid, Func<ResponseHold, bool> ready)
        {
            var deadline = DateTime.UtcNow.AddSeconds(90);
            ResponseHold? hold = null;

            while (DateTime.UtcNow < deadline)
            {
                await using var connection = new NpgsqlConnection(ModelScaffold.ConnectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(
                    "SELECT \"Json\"::text FROM \"Archive\" WHERE \"EntityAnalysisModelInstanceEntryGuid\" = @guid",
                    connection);
                command.Parameters.AddWithValue("guid", Guid.Parse(guid));

                hold = ResponseHoldJson.Read(await command.ExecuteScalarAsync() as string);

                if (hold != null && ready(hold))
                {
                    return hold;
                }

                await Task.Delay(500);
            }

            return hold;
        }

        private static (HttpListener Listener, ConcurrentQueue<(string Path, string Header)> Received) Receiver(
            out int port)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            var received = new ConcurrentQueue<(string, string)>();

            _ = Task.Run(async () =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
                        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                        await reader.ReadToEndAsync();
                        received.Enqueue((context.Request.Url!.AbsolutePath, context.Request.Headers["X-Test"] ?? ""));
                        context.Response.StatusCode = 200;
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

        [BrokerFact]
        public async Task TheWebHookAndTheNotificationOfARangeTravelThroughTheBrokerAndTheirOutcomesAreRecordedAsync()
        {
            Environment.SetEnvironmentVariable(Credential, "amqp-token-1");
            var (listener, received) = Receiver(out var port);

            try
            {
                await using var scaffold = await fixture.CreateIsolatedAsync();

                await ExecuteAsync($"""
                                   UPDATE "EntityAnalysisModelResponseElevationRange"
                                   SET "Hold" = 1, "Decline" = 0, "Active" = 1, "Name" = 'Review hold',
                                       "EnableHttpEndpoint" = 1,
                                       "HttpEndpoint" = 'http://127.0.0.1:{port}/hook/[@Payload.AccountId@]',
                                       "HttpEndpointTypeId" = 1, "HttpEndpointHeaderName" = 'X-Test',
                                       "HttpEndpointCredentialVariable" = '{Credential}',
                                       "EnableNotification" = 1, "NotificationTypeId" = 1,
                                       "NotificationDestination" = 'analyst@example.invalid',
                                       "NotificationSubject" = 'Held', "NotificationBody" = 'A payment is held'
                                   WHERE "EntityAnalysisModelId" = @model AND coalesce("Deleted",0) = 0;
                                   """, scaffold.ModelId);

                await ModelSync.SyncAsync(fixture.Engine, scaffold);

                var accountId = "ZzTestAmqp" + Guid.NewGuid().ToString("N");

                var response = await fixture.Api.InvokeAsync(scaffold, Payloads.Example().With("AccountId", accountId));

                Assert.Equal(200, response.Status);
                Assert.Equal("Review hold", response.String("ResponseElevation", "Name"));

                var guid = response.String("EntityAnalysisModelInstanceEntryGuid")!;

                var hold = await WaitForHoldAsync(guid,
                               h => h.Legs.Any(l => l.KindId == (int)ResponseHoldLegKind.Webhook &&
                                                    l.OutcomeId == (int)ResponseHoldLegOutcome.Sent)
                                    && h.Legs.Any(l => l.KindId == (int)ResponseHoldLegKind.Email &&
                                                       l.OutcomeId != (int)ResponseHoldLegOutcome.Queued))
                           ?? throw new InvalidOperationException("The hold was never archived.");

                Assert.Equal((int)ResponseHoldState.Held, hold.StateId);

                var call = Assert.Single(received);
                Assert.Equal("/hook/" + accountId, call.Path);
                Assert.Equal("amqp-token-1", call.Header);

                Assert.Contains(hold.Legs, l => l.KindId == (int)ResponseHoldLegKind.Email
                                                && l.OutcomeId == (int)ResponseHoldLegOutcome.Failed);

                using var probe = new ConnectionFactory
                {
                    Uri = new Uri(Environment.GetEnvironmentVariable(BrokerFactAttribute.Variable)!)
                }.CreateConnection();
                using var channel = probe.CreateModel();

                Assert.Equal(1u, channel.ConsumerCount("jubeWebhooks"));
                Assert.Equal(1u, channel.ConsumerCount("jubeNotifications"));
                Assert.Equal(0u, channel.MessageCount("jubeWebhooks"));
                Assert.Equal(0u, channel.MessageCount("jubeNotifications"));
            }
            finally
            {
                listener.Stop();
                Environment.SetEnvironmentVariable(Credential, null);
            }
        }
    }
}
