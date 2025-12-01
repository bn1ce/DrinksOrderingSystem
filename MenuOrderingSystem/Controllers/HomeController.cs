using Microsoft.AspNetCore.Mvc;
using MenuOrderingSystem.Models; 

namespace MenuOrderingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly DB _context;

        public HomeController(DB context)  
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var drinks = _context.Drinks
                .Where(d => d.IsAvailable)
                .Take(4)
                .ToList();

            return View(drinks);
        }
    }
}
