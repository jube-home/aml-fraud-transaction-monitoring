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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

#pragma warning disable 1998

namespace Jube.App.Code
{
    public class UserStore : IUserEmailStore<ApplicationUser>, IUserPhoneNumberStore<ApplicationUser>,
        IUserTwoFactorStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>
    {
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                user.Id = await connection.QuerySingleAsync<int>($@"INSERT INTO [ApplicationUser] ([UserName], [NormalizedUserName], [Email],
                    [NormalizedEmail], [EmailConfirmed], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled])
                    VALUES (@{nameof(ApplicationUser.UserName)}, @{nameof(ApplicationUser.NormalizedUserName)}, @{nameof(ApplicationUser.Email)},
                    @{nameof(ApplicationUser.NormalizedEmail)}, @{nameof(ApplicationUser.EmailConfirmed)}, @{nameof(ApplicationUser.PasswordHash)},
                    @{nameof(ApplicationUser.PhoneNumber)}, @{nameof(ApplicationUser.PhoneNumberConfirmed)}, @{nameof(ApplicationUser.TwoFactorEnabled)});
                    SELECT CAST(SCOPE_IDENTITY() as int)", user);
            }*/

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                await connection.ExecuteAsync($"DELETE FROM [ApplicationUser] WHERE [Id] = @{nameof(ApplicationUser.Id)}", user);
            }*/

            return IdentityResult.Success;
        }

        public async Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                return await connection.QuerySingleOrDefaultAsync<ApplicationUser>($@"SELECT * FROM [ApplicationUser]
                    WHERE [Id] = @{nameof(userId)}", new { userId });
            }*/
            return null;
        }

        public async Task<ApplicationUser> FindByNameAsync(string normalizedUserName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                return await connection.QuerySingleOrDefaultAsync<ApplicationUser>($@"SELECT * FROM [ApplicationUser]
                    WHERE [NormalizedUserName] = @{nameof(normalizedUserName)}", new { normalizedUserName });
            }*/
            return null;
        }

        public Task<string> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string normalizedName,
            CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(ApplicationUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.FromResult(0);
        }

        public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                await connection.ExecuteAsync($@"UPDATE [ApplicationUser] SET
                    [UserName] = @{nameof(ApplicationUser.UserName)},
                    [NormalizedUserName] = @{nameof(ApplicationUser.NormalizedUserName)},
                    [Email] = @{nameof(ApplicationUser.Email)},
                    [NormalizedEmail] = @{nameof(ApplicationUser.NormalizedEmail)},
                    [EmailConfirmed] = @{nameof(ApplicationUser.EmailConfirmed)},
                    [PasswordHash] = @{nameof(ApplicationUser.PasswordHash)},
                    [PhoneNumber] = @{nameof(ApplicationUser.PhoneNumber)},
                    [PhoneNumberConfirmed] = @{nameof(ApplicationUser.PhoneNumberConfirmed)},
                    [TwoFactorEnabled] = @{nameof(ApplicationUser.TwoFactorEnabled)}
                    WHERE [Id] = @{nameof(ApplicationUser.Id)}", user);
            }*/

            return IdentityResult.Success;
        }

        public Task SetEmailAsync(ApplicationUser user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public async Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                return await connection.QuerySingleOrDefaultAsync<ApplicationUser>($@"SELECT * FROM [ApplicationUser]
                    WHERE [NormalizedEmail] = @{nameof(normalizedEmail)}", new { normalizedEmail });
            }*/
            return null;
        }

        public Task<string> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(ApplicationUser user, string normalizedEmail,
            CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            // Nothing to dispose.
        }

        public Task SetPasswordHashAsync(ApplicationUser user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash != null);
        }

        public Task SetPhoneNumberAsync(ApplicationUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberConfirmedAsync(ApplicationUser user, bool confirmed,
            CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public Task SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.TwoFactorEnabled);
        }
    }
}