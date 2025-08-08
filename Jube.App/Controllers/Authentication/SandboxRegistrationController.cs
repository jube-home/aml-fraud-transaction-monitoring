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
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Threading.Tasks;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Engine.Helpers;
using Jube.Service.Authentication;
using Jube.Service.Dto.Authentication;
using Jube.Service.Exceptions.Authentication;
using Jube.Validations.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jube.App.Controllers.Authentication
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class SandboxRegistrationController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly SandboxRegistration _service;

        public SandboxRegistrationController(DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            _dynamicEnvironment = dynamicEnvironment;
            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(
                    _dynamicEnvironment.AppSettings("ConnectionString"));
            _service = new SandboxRegistration(_dbContext);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Close();
                _dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpPost("Register")]
        [ProducesResponseType(typeof(SandboxRegistrationResponseDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<AuthenticationResponseDto>> Register(
            [FromBody] SandboxRegistrationRequestDto model)
        {
            if (!_dynamicEnvironment.AppSettings("EnableSandbox").Equals("True")) return Unauthorized();

            var validator = new SandboxRegistrationRequestDtoValidator();
            var results = await validator.ValidateAsync(model);
            if (!results.IsValid) return BadRequest(results);

            try
            {
                var sandboxRegistrationResponseDto =
                    await _service.Register(model, _dynamicEnvironment.AppSettings("PasswordHashingKey"));
                SetAuthenticationCookie(model);
                return Ok(sandboxRegistrationResponseDto);
            }
            catch (ConflictException)
            {
                return StatusCode(409);
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        private void SetAuthenticationCookie(SandboxRegistrationRequestDto model)
        {
            var token = Jwt.CreateToken(model.UserName,
                _dynamicEnvironment.AppSettings("JWTKey"),
                _dynamicEnvironment.AppSettings("JWTValidIssuer"),
                _dynamicEnvironment.AppSettings("JWTValidAudience")
            );

            var expiration = DateTime.Now.AddMinutes(15);

            var authenticationDto = new AuthenticationResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expiration
            };

            var cookieOptions = new CookieOptions
            {
                Expires = expiration,
                HttpOnly = true
            };

            Response.Cookies.Append("authentication",
                authenticationDto.Token, cookieOptions);
        }
    }
}