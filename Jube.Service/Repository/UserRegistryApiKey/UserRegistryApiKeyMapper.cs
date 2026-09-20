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

using System.Diagnostics.CodeAnalysis;
using Jube.Dto.Repository.UserRegistryApiKey;
using RulePoco = Jube.Data.Poco.UserRegistryApiKey;

namespace Jube.Service.Repository.UserRegistryApiKey
{
    internal static class UserRegistryApiKeyMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static UserRegistryApiKeyDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new UserRegistryApiKeyDto
            {
                Id = rulePoco.Id,
                UserRegistryId = rulePoco.UserRegistryId.GetValueOrDefault(),
                Name = rulePoco.Name,
                Description = rulePoco.Description,
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                ApiKeyDisplay = rulePoco.ApiKeyDisplay
            };

        public static List<UserRegistryApiKeyDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(UserRegistryApiKeyDto dto) => new()
        {
            UserRegistryId = dto.UserRegistryId,
            Name = dto.Name,
            Description = dto.Description
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}