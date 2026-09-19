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
using Npgsql;

namespace Jube.Service.Concurrency
{
    public static class UniqueViolation
    {
        public static bool Is(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    return true;
                }
            }

            return false;
        }

        public static ValidationResult NameDuplicateResult(string message)
        {
            return new ValidationResult([new ValidationFailure("Name", message) { ErrorCode = "NameDuplicate" }]);
        }

        public static async Task<T> GuardAsync<T>(Func<Task<T>> action, Func<ValidationResult, Exception> toException,
            string message)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (Is(ex))
            {
                throw toException(NameDuplicateResult(message));
            }
        }
    }
}