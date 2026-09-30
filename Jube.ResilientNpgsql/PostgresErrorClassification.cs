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

namespace Jube.ResilientNpgsqlConnection
{
    using System;
    using Npgsql;

    public static class PostgresErrorClassification
    {
        public const string ReadOnlyTransaction = "25006";

        private const string ConnectionExceptionClass = "08";

        private const string InsufficientResourcesClass = "53";

        public static bool IsRetryable(PostgresException exception)
        {
            return exception.SqlState.StartsWith(ConnectionExceptionClass)
                   || exception.SqlState.StartsWith(InsufficientResourcesClass)
                   || exception.SqlState is "57P01" or "57P02" or "57P03" or "40001" or "40P01" or "55P03"
                   || exception.SqlState == ReadOnlyTransaction;
        }

        public static bool IsConnectionAcquisitionTimeout(Exception exception)
        {
            return exception is NpgsqlException and not PostgresException
                   && exception.InnerException is TimeoutException;
        }

        public static bool WarrantsPoolClear(Exception exception)
        {
            if (IsConnectionAcquisitionTimeout(exception))
            {
                return false;
            }

            if (exception is not PostgresException postgresException)
            {
                return true;
            }

            return !postgresException.SqlState.StartsWith(InsufficientResourcesClass)
                   && postgresException.SqlState is not "55P03" and not "40001" and not "40P01";
        }

        public static bool WarrantsConnectionRecycle(Exception exception)
        {
            if (IsConnectionAcquisitionTimeout(exception))
            {
                return false;
            }

            if (exception is not PostgresException postgresException)
            {
                return true;
            }

            return postgresException.SqlState == ReadOnlyTransaction
                   || postgresException.SqlState.StartsWith(ConnectionExceptionClass)
                   || postgresException.SqlState is "57P01" or "57P02" or "57P03";
        }
    }
}