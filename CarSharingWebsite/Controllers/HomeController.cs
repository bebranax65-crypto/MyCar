using CarSharingWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CarSharingWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Home/Index
        public IActionResult Index()
        {
            var cars = _context.Cars
                .Include(c => c.Tariffs)
                .Where(c => c.status != "Занят")
                .Take(3)
                .ToList();
            return View(cars);
        }

        // GET: /Home/Cars
        public IActionResult Cars()
        {
            var cars = _context.Cars.Include(c => c.Tariffs).ToList();
            return View(cars);
        }

        // GET: /Home/CarDetails/{id}
        public IActionResult CarDetails(int id)
        {
            var car = _context.Cars.Include(c => c.Tariffs).FirstOrDefault(c => c.ID_Car == id);
            if (car == null)
            {
                return NotFound();
            }
            return View(car);
        }

        [HttpPost]
        public IActionResult CreateBooking(int CarId, DateTime StartTime, DateTime EndTime)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = _context.Users.Find(userId.Value);
            if (currentUser != null && !currentUser.IsVerified && currentUser.Role != "Admin")
            {
                TempData["Error"] = "Ваш аккаунт ещё не верифицирован администратором. Бронирование недоступно.";
                return RedirectToAction("CarDetails", new { id = CarId });
            }

            if (StartTime >= EndTime)
            {
                TempData["Error"] = "Время окончания должно быть позже времени начала";
                return RedirectToAction("CarDetails", new { id = CarId });
            }

            if (StartTime < DateTime.Now)
            {
                TempData["Error"] = "Время начала не может быть в прошлом";
                return RedirectToAction("CarDetails", new { id = CarId });
            }

            var car = _context.Cars.Include(c => c.Tariffs).FirstOrDefault(c => c.ID_Car == CarId);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }

            var conflictingBookings = _context.Bookings
                .Where(b => b.ID_Car == CarId)
                .Where(b => b.Status == "Активна" || b.Status == "активна")
                .Where(b => (StartTime < b.end_datetime && EndTime > b.start_datetime))
                .ToList();

            if (conflictingBookings.Any())
            {
                TempData["Error"] = $"Это время уже занято. Найдено {conflictingBookings.Count} бронирований.";
                return RedirectToAction("CarDetails", new { id = CarId });
            }

            var client = _context.Clients.FirstOrDefault(c => c.ID_User == userId.Value);
            if (client == null)
            {
                TempData["Error"] = "Профиль клиента не найден. Обратитесь в поддержку.";
                return RedirectToAction("CarDetails", new { id = CarId });
            }

            decimal totalCost = 0;
            var tariff = car.Tariffs?.FirstOrDefault();
            if (tariff != null && tariff.price_per_minute.HasValue)
            {
                var duration = EndTime - StartTime;
                decimal minutes = (decimal)Math.Ceiling(duration.TotalMinutes);
                totalCost = minutes * tariff.price_per_minute.Value;
            }

            if (client.Balance == null || client.Balance < totalCost)
            {
                TempData["Error"] = $"Недостаточно средств. Ваш баланс: {client.Balance ?? 0:F2} BYN, стоимость аренды: {totalCost:F2} BYN.";
                return RedirectToAction("CarDetails", new { id = CarId });
            }

            client.Balance -= totalCost;

            var booking = new Booking
            {
                ID_User = userId.Value,
                ID_Car = CarId,
                start_datetime = StartTime,
                end_datetime = EndTime,
                Status = "Активна"
            };

            car.status = "Занят";

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            TempData["Success"] = $"Автомобиль {car.Brand} {car.Model} успешно забронирован!";
            return RedirectToAction("MyBookings", "Account");
        }

        // GET: /Home/Map
        public IActionResult Map()
        {
            var cars = _context.Cars.Include(c => c.Tariffs).Where(c => c.status != "Занят").ToList();
            var rand = new Random();
            bool needSave = false;

            foreach (var car in cars)
            {
                if (car.Latitude == null || car.Longitude == null)
                {
                    car.Latitude = 53.85 + rand.NextDouble() * 0.1;
                    car.Longitude = 27.50 + rand.NextDouble() * 0.15;
                    needSave = true;
                }
            }

            if (needSave)
            {
                _context.SaveChanges();
            }

            return View(cars);
        }

        // GET: /Home/HowItWorks
        public IActionResult HowItWorks()
        {
            return View();
        }

        // GET: /Home/Tariffs
        public IActionResult Tariffs()
        {
            return View();
        }

        // GET: /Home/CheckAvailability
        [HttpGet]
        public JsonResult CheckAvailability(int carId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var conflictingBookings = _context.Bookings
                    .Where(b => b.ID_Car == carId)
                    .Where(b => b.Status == "Активна" || b.Status == "активна")
                    .Where(b => (startTime < b.end_datetime && endTime > b.start_datetime))
                    .ToList();

                return Json(new
                {
                    available = !conflictingBookings.Any(),
                    count = conflictingBookings.Count,
                    message = conflictingBookings.Any() ? $"Найдено {conflictingBookings.Count} бронирований" : "Свободно"
                });
            }
            catch (Exception ex)
            {
                return Json(new { available = false, error = ex.Message });
            }
        }

        // GET: /Home/Reviews
        public IActionResult Reviews()
        {
            var reviews = _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.review_date)
                .ToList();
            return View(reviews);
        }

        [HttpPost]
        public IActionResult AddReview(int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Оценка должна быть от 1 до 5";
                return RedirectToAction("Reviews");
            }

            var review = new Review
            {
                ID_User = userId.Value,
                rating = rating,
                comment = string.IsNullOrEmpty(comment) ? "Без комментария" : comment,
                review_date = DateTime.Now
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            TempData["Success"] = "Ваш отзыв успешно добавлен!";
            return RedirectToAction("Reviews");
        }
    }
}