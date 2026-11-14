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
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Jube.App.Wire;
using Jube.Dto.Repository.CaseFile;
using Jube.Dto.Repository.Preservation;
using MessagePack;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireBytesTests
    {
        [Fact]
        public void ABytesMemberHasABytesShapeAndTheRequestDtosAreEncodable()
        {
            var registry = new WireTypeRegistry();

            registry.ShapeOf(typeof(PreservationImportRequestDto)).Should().NotBeNull();
            registry.ShapeOf(typeof(CaseFileUploadRequestDto)).Should().NotBeNull();
            registry.ShapeOf(typeof(byte[])).Should().Be("bytes");
        }

        [Fact]
        public void APackageSurvivesAMessagePackRoundTripAsABinaryValue()
        {
            var package = RandomNumberGenerator.GetBytes(3 * 1024 * 1024);

            var packed = MessagePackSerializer.Serialize(
                new PreservationImportRequestDto { Password = "p", FileName = "a.jemp", Package = package },
                WireSerializer.Options);

            packed.Length.Should().BeLessThan(package.Length + 64, "bytes travel as a binary value, not as text");

            var unpacked = MessagePackSerializer.Deserialize<PreservationImportRequestDto>(packed,
                WireSerializer.Options);

            unpacked.Package.Should().Equal(package);
            unpacked.Password.Should().Be("p");
        }

        [Fact]
        public void APackageSurvivesAJsonRoundTripAsBase64Text()
        {
            var package = RandomNumberGenerator.GetBytes(4096);

            var json = JsonSerializer.Serialize(new CaseFileUploadRequestDto { CaseId = 7, Content = package },
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            json.Should().Contain(System.Convert.ToBase64String(package));

            JsonSerializer.Deserialize<CaseFileUploadRequestDto>(json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Content.Should().Equal(package);
        }
    }
}
