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

using FluentValidation.Results;
using Jube.Dto.Validation;
using Jube.Parser;

namespace Jube.Service.Agent
{
    public static class ValidationResultMapper
    {
        public static ValidationResultDto ToDto(ValidationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return new ValidationResultDto
            {
                IsValid = result.IsValid,
                Errors = result.Errors.Select(ToDto).ToList()
            };
        }

        private static ValidationErrorDto ToDto(ValidationFailure failure)
        {
            var span = failure.CustomState as ErrorSpan;

            return new ValidationErrorDto
            {
                PropertyName = failure.PropertyName ?? string.Empty,
                ErrorCode = failure.ErrorCode ?? string.Empty,
                Message = span?.Message ?? failure.ErrorMessage ?? string.Empty,
                Line = span?.Line,
                Start = span?.Start,
                Length = span?.Length
            };
        }
    }
}