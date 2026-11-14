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

namespace Jube.Engine.Helpers
{
    using System;
    using Accord.Neuro;
    using Dictionary.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class JsonSerializationHelper
    {
        private readonly DefaultContractResolver defaultContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        public JsonSerializerSettings DefaultJsonSerializerSettingsSettings
        {
            get
            {
                var serializer = new JsonSerializerSettings
                {
                    ContractResolver = defaultContractResolver
                };

                serializer.Converters.Add(new DictionaryNoBoxingConverter());

                return serializer;
            }
        }

        public JsonSerializerSettings TopologyNetworkJsonSerializerSettings
        {
            get
            {
                return new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    SerializationBinder = new AnchoredAssemblySerializationBinder(typeof(ActivationNetwork)),
                    ContractResolver = defaultContractResolver
                };
            }
        }

        public JsonSerializerSettings DeserializeTopologyNetworkJsonSerializerSettings
        {
            get
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    SerializationBinder = new AnchoredAssemblySerializationBinder(typeof(ActivationNetwork)),
                    ContractResolver = defaultContractResolver
                };

                settings.Error += (_, args) =>
                {
                    if (args.ErrorContext.Error.InnerException is NotImplementedException)
                    {
                        return;
                    }

                    args.ErrorContext.Handled = true;
                };

                return settings;
            }
        }

        public JsonSerializer ArchiveJsonSerializer
        {
            get
            {
                var serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = defaultContractResolver
                };

                serializer.Converters.Add(new DictionaryNoBoxingConverter());

                return serializer;
            }
        }
    }
}
