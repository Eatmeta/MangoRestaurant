﻿using System.Security.Claims;
using IdentityModel;
using Mango.Services.Identity.DbContexts;
using Mango.Services.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Mango.Services.Identity.Initializer;

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DbInitializer(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public void Initialize()
    {
        if (_roleManager.FindByNameAsync(Sd.Admin).Result == null)
        {
            _roleManager.CreateAsync(new IdentityRole(Sd.Admin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Sd.Customer)).GetAwaiter().GetResult();
        }
        else
        {
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = "admin1@gmail.com",
            Email = "admin1@gmail.com",
            EmailConfirmed = true,
            PhoneNumber = "111111111111",
            FirstName = "Ben",
            LastName = "Admin"
        };

        _userManager.CreateAsync(adminUser, "Admin123*").GetAwaiter().GetResult();
        _userManager.AddToRoleAsync(adminUser, Sd.Admin).GetAwaiter().GetResult();

        var temp1 = _userManager.AddClaimsAsync(adminUser, new Claim[]
        {
            new Claim(JwtClaimTypes.Name, adminUser.FirstName + " " + adminUser.LastName),
            new Claim(JwtClaimTypes.GivenName, adminUser.FirstName),
            new Claim(JwtClaimTypes.FamilyName, adminUser.LastName),
            new Claim(JwtClaimTypes.Role, Sd.Admin),
        }).Result;

        var customerUser = new ApplicationUser
        {
            UserName = "customer1@gmail.com",
            Email = "customer1@gmail.com",
            EmailConfirmed = true,
            PhoneNumber = "111111111111",
            FirstName = "Ben",
            LastName = "Cust"
        };

        _userManager.CreateAsync(customerUser, "Admin123*").GetAwaiter().GetResult();
        _userManager.AddToRoleAsync(customerUser, Sd.Customer).GetAwaiter().GetResult();

        var temp2 = _userManager.AddClaimsAsync(customerUser, new Claim[]
        {
            new Claim(JwtClaimTypes.Name, customerUser.FirstName + " " + customerUser.LastName),
            new Claim(JwtClaimTypes.GivenName, customerUser.FirstName),
            new Claim(JwtClaimTypes.FamilyName, customerUser.LastName),
            new Claim(JwtClaimTypes.Role, Sd.Customer),
        }).Result;
    }
}