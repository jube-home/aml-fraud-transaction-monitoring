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

using Jube.Dto.Repository.CaseFile;
using Jube.Dto.Repository.CaseResearch;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseResearch;
using RulePoco = Jube.Data.Poco.CaseResearch;
using SourcePoco = Jube.Data.Poco.CaseResearchCitation;

namespace Jube.Service.Repository.CaseResearch
{
    internal static class CaseResearchMapper
    {
        public static CaseResearchDto ToDto(RulePoco research, IEnumerable<SourcePoco> sources,
            IEnumerable<CaseFileDto>? files = null) => new()
        {
            Id = research.Id,
            CaseId = research.CaseId,
            CaseKey = research.CaseKey,
            CaseKeyValue = research.CaseKeyValue,
            Topic = HtmlSanitiser.Sanitise(research.Topic) ?? string.Empty,
            Findings = HtmlSanitiser.Sanitise(research.Findings) ?? string.Empty,
            SourceId = research.SourceId,
            Sources = sources.Where(w => CitationAddress.IsWeb(w.Url)).Select(ToDto).ToList(),
            Files = files?.ToList() ?? [],
            CreatedUser = research.CreatedUser,
            CreatedDate = research.CreatedDate.HasValue ? Utc(research.CreatedDate.Value) : null
        };

        public static CaseResearchSourceDto ToDto(SourcePoco source) => new()
        {
            Id = source.Id,
            Title = HtmlSanitiser.Sanitise(source.Title),
            Url = source.Url,
            RetrievedDate = Utc(source.RetrievedDate)
        };

        public static List<CaseResearchDto> ToDto(IEnumerable<RulePoco> research, IEnumerable<SourcePoco> sources,
            IEnumerable<CaseFileDto>? files = null)
        {
            var grouped = sources.GroupBy(w => w.CaseResearchId).ToDictionary(g => g.Key, g => g.ToList());

            var attached = (files ?? []).Where(w => w.CaseResearchId.HasValue)
                .GroupBy(w => w.CaseResearchId!.Value).ToDictionary(g => g.Key, g => g.ToList());

            return research.Select(r => ToDto(r, grouped.GetValueOrDefault(r.Id) ?? [],
                attached.GetValueOrDefault(r.Id))).ToList();
        }

        public static SourcePoco ToPoco(CaseResearchSourceDto source, DateTime now) => new()
        {
            Title = string.IsNullOrWhiteSpace(source.Title) ? null : HtmlSanitiser.Sanitise(source.Title.Trim()),
            Url = ((string?)source.Url ?? string.Empty).Trim(),
            RetrievedDate = source.RetrievedDate?.UtcDateTime ?? now
        };

        private static DateTimeOffset Utc(DateTime value) =>
            new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
