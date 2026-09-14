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

using Jube.OpenTelemetryListener;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(4318));

var app = builder.Build();

app.MapPost("/v1/{signal}", async (string signal, HttpRequest request) =>
{
    using var memoryStream = new MemoryStream();
    await request.Body.CopyToAsync(memoryStream);
    var body = memoryStream.ToArray();

    var strings = ProtobufStringScanner.ExtractStrings(body);

    Console.WriteLine(
        $"[{DateTime.UtcNow:O}] POST /v1/{signal} contentType={request.ContentType} bytes={body.Length} " +
        $"strings=[{string.Join(", ", strings.Distinct())}]");

    return Results.Ok();
});

Console.WriteLine(
    "Jube.OpenTelemetryListener starting on port 4318 (OTLP/HTTP). Not a real collector -- see Program.cs.");

app.Run();