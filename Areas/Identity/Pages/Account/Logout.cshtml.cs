// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Matchletic.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        // Areas/Identity/Pages/Account/Logout.cshtml.cs
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // Očisti sesiju
            HttpContext.Session.Clear();

            // Odjavi korisnika
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Korisnik se odjavio.");

            // Preusmjeri na login stranicu umjesto na home screen
            return LocalRedirect(Url.Content("~/Identity/Account/Login"));
        }

    }
}
