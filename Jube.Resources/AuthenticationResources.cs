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

namespace Jube.Resources
{
    public sealed class AuthenticationResources
    {
        public const string PasswordMinLength = nameof(PasswordMinLength);
        public const string PasswordMaxLength = nameof(PasswordMaxLength);
        public const string PasswordUppercase = nameof(PasswordUppercase);
        public const string PasswordLowercase = nameof(PasswordLowercase);
        public const string PasswordDigit = nameof(PasswordDigit);
        public const string PasswordSpecial = nameof(PasswordSpecial);
        public const string PasswordRepeated = nameof(PasswordRepeated);
        public const string PasswordCommonPattern = nameof(PasswordCommonPattern);
        public const string MissingNewPassword = nameof(MissingNewPassword);
        public const string AuthorizedUserNameNull = nameof(AuthorizedUserNameNull);
        public const string UserNameRequired = nameof(UserNameRequired);
        public const string PasswordRequired = nameof(PasswordRequired);
        public const string MfaRequired = nameof(MfaRequired);
        public const string NewPasswordRequired = nameof(NewPasswordRequired);
    }
}