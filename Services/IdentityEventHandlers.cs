// Services/IdentityEventHandlers.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Matchletic.Services
{
    public static class IdentityEventHandlers
    {
        public static void ConfigureIdentityEvents(this IServiceCollection services)
        {
            // Dodavanje event handlera za uspješnu registraciju
            services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            });

            // Event handler za kreiranje korisnika
            services.AddScoped<IUserConfirmation<IdentityUser>, DefaultUserConfirmation<IdentityUser>>();
            services.AddTransient<IUserStore<IdentityUser>, UserStore<IdentityUser, IdentityRole, ApplicationDbContext>>();
        }
    }

    // Event handler za uspješnu registraciju
    public class UserRegistrationEventHandler
    {
        private readonly UserSyncService _userSyncService;

        public UserRegistrationEventHandler(UserSyncService userSyncService)
        {
            _userSyncService = userSyncService;
        }

        public async Task HandleUserRegisteredAsync(IdentityUser user)
        {
            Console.WriteLine($"User registration event triggered for: {user.Email}");
            await _userSyncService.SyncIdentityUser(user.Email);
        }
    }
}
