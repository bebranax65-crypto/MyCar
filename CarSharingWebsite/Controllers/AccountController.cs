using CarSharingWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace CarSharingWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(string login, string password)
        {
            if (int.TryParse(password, out int pwdInt))
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == login && u.password == pwdInt);

                if (user != null)
                {
                    if (user.Role == "Blocked")
                    {
                        ViewBag.Error = "Ваш аккаунт заблокирован. Обратитесь к администратору.";
                        return View();
                    }

                    HttpContext.Session.SetInt32("UserId", user.ID_User);
                    HttpContext.Session.SetString("UserLogin", user.Login ?? "");
                    HttpContext.Session.SetString("UserRole", user.Role ?? "");
                    HttpContext.Session.SetString("UserFullName", (user.first_name + " " + user.last_name).Trim());
                    HttpContext.Session.SetString("UserIsVerified", user.IsVerified ? "true" : "false");

                    if (user.Role == "Admin")
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            
            ViewBag.Error = "Неверный логин или пароль";
            return View();
        }

        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user, string FullName, string PhoneNumber, string Email)
        {
            if (_context.Users.Any(u => u.Login == user.Login))
            {
                ModelState.AddModelError("Login", "Пользователь с таким логином уже существует");
                return View(user);
            }

            user.registration_date = DateTime.Now;
            if (string.IsNullOrEmpty(user.Role)) user.Role = "Client";
            user.IsVerified = false;
            
            user.first_name = FullName;
            user.phone = PhoneNumber;
            user.Email = Email;

            _context.Users.Add(user);
            _context.SaveChanges();

            var client = new Client 
            { 
                ID_User = user.ID_User, 
                Balance = 0,
                license_number = null
            };
            _context.Clients.Add(client);
            _context.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.ID_User);
            HttpContext.Session.SetString("UserLogin", user.Login ?? "");
            HttpContext.Session.SetString("UserRole", user.Role ?? "");
            HttpContext.Session.SetString("UserFullName", (user.first_name + " " + user.last_name).Trim());
            HttpContext.Session.SetString("UserIsVerified", "false");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> UploadDocuments(IFormFile passport, IFormFile license)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "img", "docs");
            if (!System.IO.Directory.Exists(uploadsFolder))
            {
                System.IO.Directory.CreateDirectory(uploadsFolder);
            }

            if (passport != null && passport.Length > 0)
            {
                var pPath = System.IO.Path.Combine(uploadsFolder, $"passport_{userId.Value}{System.IO.Path.GetExtension(passport.FileName)}");
                using (var stream = new System.IO.FileStream(pPath, System.IO.FileMode.Create))
                {
                    await passport.CopyToAsync(stream);
                }
            }

            if (license != null && license.Length > 0)
            {
                var lPath = System.IO.Path.Combine(uploadsFolder, $"license_{userId.Value}{System.IO.Path.GetExtension(license.FileName)}");
                using (var stream = new System.IO.FileStream(lPath, System.IO.FileMode.Create))
                {
                    await license.CopyToAsync(stream);
                }
            }

            var client = _context.Clients.FirstOrDefault(c => c.ID_User == userId.Value);

            TempData["Success"] = "Документы успешно отправлены на проверку администратору.";
            return RedirectToAction("Profile");
        }

        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId.Value);
            var clientInfo = _context.Clients.FirstOrDefault(c => c.ID_User == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            ViewBag.Balance = clientInfo?.Balance ?? 0;
            ViewBag.ClientInfo = clientInfo;
            ViewBag.HasCompletedBookings = _context.Bookings.Any(b => b.ID_User == userId.Value && b.Status == "Завершено") || _context.Rentals.Any(r => r.ID_User == userId.Value && r.status == "Завершено");
            return View(user);
        }

        public IActionResult DeleteBooking(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var booking = _context.Bookings.FirstOrDefault(b => b.ID_Booking == id && b.ID_User == userId.Value);
            if (booking != null)
            {
                var car = _context.Cars.Find(booking.ID_Car);
                if (car != null)
                {
                    car.status = "Свободен";
                }

                _context.Bookings.Remove(booking);
                _context.SaveChanges();
                TempData["Success"] = "Бронирование успешно удалено.";
            }

            return RedirectToAction("MyBookings");
        }

        public IActionResult MyBookings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var bookings = _context.Bookings
                .Include(b => b.Car)
                .ThenInclude(c => c.Tariffs)
                .Where(b => b.ID_User == userId)
                .OrderByDescending(b => b.start_datetime)
                .ToList();

            return View(bookings);
        }

        public IActionResult ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (user.password.ToString() != currentPassword)
            {
                ViewBag.Error = "Неверный текущий пароль";
                return View();
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ViewBag.Error = "Новый пароль не может быть пустым";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Новый пароль и подтверждение не совпадают";
                return View();
            }

            if (int.TryParse(newPassword, out int npwd))
            {
                user.password = npwd;
                _context.SaveChanges();
                ViewBag.Success = "Пароль успешно изменен";
            }
            else
            {
                ViewBag.Error = "Пароль должен состоять только из цифр (согласно схеме)";
            }

            return View();
        }

        public IActionResult AddFunds()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var client = _context.Clients.FirstOrDefault(c => c.ID_User == userId.Value);
            ViewBag.CurrentBalance = client?.Balance ?? 0;
            return View();
        }

        [HttpPost]
        public IActionResult AddFunds(string amount, string paymentMethod, string promoCode)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var client = _context.Clients.FirstOrDefault(c => c.ID_User == userId.Value);
            
            string amountStr = amount?.Replace(",", ".") ?? "0";
            if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedAmount) || parsedAmount <= 0)
            {
                ViewBag.Error = "Сумма должна быть больше 0";
                ViewBag.CurrentBalance = client?.Balance ?? 0;
                return View();
            }

            if (client == null)
            {
                client = new Client { ID_User = userId.Value, Balance = 0 };
                _context.Clients.Add(client);
            }

            client.Balance = (client.Balance ?? 0) + parsedAmount;
            
            if (!string.IsNullOrEmpty(promoCode) && promoCode.ToUpper().Trim() == "HELLO")
            {
                client.Balance += 10m;
                ViewBag.Success = $"Баланс успешно пополнен на {parsedAmount} BYN. Применен промокод: +10 BYN в подарок!";
            }
            else if (!string.IsNullOrEmpty(promoCode))
            {
                ViewBag.Error = "Введен неверный промокод, но основная сумма зачислена.";
                ViewBag.Success = null;
            }
            else
            {
                ViewBag.Success = $"Баланс успешно пополнен на {parsedAmount} BYN";
            }

            _context.SaveChanges();

            ViewBag.CurrentBalance = client?.Balance ?? 0;
            return View();
        }
    }
}