using System;
using System.Collections.Generic;
using System.IO;
using Jube.App.Code;
using Jube.Cryptography.Exceptions;
using Jube.Data.Context;
using Jube.Engine.Helpers;
using Jube.Preservation;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jube.App.Controllers.Preservation
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PreservationController : Controller
    {
        private readonly DbContext _dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;

        // ReSharper disable once NotAccessedField.Local
        private readonly ILog _log;

        // ReSharper disable once NotAccessedField.Local
        private readonly PermissionValidation _permissionValidation;
        private readonly string _userName;

        public PreservationController(ILog log,
            IHttpContextAccessor httpContextAccessor, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
                _userName = httpContextAccessor.HttpContext.User.Identity.Name;
            _log = log;

            _dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));

            _permissionValidation = new PermissionValidation(_dbContext, _userName);
            _log = log;
            _dynamicEnvironment = dynamicEnvironment;
        }

        [HttpPost("Import")]
        public IActionResult Upload(List<IFormFile> files, string password, bool exhaustive,
            bool suppressions,
            bool lists, bool dictionaries, bool visualisations)
        {
            if (!_permissionValidation.Validate(new[] { 38 })) return Forbid();

            if (files.Count <= 0) return BadRequest();

            try
            {
                var importExportOptions = new ImportExportOptions
                {
                    Password = password,
                    Exhaustive = exhaustive,
                    Suppressions = suppressions,
                    Lists = lists,
                    Dictionaries = dictionaries,
                    Visualisations = visualisations
                };

                var preservation = new Jube.Preservation.Preservation(_dbContext, _userName,
                    _dynamicEnvironment.AppSettings("PreservationSalt"));

                foreach (var file in files)
                {
                    using var stream = file.OpenReadStream();
                    using var reader = new BinaryReader(stream);
                    var bytes = reader.ReadBytes((int)stream.Length);
                    preservation.Import(bytes, importExportOptions);
                    return Ok();
                }

                return BadRequest();
            }
            catch (InvalidHmacException)
            {
                return BadRequest();
            }
            catch (InvalidDecryptionException)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                _log.Error($"Exception while exporting {ex}");
                throw;
            }
        }

        [HttpGet("ExportPeek")]
        [Produces("text/plain")]
        public ActionResult<string> Preview(bool exhaustive, bool suppressions, bool lists, bool dictionaries,
            bool visualisations)
        {
            if (!_permissionValidation.Validate(new[] { 38 })) return Forbid();

            var importExportOptions = new ImportExportOptions
            {
                Exhaustive = exhaustive,
                Suppressions = suppressions,
                Lists = lists,
                Dictionaries = dictionaries,
                Visualisations = visualisations
            };

            var preservation = new Jube.Preservation.Preservation(_dbContext, _userName);
            var payload = preservation.ExportPeek(importExportOptions);
            return payload.Yaml;
        }

        [HttpGet("Export")]
        public IActionResult Export(string password, bool exhaustive, bool suppressions, bool lists, bool dictionaries,
            bool visualisations)
        {
            if (!_permissionValidation.Validate(new[] { 38 })) return Forbid();

            try
            {
                var preservation = new Jube.Preservation.Preservation(_dbContext, _userName,
                    _dynamicEnvironment.AppSettings("PreservationSalt"));

                var importExportOptions = new ImportExportOptions
                {
                    Password = password,
                    Exhaustive = exhaustive,
                    Suppressions = suppressions,
                    Lists = lists,
                    Dictionaries = dictionaries,
                    Visualisations = visualisations
                };

                var export = preservation.Export(importExportOptions);
                return File(export.EncryptedBytes, "application/octet-stream", $"{export.Guid}.jemp");
            }
            catch (Exception ex)
            {
                _log.Error($"Error exporting {ex}");

                return StatusCode(500);
            }
        }
    }
}