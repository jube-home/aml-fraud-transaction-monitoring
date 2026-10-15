/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.Data.Poco;
using Jube.Data.Repository;

namespace Jube.Service.AskJoobyExport
{
    public sealed class AskJoobyFileExportClient(AskJoobyFileRepository fileRepository, int? conversationId)
        : IFileExportClient
    {
        public async Task<int> ExportAsync(byte[] content, string fileName, string contentType,
            CancellationToken token = default)
        {
            var saved = await fileRepository.InsertAsync(new AskJoobyFile
            {
                AskJoobyConversationId = conversationId,
                Object = content,
                Name = fileName,
                ContentType = contentType,
                Extension = Path.GetExtension(fileName).ToLowerInvariant(),
                Size = content.LongLength
            }, token).ConfigureAwait(false);

            return saved.Id;
        }
    }
}