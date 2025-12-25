using MenuOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Text.Json;

namespace MenuOrderingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly DB db;
        private readonly Helper hp;

        public AdminController(DB context, Helper helper)
        {
            db = context;
            hp = helper;
        }

        // Admin Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // -------------------------------
        // Sales Reports
        // -------------------------------
        public IActionResult Report()
        {
            // Sales by Day (grouped)
            var salesByDay = db.Orders
                .Where(o => o.Status == "Paid" || o.Status == "Completed") // Include Completed orders in sales
                .GroupBy(o => new { o.OrderTime.Year, o.OrderTime.Month, o.OrderTime.Day })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .AsEnumerable()
                .Select(g => new
                {
                    Period = $"{g.Year}-{g.Month:D2}-{g.Day:D2}",
                    g.TotalSales
                })
                .OrderBy(g => g.Period)
                .ToList();

            // Top Drinks
            var topDrinks = db.OrderItems
                .Include(oi => oi.Drink)
                .GroupBy(oi => oi.Drink.Name)
                .Select(g => new
                {
                    Drink = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(5)
                .ToList();

            // Database total
            var dbTotal = db.Orders.Where(o => o.Status == "Paid" || o.Status == "Completed").Sum(o => o.TotalAmount);

            // Stripe total (optional)
            // var stripeService = new BalanceService(); ... (Your existing Stripe logic)
            // For now defaulting to 0 to prevent crashes if Stripe key isn't set
            var stripeTotal = 0m;

            ViewBag.SalesLabels = JsonSerializer.Serialize(salesByDay.Select(x => x.Period));
            ViewBag.SalesData = JsonSerializer.Serialize(salesByDay.Select(x => x.TotalSales));
            ViewBag.DrinkLabels = JsonSerializer.Serialize(topDrinks.Select(x => x.Drink));
            ViewBag.DrinkData = JsonSerializer.Serialize(topDrinks.Select(x => x.TotalSold));
            ViewBag.DbTotal = dbTotal;
            ViewBag.StripeTotal = stripeTotal;

            return View("SalesReport");
        }

        // -------------------------------
        // Orders Management
        // -------------------------------
        public IActionResult Orders()
        {
            return View();
        }

        public IActionResult OrderDetail(int id)
        {
            var order = db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Drink)
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null) return RedirectToAction("Orders");

            return View(order);
        }

        public IActionResult LoadOrders(string search = "", string sort = "id_desc", int page = 1, int pageSize = 10, string status = "")
        {
            var query = db.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.MemberEmail.Contains(search) ||
                    o.OrderID.ToString().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Sorting
            query = sort switch
            {
                "id" => query.OrderBy(o => o.OrderID),
                "id_desc" => query.OrderByDescending(o => o.OrderID),
                "time" => query.OrderBy(o => o.OrderTime),
                "time_desc" => query.OrderByDescending(o => o.OrderTime),
                "total" => query.OrderBy(o => o.TotalAmount),
                "total_desc" => query.OrderByDescending(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderID)
            };

            // Paging
            int totalItems = query.Count();
            var orders = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new PagedResult<Order>
            {
                Items = orders,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };

            return PartialView("_Orders", vm);
        }


        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            // Update the status
            order.Status = status;
            db.SaveChanges();

            // Return success JSON for the AJAX call
            return Json(new { success = true });
        }

        // -------------------------------
        // Drinks Management
        // -------------------------------
        public IActionResult Drinks()
        {
            return View();
        }

        public IActionResult LoadDrinks(string search = "", string sort = "name", int page = 1, int pageSize = 10)
        {
            var query = db.Drinks.Include(d => d.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.Name.Contains(search) || d.Category.CategoryName.Contains(search));

            query = sort switch
            {
                "name" => query.OrderBy(d => d.Name),
                "name_desc" => query.OrderByDescending(d => d.Name),
                "price" => query.OrderBy(d => d.PriceRegular),
                "price_desc" => query.OrderByDescending(d => d.PriceRegular),
                _ => query.OrderBy(d => d.DrinkID)
            };

            int totalItems = query.Count();
            var drinks = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new PagedResult<Drink>
            {
                Items = drinks,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };

            return PartialView("_Drinks", vm);
        }

        // GET: Admin/AddDrink
        public IActionResult AddDrink()
        {
            return View();
        }

        // POST: Admin/AddDrink
        [HttpPost]
        public IActionResult AddDrink(DrinkVM vm)
        {
            if (vm.Image != null)
            {
                var err = hp.ValidatePhoto(vm.Image);
                if (err != "") ModelState.AddModelError("Image", err);
            }

            if (ModelState.IsValid)
            {
                var drink = new Drink
                {
                    Name = vm.Name,
                    Description = vm.Description,
                    PriceRegular = vm.PriceRegular,
                    PriceLarge = vm.PriceLarge,
                    IsAvailable = vm.IsAvailable,
                    CategoryID = vm.CategoryID,
                    ImageURL = vm.Image != null
                        ? hp.SaveDrinkImage(vm.Image, "drinks")
                        : "noimage.png"
                };

                db.Drinks.Add(drink);
                db.SaveChanges();

                TempData["Info"] = "Drink added successfully.";
                return RedirectToAction("Drinks");
            }
            return View(vm);
        }

        // GET: Admin/EditDrink/1
        public IActionResult EditDrink(int id)
        {
            var drink = db.Drinks.Find(id);
            if (drink == null) return RedirectToAction("Drinks");

            var vm = new DrinkVM
            {
                DrinkID = drink.DrinkID,
                Name = drink.Name,
                Description = drink.Description,
                PriceRegular = drink.PriceRegular,
                PriceLarge = drink.PriceLarge,
                IsAvailable = drink.IsAvailable,
                CategoryID = drink.CategoryID,
                ImageURL = drink.ImageURL
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult EditDrink(DrinkVM vm)
        {
            var drink = db.Drinks.Find(vm.DrinkID);
            if (drink == null) return RedirectToAction("Drinks");

            if (vm.Image != null)
            {
                var err = hp.ValidatePhoto(vm.Image);
                if (err != "") ModelState.AddModelError("Image", err);
            }

            if (ModelState.IsValid)
            {
                drink.Name = vm.Name;
                drink.Description = vm.Description;
                drink.PriceRegular = vm.PriceRegular;
                drink.PriceLarge = vm.PriceLarge;
                drink.IsAvailable = vm.IsAvailable;
                drink.CategoryID = vm.CategoryID;

                if (vm.Image != null)
                {
                    if (!string.IsNullOrEmpty(drink.ImageURL) && drink.ImageURL != "noimage.png")
                    {
                        hp.DeletePhoto(drink.ImageURL, "drinks");
                    }
                    drink.ImageURL = hp.SavePhoto(vm.Image, "drinks");
                }

                db.SaveChanges();

                TempData["Info"] = "Drink updated successfully.";
                return RedirectToAction("Drinks");
            }

            vm.ImageURL = drink.ImageURL;
            return View(vm);
        }

        [HttpPost]
        public IActionResult DeleteDrink(int id)
        {
            var drink = db.Drinks.Find(id);
            if (drink != null)
            {
                if (!string.IsNullOrEmpty(drink.ImageURL) && drink.ImageURL != "noimage.png")
                {
                    hp.DeletePhoto(drink.ImageURL, "drinks");
                }

                db.Drinks.Remove(drink);
                db.SaveChanges();

                TempData["Info"] = "Drink deleted successfully.";
            }

            return RedirectToAction("Drinks");
        }

        // GET: Admin/Members
        public IActionResult Members()
        {
            var members = db.Members.ToList();
            return View(members);
        }

        public IActionResult MemberDetail(string email)
        {
            var member = db.Members.FirstOrDefault(m => m.Email == email);
            if (member == null) return RedirectToAction("Members");

            return View(member);
        }
    }
}