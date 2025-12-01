using MenuOrderingSystem.Models;
using MenuOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MenuOrderingSystem.Controllers
{
    public class DrinkController : Controller
    {
        private readonly DB _context;

        public DrinkController(DB context)
        {
            _context = context;
        }

        public IActionResult Index(int? categoryId)
        {
            var categories = _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();

            int selectedCategoryId = categoryId ?? categories.FirstOrDefault()?.CategoryID ?? 0;

            var drinks = _context.Drinks
                .Include(d => d.Category)
                .Where(d => d.IsAvailable && d.CategoryID == selectedCategoryId)
                .OrderBy(d => d.Name)
                .ToList();

            var vm = new DrinkMenuViewModel
            {
                Categories = categories,
                Drinks = drinks,
                SelectedCategoryId = selectedCategoryId
            };

            return View(vm);
        }

        // AJAX Filter (search, sort, category)
        public IActionResult Filter(int categoryId, string search = "", string sort = "")
        {
            var drinksQuery = _context.Drinks
                .Include(d => d.Category)
                .Where(d => d.IsAvailable);

            if (categoryId > 0)
                drinksQuery = drinksQuery.Where(d => d.CategoryID == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                drinksQuery = drinksQuery.Where(d => d.Name.Contains(search));

            drinksQuery = sort switch
            {
                "name-asc" => drinksQuery.OrderBy(d => d.Name),
                "name-desc" => drinksQuery.OrderByDescending(d => d.Name),
                "price-asc" => drinksQuery.OrderBy(d => d.PriceRegular),
                "price-desc" => drinksQuery.OrderByDescending(d => d.PriceRegular),
                _ => drinksQuery.OrderBy(d => d.Name)
            };

            var drinks = drinksQuery.ToList();

            return PartialView("_DrinkList", drinks);
        }

        public IActionResult Details(int id)
        {
            var drink = _context.Drinks
                .Include(d => d.Category)
                .FirstOrDefault(d => d.DrinkID == id);

            if (drink == null)
                return NotFound();

            return View(drink);
        }
    }
}
