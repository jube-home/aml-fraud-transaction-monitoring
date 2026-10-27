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
using Jube.Data.Repository.Exceptions;
using Npgsql;

namespace Jube.Data.Reporting
{
    public static class PostgresErrorDescriber
    {
        public static SqlValidationFailedException ToException(Exception e)
        {
            if (e is InvalidOperationException)
            {
                return e.Message.Contains("is not permitted in this query")
                    ? new SqlValidationFailedException(e.Message, e)
                    : SqlValidationFailedException.Coded(DatasourceMessages.SqlSelectOnly, e);
            }

            for (var inner = e; inner != null; inner = inner.InnerException)
            {
                if (inner is not PostgresException postgresException)
                {
                    continue;
                }

                var described = string.IsNullOrWhiteSpace(postgresException.Hint)
                    ? SqlValidationFailedException.Coded(DatasourceMessages.SqlRejected, e,
                        postgresException.MessageText, postgresException.SqlState)
                    : SqlValidationFailedException.Coded(DatasourceMessages.SqlRejectedHint, e,
                        postgresException.MessageText, postgresException.SqlState, postgresException.Hint);

                return e.Data[PostgresErrorPosition.DataKey] is int position
                    ? new SqlValidationFailedException(described.Message, e)
                    {
                        Code = described.Code, Arguments = described.Arguments, Position = position
                    }
                    : described;
            }

            return SqlValidationFailedException.Coded(DatasourceMessages.SqlRejectedGeneric, e);
        }
    }
}
