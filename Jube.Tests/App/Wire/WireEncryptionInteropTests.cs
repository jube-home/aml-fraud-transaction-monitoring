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
using System.Linq;
using System.Text;
using FluentAssertions;
using Jube.App.Wire;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireEncryptionInteropTests
    {
        private const string ThrowawayTestPrivateKey =
            "-----BEGIN PRIVATE KEY-----\n" +
            "MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQDdgOjUn9Se3dEs\n" +
            "8OUKYJLI3xLNJFouoXxAkoNhAszKQJUOiSw4orP2t7EeDVZPcEio/EJ6FkrVcjCI\n" +
            "bTUzF+YeynPpQsp0jpidoJPA6xAw3ZNQvavKsUAtV7+fIpUG2J4OYLtHmb40IkAv\n" +
            "P6yLoG7mBqa3YjBnkzOWgWFW96SnYR3KuGN+pBOOl06cbyZBXYIQHhuRwuEsHGwu\n" +
            "1AguyZn6rMfuKMGeOhTiTFj2OaALm4X+/FjuLIUeHcO0xOSRUbwsVHRgpvrSUdCt\n" +
            "oTHiN8yiogukhyiNntuONCkewvq6kOdh1nYFxk4zhREV5ey5S8vCdu5rxx2OEHZ+\n" +
            "Sr89eWGJAgMBAAECggEAFi9nIoSvownMfZfTGOr7SKsvEuTlM25xJ3aFOh4xrMAz\n" +
            "LbWHfFSvebKERuJHFsXEQUiiz9+TtzUsrz1g6rEmF0dnvKE37dyqZH9EzTm98Wb/\n" +
            "7kJRBogiSWhUhPfNvVXM3tvJZi574nTbwN+7xHBZKyjXYd6vwm/35scmnLNJBop/\n" +
            "3knMmiUz1hFtWtsgb2cTG3THM4OgseSVVxjvV+/dJfP/rty3QC8i4JL6zmHKbB1s\n" +
            "wN7MFh/KrMW081rTHYn4MRVjyfEedg28dy53dbzYmY6UdBNzouwOW1PmN5ckvMJC\n" +
            "pNEjPYIdu7J3M6PRGuLt/CrIjkd2XGpHDdKElxMiAQKBgQDz+mYIr7j/kOSwZ65r\n" +
            "BijNWaw8EUaxghUWDpT82A/UjFwZTl5FDEYwLH2OoRl25Dz5UW4UaPLOUASwqsQB\n" +
            "5hApG4X32rqAbv1O8g0bzgg61A6yGqAQYIISXJabj4Ucv8H2NebDI326CA23QSdv\n" +
            "sxYtV0x/JbEV5KjuE2w9x5HBWQKBgQDoawLWNVdsKSxSiKnToACowf5T3knKDBq1\n" +
            "7IVOYsTOGKAGB6xLtM3di51cnBVdfwx1WBV8BXbV+8qxpvNfdOzMMxDA4NZ3TNup\n" +
            "ojET4wh6UBbv+jppNHTgnkLa9Ob2AgYIw5RHc3gIehVsoA21ZEcOnp3ZKHHj8SQB\n" +
            "KNGekibrsQKBgD7RV/gglndm7RrmdygjO9oznn8Qy6zkm11Ef+PiFaFaSlRRsz//\n" +
            "+wpoSiOxIwdTmMdwyfEDXJD2VV7PeQwsGjkbVxEMBj0PVrU+6VUhZUkQVIBTDqdo\n" +
            "0dS8diKzf0h5rw0fr7zKpJKDhFSMuASwjaM6+P07zS6f40b5RUNa4RJxAoGASOqx\n" +
            "1CSeDdgJCMfJiBRtwIREV2mo+tO7WZIf7Iy+M+6z3dBwdLv/zl7r+s8ZQOEINpRx\n" +
            "ARzxwIajfALl3e/gDtyCiBDCGGGrlNfGz/dthHo2CAoYY0NK2+GrFei5wTq3S+6Y\n" +
            "SaZnZTlwGUlJo7wXMvX+jc/Lk3jnE9oLkdVGkrECgYBDGKL1RIlXnFfd96c+eP1l\n" +
            "qsJkZ4WoBu5Ii/shdkY/eC4nqJTC/wQjOceCMhohG9k/3eYANEbIZP5p0tC0u/Ft\n" +
            "pPxie65IHMdI8HBeZLgrzSDWgq7khvDo3mRWwLr7qa8QqSGMxgnGxESK1faQ297C\n" +
            "BzS5yylQyNc0WG+eIjKNVw==\n" +
            "-----END PRIVATE KEY-----\n";

        private const string ThrowawayTestPublicKey =
            "-----BEGIN PUBLIC KEY-----\n" +
            "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA3YDo1J/Unt3RLPDlCmCS\n" +
            "yN8SzSRaLqF8QJKDYQLMykCVDoksOKKz9rexHg1WT3BIqPxCehZK1XIwiG01Mxfm\n" +
            "Hspz6ULKdI6YnaCTwOsQMN2TUL2ryrFALVe/nyKVBtieDmC7R5m+NCJALz+si6Bu\n" +
            "5gamt2IwZ5MzloFhVvekp2EdyrhjfqQTjpdOnG8mQV2CEB4bkcLhLBxsLtQILsmZ\n" +
            "+qzH7ijBnjoU4kxY9jmgC5uF/vxY7iyFHh3DtMTkkVG8LFR0YKb60lHQraEx4jfM\n" +
            "oqILpIcojZ7bjjQpHsL6upDnYdZ2BcZOM4URFeXsuUvLwnbua8cdjhB2fkq/PXlh\n" +
            "iQIDAQAB\n" +
            "-----END PUBLIC KEY-----\n";

        private const string WrappedByTheBrowser =
            "WchJkHBEDplgYNyx191e4LeIKAJ5PA9e5JcbWYEfLS9mGwGaUhbmO5CIgWRs18ivU4GLOogrlBn7p0syq+hOkf1BxlhhWkZEcYLH0lmo+Ms2s7JNK+zESnbsocAWIarjyc87W5S4fnf8MQV2XS6USBPN7/fj3c9Rs/LC+IRTeSjCgnSRVYV67+wJyxH+UGz0W4AEObf8Zjai58rznmApxYQbFdS00WWhOezKwdrCg1L4Ef9c9wqH5ScHEgCCY9voH2/xvySnZyKxrNqPP+otfbn32Pa7bfFzBas9hSv0UwfLmUlWWXpmK+k1NAZNvX/2T7h/ND6WIue9Vo/3gh0hsg==";

        private const string SealedByTheBrowser =
            "a0a1a2a3a4a5a6a7a8a9aaabc2f35402df8f6d285d60c30694a71aa624b2c67c364a1d905a6b152d6ff0dc675b7bd55fb8a7b6";

        private static WireCipher Cipher()
        {
            return new WireCipher(Convert.ToBase64String(Encoding.UTF8.GetBytes(ThrowawayTestPrivateKey)),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(ThrowawayTestPublicKey)));
        }

        [Fact]
        public void AKeyWrappedByWebCryptoIsUnwrappedByTheServer()
        {
            using var cipher = Cipher();

            var key = cipher.Unwrap(Convert.FromBase64String(WrappedByTheBrowser));

            key.Should().Equal(Enumerable.Range(1, 32).Select(value => (byte)value));
        }

        [Fact]
        public void AMessageSealedByWebCryptoIsOpenedByTheServerWithTheSameContext()
        {
            using var cipher = Cipher();
            var key = cipher.Unwrap(Convert.FromBase64String(WrappedByTheBrowser));

            var plain = WireCipher.Open(key, Convert.FromHexString(SealedByTheBrowser),
                WireCipher.RequestAssociatedData("post", "/api/Rows?x=1", "1700000000000", "application/json"));

            Encoding.UTF8.GetString(plain).Should().Be("{\"id\":7,\"name\":\"seven\"}");
        }

        [Fact]
        public void TheServerPublishesTheSubjectPublicKeyInfoTheBrowserImports()
        {
            using var cipher = Cipher();

            cipher.PublicKey.Should().NotBeNullOrEmpty();
            Convert.FromBase64String(cipher.PublicKey).Should().HaveCountGreaterThan(256);
        }
    }
}
