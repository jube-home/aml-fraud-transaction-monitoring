using Jube.App.Code;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Jube.App.Pages.Administration
{
    [Authorize]
    public class EntityAnalysisModelProcessingCounter : PageModel
    {
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public EntityAnalysisModelProcessingCounter(ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            permissionValidation =
                new PermissionValidation(dynamicEnvironment.AppSettings("ConnectionString"), userName, log);
        }

        public ActionResult OnGet()
        {
            if (!permissionValidation.Landlord)
            {
                return Forbid();
            }

            return new PageResult();
        }
    }
}