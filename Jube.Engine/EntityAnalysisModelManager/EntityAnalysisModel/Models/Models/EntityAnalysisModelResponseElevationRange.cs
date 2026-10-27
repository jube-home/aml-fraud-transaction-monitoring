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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    public class EntityAnalysisModelResponseElevationRange
    {
        public int Id { get; init; }
        public Guid Guid { get; init; }
        public string Name { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public bool Decline { get; set; }
        public bool Hold { get; set; }
        public string Content { get; set; }
        public string Redirect { get; set; }
        public string ForeColor { get; set; }
        public string BackColor { get; set; }
        public bool EnableHttpEndpoint { get; set; }
        public string HttpEndpoint { get; set; }
        public int HttpEndpointTypeId { get; set; }
        public string HttpEndpointHeaderName { get; set; }
        public string HttpEndpointCredentialVariable { get; set; }
        public bool EnableNotification { get; set; }
        public int NotificationTypeId { get; set; }
        public string NotificationDestination { get; set; }
        public string NotificationSubject { get; set; }
        public string NotificationBody { get; set; }

        public bool Contains(int level)
        {
            return level >= Minimum && level <= Maximum;
        }
    }
}
