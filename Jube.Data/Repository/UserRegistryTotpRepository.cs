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

namespace Jube.Data.Repository
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB.Data;
    using LinqToDB.Async;

    public class UserRegistryTotpRepository(DbContext dbContext)
    {
        private const string Active = "(\"Deleted\" IS NULL OR \"Deleted\" = 0)";

        public async Task<UserRegistryTotpState> GetAsync(string userName, CancellationToken token = default)
        {
            var rows = await dbContext.QueryToListAsync<UserRegistryTotpState>(
                $"SELECT \"TotpSecret\" AS \"ProtectedSecret\", \"TotpEnrolledDate\" AS \"EnrolledDate\", \"TotpLastAcceptedStep\" AS \"LastAcceptedStep\" FROM \"UserRegistry\" WHERE \"Name\" = @user AND {Active}",
                token, new DataParameter("user", userName)).ConfigureAwait(false);

            return rows.FirstOrDefault();
        }

        public Task<int> SetPendingAsync(string userName, string protectedSecret, CancellationToken token = default)
        {
            return dbContext.ExecuteAsync(
                $"UPDATE \"UserRegistry\" SET \"TotpSecret\" = @secret, \"TotpEnrolledDate\" = NULL, \"TotpLastAcceptedStep\" = NULL WHERE \"Name\" = @user AND \"TotpEnrolledDate\" IS NULL AND {Active}",
                token, new DataParameter("secret", protectedSecret), new DataParameter("user", userName));
        }

        public Task<int> AcceptStepAsync(string userName, long step, CancellationToken token = default)
        {
            return dbContext.ExecuteAsync(
                $"UPDATE \"UserRegistry\" SET \"TotpLastAcceptedStep\" = @step, \"TotpEnrolledDate\" = COALESCE(\"TotpEnrolledDate\", @now) WHERE \"Name\" = @user AND \"TotpSecret\" IS NOT NULL AND (\"TotpLastAcceptedStep\" IS NULL OR \"TotpLastAcceptedStep\" < @step) AND {Active}",
                token, new DataParameter("step", step), new DataParameter("now", DateTime.UtcNow),
                new DataParameter("user", userName));
        }

        public Task<int> ResetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.ExecuteAsync(
                $"UPDATE \"UserRegistry\" SET \"TotpSecret\" = NULL, \"TotpEnrolledDate\" = NULL, \"TotpLastAcceptedStep\" = NULL WHERE \"Id\" = @id AND {Active}",
                token, new DataParameter("id", id));
        }
    }

    public class UserRegistryTotpState
    {
        public string ProtectedSecret { get; set; }
        public DateTime? EnrolledDate { get; set; }
        public long? LastAcceptedStep { get; set; }
    }
}
