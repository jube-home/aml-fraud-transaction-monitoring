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
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using RabbitMQ.Client;

namespace Jube.Test.Service.ModelScaffolding
{
    public sealed class AmqpModelScaffoldFixture : ModelScaffoldFixture
    {
        protected override IReadOnlyDictionary<string, string> EngineSettings =>
            new Dictionary<string, string>
            {
                ["AMQP"] = BrokerAvailable ? "True" : "False",
                ["EnableCallback"] = "True",
                ["EnableNotification"] = "True"
            };

        private static bool BrokerAvailable =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BrokerFactAttribute.Variable));

        protected override IConnection? CreateRabbitMqConnection()
        {
            return BrokerAvailable
                ? new ConnectionFactory
                {
                    Uri = new Uri(Environment.GetEnvironmentVariable(BrokerFactAttribute.Variable)!)
                }.CreateConnection()
                : null;
        }
    }
}
