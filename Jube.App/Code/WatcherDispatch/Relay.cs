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
using System.Text;
using System.Threading;
using Jube.App.Code.signalr;
using Jube.Data.Context;
using Jube.Data.Repository;
using LinqToDB.Configuration;
using log4net;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Jube.App.Code.WatcherDispatch
{
    public class Relay
    {
        private DefaultContractResolver _contractResolver;
        private DynamicEnvironment.DynamicEnvironment _environment;
        private ILog _log;
        private IModel _rabbitMqChannel;
        private IHubContext<WatcherHub> _watcherHub;
        private bool Stopping { get; set; }

        public void Start(IHubContext<WatcherHub> watcherHub,
            DynamicEnvironment.DynamicEnvironment environment, ILog log, IModel rabbitMqChannel,
            DefaultContractResolver contractResolver)
        {
            _watcherHub = watcherHub;
            _log = log;
            _environment = environment;
            _contractResolver = contractResolver;

            if (environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                _rabbitMqChannel = rabbitMqChannel;
                ConnectToAmqp();
            }
            else
            {
                if (environment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var fromDatabaseNotifications = new Thread(ConnectToDatabaseNotifications);
                    fromDatabaseNotifications.Start();
                }
                else
                {
                    if (!environment.AppSettings("ActivationWatcherAllowPersist")
                            .Equals("True", StringComparison.OrdinalIgnoreCase)) return;

                    var fromDbContext = new Thread(FromDbContext);
                    fromDbContext.Start();
                }
            }
        }

        private void EventHandlerDatabase(string payload)
        {
            try
            {
                _log.Info("Activation Relay: String representation of body received is " + payload + " .");

                var json = JObject.Parse(payload);
                var tenantRegistryId = (json.SelectToken("tenantRegistryId") ?? 0).Value<string>();

                _watcherHub.Clients.Group("Tenant_" + tenantRegistryId)
                    .SendAsync("ReceiveMessage", "RealTime", payload);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }

        private void EventHandlerSignalR(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                _log.Info("Activation Relay: Message Received.");

                var bodyString = Encoding.UTF8.GetString(e.Body.ToArray());

                _log.Info("Activation Relay: String representation of body received is " + bodyString + " .");

                var json = JObject.Parse(bodyString);
                var tenantRegistryId = (json.SelectToken("tenantRegistryId") ?? 0).Value<string>();

                _watcherHub.Clients.Group("Tenant_" + tenantRegistryId)
                    .SendAsync("ReceiveMessage", "RealTime", bodyString);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }

        private void ConnectToDatabaseNotifications()
        {
            try
            {
                var connection = new NpgsqlConnection(_environment.AppSettings("ConnectionString"));
                try
                {
                    connection.Open();

                    connection.Notification += (_, e)
                        => EventHandlerDatabase(e.Payload);

                    using (var cmd = new NpgsqlCommand("LISTEN activation", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    while (true) connection.Wait();
                }
                catch (Exception ex)
                {
                    _log.Error($"Streaming Activations Database: Has created an exception as {ex}.");
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Dispatch to SignalR: Error making connections for the Activation Watcher relay " + ex +
                           ".");
            }
        }

        private void ConnectToAmqp()
        {
            try
            {
                _rabbitMqChannel.ExchangeDeclare("jubeActivations", ExchangeType.Fanout);

                var rabbitMqQueueName = _rabbitMqChannel.QueueDeclare();
                _rabbitMqChannel.QueueBind(rabbitMqQueueName, "jubeActivations", "");

                var consumer = new EventingBasicConsumer(_rabbitMqChannel);
                consumer.Received += EventHandlerSignalR;

                _rabbitMqChannel.BasicConsume(rabbitMqQueueName, true, consumer);
            }
            catch (Exception ex)
            {
                _log.Error("Dispatch to SignalR: Error making connections for the Activation Watcher relay " + ex +
                           ".");
            }
        }

        private void FromDbContext()
        {
            _log.Info(
                $"Data Connection DbContext: Is about to attempt construction of database context with {_environment.AppSettings("ConnectionString")}.");

            var builder = new LinqToDbConnectionOptionsBuilder();
            builder.UsePostgreSQL(_environment.AppSettings("ConnectionString"));
            var connection = builder.Build<DbContext>();

            var dbContext = new DbContext(connection);

            _log.Info("Data Connection DbContext: Database context has been constructed.  Returning database context.");

            var activationWatcherRepository = new ActivationWatcherRepository(dbContext);

            var lastActivationWatcher = activationWatcherRepository.GetLast();
            var lastActivationWatcherId = 0;

            if (lastActivationWatcher != null) lastActivationWatcherId = lastActivationWatcher.Id;

            while (!Stopping)
            {
                foreach (var activationWatcher in activationWatcherRepository.GetAllSinceId(lastActivationWatcherId,
                             100))
                {
                    lastActivationWatcherId = activationWatcher.Id;

                    var stringRepresentationOfObj = JsonConvert.SerializeObject(activationWatcher,
                        new JsonSerializerSettings
                        {
                            ContractResolver = _contractResolver
                        });

                    _watcherHub.Clients.Group("Tenant_" + 1)
                        .SendAsync("ReceiveMessage", "RealTime", stringRepresentationOfObj);
                }

                Thread.Sleep(int.Parse(_environment.AppSettings("WaitPollFromActivationWatcherTable")));
            }
        }
    }
}