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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.EntityAnalysisModelSynchronisationNodeStatusEntries
{
    public class EntityAnalysisModelSynchronisationNodeStatusEntriesDto
    {
        [Description("True when a synchronisation has been scheduled and has come due, or the node has never " +
                     "synchronised, so the node has yet to pick up the latest model configuration.")]
        public bool SynchronisationPending { get; set; }

        [Description("True when the node has sent a heartbeat within the last two minutes.")]
        public bool InstanceAvailable { get; set; }

        [Description("Identifier of the node instance reporting its synchronisation status.")]
        public string? Instance { get; set; }

        [Description("UTC date the node last completed a synchronisation; the default date when it never has.")]
        public DateTime SynchronisedDate { get; set; }

        [Description("UTC date of the last heartbeat received from the node.")]
        public DateTime HeartbeatDate { get; set; }
    }
}