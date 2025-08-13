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

using FluentMigrator.Runner;
using FluentValidation;
using Jube.Blazor.Components;
using Jube.Blazor.Components.Code;
using Jube.DynamicEnvironment;
using Jube.Migrations.Baseline;
using Jube.Validations.Authentication;
using log4net;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

var log = LogManager.GetLogger(typeof(ILog));
builder.Services.AddSingleton(log);

var dynamicEnvironment = new DynamicEnvironment(log);
builder.Services.AddSingleton(dynamicEnvironment);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddRadzenComponents();

builder.Services.AddLocalization();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("HasCounterPermission", p => p.RequireClaim("Permission", "1"));

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

builder.Services
    .AddValidatorsFromAssemblyContaining<AuthenticationRequestDtoValidator>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHttpContextAccessor();

if (dynamicEnvironment.AppSettings("EnableMigration").Equals("True", StringComparison.OrdinalIgnoreCase))
{
    RunFluentMigrator(dynamicEnvironment);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US")
    .AddSupportedUICultures("en-US"));

app.Run();
return;

void RunFluentMigrator(DynamicEnvironment dynamicEnvironment)
{
#pragma warning disable ASP0000
    var serviceCollection = new ServiceCollection().AddFluentMigratorCore()
        .AddSingleton(dynamicEnvironment)
        .ConfigureRunner(rb => rb
            .AddPostgres11_0()
            .WithGlobalConnectionString(dynamicEnvironment.AppSettings("ConnectionString"))
            .ScanIn(typeof(AddActivationWatcherTableIndex).Assembly).For.Migrations())
        .BuildServiceProvider(false);
#pragma warning restore ASP0000

    using var scope = serviceCollection.CreateScope();
    var runner = serviceCollection.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}