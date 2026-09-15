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

using System.ComponentModel;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.DockerEvent
{
    public sealed record DockerEventDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp Docker itself recorded this event at.")]
        DateTime OccurredDate,
        [property:
            Description(
                "Docker's own event category: container, network, volume, image, or (in Swarm scope) service/node/secret/config.")]
        string EventType,
        [property:
            Description(
                "Docker's own action string, e.g. start/stop/die/kill/oom/health_status/exec_create/exec_start/exec_die. exec_create/exec_start carry the exact \"docker exec\" command line verbatim as part of this text -- a genuine audit signal, but also means a command line containing a secret as a bare argument would be visible here too (an existing property of the Docker Engine API itself).")]
        string Action,
        [property:
            Description("Docker's own id for the actor this event happened to (container/network/volume/image id).")]
        string ActorId,
        [property: Description("The actor's name, when Docker reports one (usually present for containers).")]
        string? ActorName,
        [property: Description("\"local\" or \"swarm\".")]
        string Scope,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube.Monitoring sidecar instance that captured this row.")]
        string Instance);
}