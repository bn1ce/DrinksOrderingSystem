using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOrderingSystem.Models;
using MenuOrderingSystem.Models.ViewModels;

namespace MenuOrderingSystem.Controllers
{
    [Authorize(Roles = "Member")]  // <-- only members can access
    public class FeedbackController : Controller
    {
        private readonly DB db;
        private readonly Helper hp;

        public FeedbackController(DB db, Helper hp)
        {
            this.db = db;
            this.hp = hp;
        }

        public IActionResult Index()
        {
            ViewBag.Feedbacks = db.Feedback.OrderByDescending(f => f.FeedbackTime).ToList();
            return View(new FeedbackVM());
        }

        [HttpPost]
        public IActionResult Create(FeedbackVM vm)
        {
            if (ModelState.IsValid)
            {
                var member = db.Set<Member>().Find(User.Identity!.Name);
                if (member == null) return RedirectToAction("Index", "Home");

                db.Feedback.Add(new Feedback
                {
                    MemberEmail = member.Email,
                    Name = member.Name,
                    Rating = vm.Rating,
                    Comment = vm.Comment,
                    FeedbackTime = DateTime.Now
                });
                db.SaveChanges();

                TempData["Info"] = "Feedback submitted successfully.";
                return RedirectToAction("Index");
            }

            ViewBag.Feedbacks = db.Feedback.OrderByDescending(f => f.FeedbackTime).ToList();
            return View(vm);
        }
    }
}
