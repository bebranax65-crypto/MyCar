using CarSharingWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace CarSharingWebsite.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin";
        }

        public IActionResult Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.ActiveRentals = _context.Bookings.Count(b => b.Status == "Активна");
            ViewBag.AvailableCars = _context.Cars.Count(c => c.status != "Занят");
            ViewBag.TodayRevenue = 0; // Rentals / Payments logic needed here later
            ViewBag.PendingVerifications = _context.Users.Count(u => !u.IsVerified && u.Role != "Admin");

            var latestUsers = _context.Users.OrderByDescending(u => u.registration_date).Take(5).ToList();
            var latestBookings = _context.Bookings.Include(b => b.Car).Include(b => b.User).OrderByDescending(b => b.start_datetime).Take(5).ToList();
            
            var activities = new System.Collections.Generic.List<Tuple<string, DateTime, string>>();
            foreach(var u in latestUsers) {
                if (u.registration_date != null) {
                    activities.Add(new Tuple<string, DateTime, string>("User", u.registration_date.Value, $"Новый пользователь - {u.first_name} {u.last_name}"));
                }
            }
            foreach(var b in latestBookings) {
                if (b.start_datetime != null) {
                    var brand = b.Car?.Brand ?? "Авто";
                    var model = b.Car?.Model ?? "";
                    activities.Add(new Tuple<string, DateTime, string>("Booking", b.start_datetime.Value, $"Забронирован - {brand} {model}"));
                }
            }
            ViewBag.RecentActivities = activities.OrderByDescending(a => a.Item2).Take(5).ToList();

            var popularCars = _context.Bookings
                .GroupBy(b => b.ID_Car)
                .Select(g => new { CarId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(3)
                .ToList();
                
            var carIds = popularCars.Select(c => c.CarId).ToList();
            var carsInfo = _context.Cars.Where(c => carIds.Contains(c.ID_Car)).ToList();
            
            int maxCount = popularCars.Any() ? popularCars.First().Count : 1;
            
            ViewBag.PopularCars = popularCars.Select(pc => new Tuple<string, int, int>(
                carsInfo.FirstOrDefault(c => c.ID_Car == pc.CarId)?.Brand + " " + carsInfo.FirstOrDefault(c => c.ID_Car == pc.CarId)?.Model,
                pc.Count,
                (int)((double)pc.Count / maxCount * 100)
            )).ToList();

            return View();
        }

        public IActionResult Users()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var users = _context.Users.ToList();
            ViewBag.Balances = _context.Clients.ToDictionary(c => c.ID_User, c => c.Balance ?? 0m);
            return View(users);
        }

        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.Find(id);
            if (user != null)
            {
                var activeBookings = _context.Bookings.Any(b => b.ID_User == id && b.Status == "Активна");
                if (activeBookings)
                {
                    TempData["Error"] = "Нельзя удалить пользователя с активными бронированиями";
                    return RedirectToAction("Users");
                }

                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "Пользователь удален";
            }

            return RedirectToAction("Users");
        }

        public IActionResult ManageCars()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var cars = _context.Cars.Include(c => c.Tariffs).ToList();
            return View(cars);
        }

        public IActionResult AddCar()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> AddCar(Car car, IFormFile ImageFile, string PricePerMinute, string PricePerHour, int MinDuration)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(ImageFile.FileName);
                var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "img", "cars");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }
                var filePath = System.IO.Path.Combine(uploadsFolder, fileName);

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                car.PhotoUrl = "/img/cars/" + fileName;
            }
            else
            {
                car.PhotoUrl = "/img/default_car.jpg";
            }

            car.ID_Car = 0; // Prevent IDENTITY_INSERT error
            var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
            car.ID_User = userId;
            car.status = "Свободен";

            car.Latitude = 53.9045; // Default Minsk
            car.Longitude = 27.5615;

            if (!string.IsNullOrWhiteSpace(car.Address))
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "CarSharingWebsite/1.0");
                        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(car.Address)}&format=json&limit=1";
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json))
                            {
                                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                                {
                                    var first = doc.RootElement[0];
                                    if (first.TryGetProperty("lat", out var latProp) && first.TryGetProperty("lon", out var lonProp))
                                    {
                                        if (double.TryParse(latProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                                            double.TryParse(lonProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                                        {
                                            car.Latitude = lat;
                                            car.Longitude = lon;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore exceptions and use default coordinates
                }
            }

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            string ppmStr = PricePerMinute?.Replace(",", ".") ?? "0";
            string pphStr = PricePerHour?.Replace(",", ".") ?? "0";
            decimal.TryParse(ppmStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedPpm);
            decimal.TryParse(pphStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedPph);

            var tariff = new Tariff
            {
                Name = "Базовый",
                price_per_minute = parsedPpm,
                price_per_hour = parsedPph,
                min_duration = MinDuration,
                ID_Car = car.ID_Car
            };

            _context.Tariffs.Add(tariff);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Автомобиль добавлен";
            return RedirectToAction("ManageCars");
        }

        public IActionResult EditCar(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var car = _context.Cars.Include(c => c.Tariffs).FirstOrDefault(c => c.ID_Car == id);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("ManageCars");
            }

            var tariff = car.Tariffs?.FirstOrDefault();
            ViewBag.PricePerMinute = tariff?.price_per_minute?.ToString("F2") ?? "0.00";
            ViewBag.PricePerHour = tariff?.price_per_hour?.ToString("F2") ?? "0.00";
            ViewBag.MinDuration = tariff?.min_duration ?? 60;

            return View(car);
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> EditCar(int ID_Car, Car car, IFormFile ImageFile, string PricePerMinute, string PricePerHour, int MinDuration)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var existingCar = _context.Cars.Include(c => c.Tariffs).FirstOrDefault(c => c.ID_Car == ID_Car);
            if (existingCar == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("ManageCars");
            }

            existingCar.Brand = car.Brand;
            existingCar.Model = car.Model;
            existingCar.LicensePlate = car.LicensePlate;
            existingCar.plate_number = car.plate_number;
            existingCar.condition = car.condition;
            existingCar.Color = car.Color;
            existingCar.Year = car.Year;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(ImageFile.FileName);
                var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "img", "cars");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }
                var filePath = System.IO.Path.Combine(uploadsFolder, fileName);

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                existingCar.PhotoUrl = "/img/cars/" + fileName;
            }

            if (!string.IsNullOrWhiteSpace(car.Address) && car.Address != existingCar.Address)
            {
                existingCar.Address = car.Address;
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "CarSharingWebsite/1.0");
                        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(car.Address)}&format=json&limit=1";
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json))
                            {
                                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                                {
                                    var first = doc.RootElement[0];
                                    if (first.TryGetProperty("lat", out var latProp) && first.TryGetProperty("lon", out var lonProp))
                                    {
                                        if (double.TryParse(latProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                                            double.TryParse(lonProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                                        {
                                            existingCar.Latitude = lat;
                                            existingCar.Longitude = lon;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore geocoding errors
                }
            }

            // Update tariff
            string ppmStr = PricePerMinute?.Replace(",", ".") ?? "0";
            string pphStr = PricePerHour?.Replace(",", ".") ?? "0";
            decimal.TryParse(ppmStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedPpm);
            decimal.TryParse(pphStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedPph);

            var existingTariff = existingCar.Tariffs?.FirstOrDefault();
            if (existingTariff != null)
            {
                existingTariff.price_per_minute = parsedPpm;
                existingTariff.price_per_hour = parsedPph;
                existingTariff.min_duration = MinDuration;
            }
            else
            {
                var tariff = new Tariff
                {
                    Name = "Базовый",
                    price_per_minute = parsedPpm,
                    price_per_hour = parsedPph,
                    min_duration = MinDuration,
                    ID_Car = existingCar.ID_Car
                };
                _context.Tariffs.Add(tariff);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Автомобиль обновлён";
            return RedirectToAction("ManageCars");
        }

        public IActionResult DeleteCar(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var car = _context.Cars.Find(id);
            if (car != null)
            {
                var activeBookings = _context.Bookings.Any(b => b.ID_Car == id && b.Status == "Активна");
                if (activeBookings)
                {
                    TempData["Error"] = "Нельзя удалить автомобиль с активными бронированиями";
                    return RedirectToAction("ManageCars");
                }

                _context.Cars.Remove(car);
                _context.SaveChanges();
                TempData["Success"] = "Автомобиль удален";
            }

            return RedirectToAction("ManageCars");
        }

        public IActionResult Bookings()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.Bookings
                .Include(b => b.Car)
                .ThenInclude(c => c.Tariffs)
                .Include(b => b.User)
                .OrderByDescending(b => b.start_datetime)
                .ToList();
            return View(bookings);
        }

        public IActionResult Statistics()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var completedBookings = _context.Bookings
                .Include(b => b.Car)
                .ThenInclude(c => c.Tariffs)
                .Where(b => b.Status == "Завершена")
                .ToList();

            var totalTrips = completedBookings.Count;
            decimal totalRevenue = 0;
            var userStats = new System.Collections.Generic.Dictionary<int, (decimal TotalSpent, decimal TotalHours, int Trips)>();

            foreach (var b in completedBookings)
            {
                if (b.start_datetime.HasValue && b.end_datetime.HasValue && b.Car?.Tariffs?.FirstOrDefault() != null)
                {
                    var minutes = (decimal)(b.end_datetime.Value - b.start_datetime.Value).TotalMinutes;
                    var hours = (decimal)(b.end_datetime.Value - b.start_datetime.Value).TotalHours;
                    var price = b.Car.Tariffs.First().price_per_minute ?? 0m;
                    var cost = Math.Max(0m, minutes) * price;
                    totalRevenue += cost;

                    if (!userStats.ContainsKey(b.ID_User))
                    {
                        userStats[b.ID_User] = (0m, 0m, 0);
                    }
                    var current = userStats[b.ID_User];
                    userStats[b.ID_User] = (current.TotalSpent + cost, current.TotalHours + hours, current.Trips + 1);
                }
            }

            ViewBag.TotalTrips = totalTrips;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AverageCheck = totalTrips > 0 ? (totalRevenue / totalTrips) : 0m;

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            ViewBag.NewUsers = _context.Users.Count(u => u.registration_date >= thirtyDaysAgo);

            var topUserIds = userStats.OrderByDescending(kvp => kvp.Value.TotalSpent).Take(5).Select(kvp => kvp.Key).ToList();
            var topUsersInfo = _context.Users.Where(u => topUserIds.Contains(u.ID_User)).ToList();
            
            var topUsersDetailed = topUserIds.Select(id => new Tuple<CarSharingWebsite.Models.User, int, decimal, decimal>(
                topUsersInfo.FirstOrDefault(u => u.ID_User == id),
                userStats[id].Trips,
                userStats[id].TotalSpent,
                userStats[id].TotalHours
            )).ToList();

            ViewBag.TopUsers = topUsersDetailed;

            return View();
        }

        [HttpPost]
        public IActionResult BlockUser(int id)
        {
            if (!IsAdmin() || id == HttpContext.Session.GetInt32("UserId"))
            {
                return Json(new { success = false });
            }

            var user = _context.Users.Find(id);
            if (user != null)
            {
                // To simulate blocking since Status is gone, we can change Role
                user.Role = "Blocked";
                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UnblockUser(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false });
            }

            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.Role = "Client";
                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult VerifyUser(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false });
            }

            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsVerified = true;
                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult RejectUser(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false });
            }

            var user = _context.Users.Find(id);
            if (user != null && user.Role != "Admin")
            {
                var client = _context.Clients.FirstOrDefault(c => c.ID_User == id);
                if (client != null)
                {
                    _context.Clients.Remove(client);
                }
                _context.Users.Remove(user);
                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        public IActionResult DeleteBooking(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = _context.Bookings.Find(id);
            if (booking != null)
            {
                var car = _context.Cars.Find(booking.ID_Car);
                if (car != null)
                {
                    car.status = "Свободен";
                }

                _context.Bookings.Remove(booking);
                _context.SaveChanges();
                TempData["Success"] = "Бронирование удалено";
            }

            return RedirectToAction("Bookings");
        }
    }
}