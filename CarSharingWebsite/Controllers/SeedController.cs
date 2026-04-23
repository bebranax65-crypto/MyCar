using CarSharingWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CarSharingWebsite.Controllers
{
    public class SeedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeedController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return Content("Visit /Seed/Populate to seed the database with cars and users.");
        }

        public IActionResult Populate()
        {
            try
            {
                // 1. Seed Users
                var existingLogins = _context.Users.Select(u => u.Login).ToHashSet();
                
                var usersToSeed = new List<User>
                {
                    new User { Login = "ivanov", password = 1234, first_name = "Иван Иванов", Email = "ivan@example.com", Role = "Client", IsVerified = true, registration_date = DateTime.Now.AddDays(-10) },
                    new User { Login = "petrov", password = 1234, first_name = "Петр Петров", Email = "petr@example.com", Role = "Client", IsVerified = true, registration_date = DateTime.Now.AddDays(-8) },
                    new User { Login = "sidorov", password = 1234, first_name = "Сидор Сидоров", Email = "sidor@example.com", Role = "Client", IsVerified = false, registration_date = DateTime.Now.AddDays(-2) },
                    new User { Login = "kuznecov", password = 1234, first_name = "Алексей Кузнецов", Email = "alex@example.com", Role = "Client", IsVerified = true, registration_date = DateTime.Now.AddDays(-15) },
                    new User { Login = "smirnov", password = 1234, first_name = "Дмитрий Смирнов", Email = "dima@example.com", Role = "Client", IsVerified = false, registration_date = DateTime.Now.AddDays(-1) },
                    new User { Login = "popov", password = 1234, first_name = "Олег Попов", Email = "oleg@example.com", Role = "Client", IsVerified = true, registration_date = DateTime.Now.AddDays(-20) },
                    new User { Login = "volkov", password = 1234, first_name = "Сергей Волков", Email = "serg@example.com", Role = "Blocked", IsVerified = true, registration_date = DateTime.Now.AddDays(-30) },
                    new User { Login = "morozov", password = 1234, first_name = "Андрей Морозов", Email = "andrey@example.com", Role = "Client", IsVerified = true, registration_date = DateTime.Now.AddDays(-5) },
                    new User { Login = "novikov", password = 1234, first_name = "Николай Новиков", Email = "nick@example.com", Role = "Client", IsVerified = false, registration_date = DateTime.Now.AddDays(-3) },
                    new User { Login = "fedorov", password = 1234, first_name = "Федор Федоров", Email = "fedor@example.com", Role = "Client", IsVerified = true, registration_date = DateTime.Now.AddDays(-12) }
                };

                foreach (var user in usersToSeed)
                {
                    if (!existingLogins.Contains(user.Login))
                    {
                        _context.Users.Add(user);
                    }
                }
                _context.SaveChanges();

                // 2. Seed Clients for New Users
                var usersWithoutClients = _context.Users
                    .Where(u => !_context.Clients.Any(c => c.ID_User == u.ID_User))
                    .ToList();

                foreach (var u in usersWithoutClients)
                {
                    _context.Clients.Add(new Client 
                    { 
                        ID_User = u.ID_User, 
                        Balance = new Random().Next(50, 500),
                        license_number = "AB" + new Random().Next(100000, 999999)
                    });
                }
                _context.SaveChanges();

                // 3. Seed Cars
                var adminUser = _context.Users.FirstOrDefault(u => u.Role == "Admin");
                int adminId = adminUser?.ID_User ?? 1;

                var carsData = new List<(string Brand, string Model, decimal Price)>
                {
                    ("BMW", "X5", 0.85m), ("BMW", "M5", 1.20m), ("BMW", "320i", 0.65m),
                    ("Mercedes-Benz", "E-Class", 0.90m), ("Mercedes-Benz", "S-Class", 1.50m), ("Mercedes-Benz", "CLA", 0.70m),
                    ("Audi", "A6", 0.80m), ("Audi", "Q7", 1.00m), ("Audi", "RS6", 1.80m),
                    ("Tesla", "Model S", 1.10m), ("Tesla", "Model 3", 0.75m),
                    ("Porsche", "911", 2.50m), ("Porsche", "Taycan", 2.20m),
                    ("Toyota", "Camry", 0.45m), ("Toyota", "RAV4", 0.50m),
                    ("Mazda", "6", 0.40m), ("Mazda", "CX-5", 0.48m),
                    ("Volkswagen", "Golf", 0.35m), ("Volkswagen", "Passat", 0.42m), ("Volkswagen", "Tiguan", 0.55m),
                    ("Lexus", "RX", 0.95m), ("Lexus", "IS", 0.70m),
                    ("Hyundai", "Sonata", 0.38m), ("Kia", "K5", 0.38m), ("Skoda", "Octavia", 0.32m)
                };

                var rand = new Random();
                var colors = new[] { "Черный", "Белый", "Серый", "Синий", "Красный", "Серебристый" };
                
                foreach (var carData in carsData)
                {
                    var car = new Car
                    {
                        Brand = carData.Brand,
                        Model = carData.Model,
                        Year = rand.Next(2020, 2024),
                        Color = colors[rand.Next(colors.Length)],
                        LicensePlate = $"{rand.Next(1000, 9999)} " + (char)rand.Next(65, 90) + (char)rand.Next(65, 90) + "-" + rand.Next(1, 7),
                        status = "Свободен",
                        ID_User = adminId,
                        Latitude = 53.85 + rand.NextDouble() * 0.1,
                        Longitude = 27.50 + rand.NextDouble() * 0.15,
                        PhotoUrl = $"/images/cars/{carData.Brand.ToLower()}_{carData.Model.ToLower().Replace(" ", "_")}.jpg"
                    };

                    _context.Cars.Add(car);
                    _context.SaveChanges(); // Save to get ID_Car

                    var tariff = new Tariff
                    {
                        Name = "Стандарт",
                        price_per_minute = carData.Price,
                        price_per_hour = carData.Price * 50, // Discount for hour
                        ID_Car = car.ID_Car
                    };
                    _context.Tariffs.Add(tariff);
                }
                _context.SaveChanges();

                return Content($"Successfully seeded {usersToSeed.Count} users and {carsData.Count} cars.");
            }
            catch (Exception ex)
            {
                return Content($"Error during seeding: {ex.Message}");
            }
        }
    }
}
