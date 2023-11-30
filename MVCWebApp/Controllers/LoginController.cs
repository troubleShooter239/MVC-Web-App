﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MVCWebApp.Models;
using MVCWebApp.Models.User;
using MVCWebApp.Tools.Hashers;

namespace MVCWebApp;

public class LoginController : Controller
{
    private readonly ILogger<LoginController> _logger;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IHasher _hasher;

    public LoginController(ILogger<LoginController> logger, IMongoCollection<User> userCollection,
        IHasher hasher)
    {
        _logger = logger;
        _userCollection = userCollection;
        _hasher = hasher;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if user already logged in
        var user = _userCollection.Find(u => 
            u.Email == model.Email && 
            u.PasswordHash == _hasher.HashString(model.Password)
        ).FirstOrDefault();

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
        }
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            // Add another claims
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe
        };

        try
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                          new ClaimsPrincipal(claimsIdentity),
                                          authProperties);
        }
        catch(Exception ex)
        {
            // Log exception
            _logger.LogError(ex, "Error during user sign in");
            throw; // Re-throw the exception to ensure proper error handling
        }

        // Log successful login
        _logger.LogInformation($"User logged in: {model.Email}");

        // Redirect to the main page
        return RedirectToAction("Index", "Home");
    }
}
