using System.Linq;
using MenuOrderingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Climate;

namespace MenuOrderingSystem.Controllers;

[Authorize(Roles = "Member")]
public class HistoryController : Controller
{
    private readonly DB db;

    public HistoryController(DB db)
    {
        this.db = db;
    }

    // GET: /History
    public IActionResult Index()
    {
        var orders = db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Drink)
            .Where(o => o.MemberEmail == User.Identity!.Name)
            .OrderByDescending(o => o.OrderID)
            .ToList();

        return View("~/Views/Cart/History.cshtml", orders);
    }

    // GET: /Cart/OrderDetail
    public IActionResult Detail(int id)
    {
        var order = db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Drink)
            .FirstOrDefault(o => o.OrderID == id && o.MemberEmail == User.Identity!.Name);

        if (order == null) return RedirectToAction("Index");

        return View("~/Views/Cart/OrderDetails.cshtml", order);
    }


    public IActionResult Cancel(int id)
    {
        var order = db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefault(o => o.OrderID == id && o.MemberEmail == User.Identity!.Name);

        if (order == null)
        {
            return RedirectToAction("Index");
        }

        // Only allow cancel if order is still "Paid" (not already cancelled or completed)
        if (order.Status != "Paid")
        {
            return RedirectToAction("Detail", new { id = order.OrderID });
        }

        order.Status = "Cancelled";
        db.SaveChanges();

        return RedirectToAction("Index");
    }
}
