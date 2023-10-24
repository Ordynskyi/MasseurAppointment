﻿using System.Text.Json;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, ILogger? logger)
    {
        if (await userManager.Users.AnyAsync()) return;

        var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

        if (userData == null) 
        {
            throw new ArgumentException("Could not get the users data");
        }

        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };

        var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

        if (users == null) 
        {
            throw new ArgumentException("Could not get users from the users data");            
        }

        var roles = new List<AppRole>()
        {
            new AppRole{Name = "Member"},
            new AppRole{Name = "Admin"},
            new AppRole{Name = "Moderator"}
        };

        foreach (var role in roles)
        {
            await roleManager.CreateAsync(role);
        }

        IdentityResult result;
        foreach (var user in users)
        {
            user.UserName = user.UserName?.ToLower() ?? string.Empty;
            
            result = await userManager.CreateAsync(user, "password");
            if (CheckAndLogError(result, logger)) continue;

            result = await userManager.AddToRoleAsync(user, "Member");
            CheckAndLogError(result, logger);
        }

        var admin = new AppUser
        {
            UserName = "admin",
        };

        result = await userManager.CreateAsync(admin, "password");
        if (CheckAndLogError(result, logger)) return;

        result = await userManager.AddToRolesAsync(admin, new [] {"Admin", "Moderator"});
        CheckAndLogError(result, logger);
    }

    private static bool CheckAndLogError(IdentityResult result, ILogger? logger)
    {
        if (result.Succeeded) return false;
        if (logger == null) return true;

        foreach (var error in result.Errors) 
        {
            logger.LogError(error.Description);
        }

        return true;
    }
}
