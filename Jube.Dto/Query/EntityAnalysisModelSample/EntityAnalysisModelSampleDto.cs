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
namespace Jube.Dto.Query.EntityAnalysisModelSample
{
    public class EntityAnalysisModelSampleOptionsDto
    {
        [Description("Guid of the Entity Analysis Model to sample archived transactions from.")]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("Start of the archive ReferenceDate window to sample from (required).")]
        public DateTimeOffset? DateFrom { get; set; }

        [Description("End of the archive ReferenceDate window to sample from (required, must be after DateFrom).")]
        public DateTimeOffset? DateTo { get; set; }

        [Description("Sampling rate between 0 and 1 inclusive; each archived row is included with this probability.")]
        public double Sample { get; set; }
    }

    [Description("A generated CSV sample file: UTF-8 encoded content, its file name and its content type.")]
    public sealed record EntityAnalysisModelSampleFileDto(byte[] Content, string FileName, string ContentType);
}