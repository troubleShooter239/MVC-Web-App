﻿using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MVCWebApp.Models;
using MVCWebApp.Models.User;
using MVCWebApp.Tools.Hashers;

namespace MVCWebApp;

public class RegisterController : Controller
{
    private readonly ILogger<RegisterController> _logger;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IHasher _hasher;

    public RegisterController(ILogger<RegisterController> logger, IMongoCollection<User> userCollection,
        IHasher hasher)
    {
        _logger = logger;
        _userCollection = userCollection;
        _hasher = hasher;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if the user has already registered
        var existingUser = await (await _userCollection.FindAsync(u => 
            u.Email == model.Email)).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "User with this email already exists");
            return View(model);
        }

        // Creating new user
        var newUser = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            PasswordHash = _hasher.HashString(model.Password),
            DeliveryAddress = model.DeliveryAddress,
            PhoneNumber = model.PhoneNumber,
            ProfilePicture = model.ProfilePicture
        };

        try
        {
            // Saving user in database
            await _userCollection.InsertOneAsync(newUser);
        }
        catch (Exception ex)
        {
            // Log exception
            _logger.LogError(ex, "Error during user registration");
            throw; // Re-throw the exception to ensure proper error handling
        }

        // Log successful registration
        _logger.LogInformation($"User registered: {model.Email}");

        // Redirect to login page
        return RedirectToAction("Login");        
    }
}
