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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Test.Service.Query.CaseHold.Models
{
    internal sealed class Receiver : IDisposable
    {
        private readonly HttpListener listener = new();
        private readonly List<ReceivedCall> calls = [];

        public Receiver(int status = 200)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            BaseUrl = $"http://127.0.0.1:{port}";
            listener.Prefixes.Add(BaseUrl + "/");
            listener.Start();

            _ = Task.Run(async () =>
            {
                while (listener.IsListening)
                {
                    HttpListenerContext context;

                    try
                    {
                        context = await listener.GetContextAsync();
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                    var body = await reader.ReadToEndAsync();

                    lock (calls)
                    {
                        calls.Add(new ReceivedCall(context.Request.HttpMethod,
                            context.Request.Url!.PathAndQuery, context.Request.Headers["X-Hook-Key"], body));
                    }

                    await Task.Delay(100);
                    context.Response.StatusCode = status;
                    context.Response.Close();
                }
            });
        }

        public string BaseUrl { get; }

        public List<ReceivedCall> Calls()
        {
            lock (calls)
            {
                return [.. calls];
            }
        }

        public void Dispose()
        {
            listener.Stop();
            listener.Close();
        }
    }
}
