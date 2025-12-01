using MenuOrderingSystem.Models;
using MenuOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuOrderingSystem.Controllers;

public class CartController : Controller
{
    private readonly DB db;
    private readonly Helper hp;

    public CartController(DB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }


    // POST: Product/UpdateCart
    [HttpPost]
    public IActionResult UpdateCart(int drinkId, string size, string iceLevel, string sugarLevel, int quantity)
    {
        var cart = hp.GetCart();

        var drink = db.Drinks.Find(drinkId);
        if (drink == null) return Redirect(Request.Headers["Referer"].ToString());

        // pick price based on size
        decimal price = 0;
        if (size == "Regular")
            price = drink.PriceRegular;
        else if (size == "Large")
            price = drink.PriceLarge;

        if (quantity >= 1 && quantity <= 10)
        {
            cart[drinkId] = new CartItem
            {
                DrinkID = drinkId,
                DrinkName = drink.Name,
                Size = size,
                IceLevel = iceLevel,
                SugarLevel = sugarLevel,
                Quantity = quantity,
                Price = price, // ✅ correct price by size
                Subtotal = quantity * price // ✅ correct subtotal
            };

            TempData["CartMessage"] = $"{drink.Name} ({size}) added to cart!";

        }
        else
        {
            cart.Remove(drinkId);
        }

        hp.SetCart(cart);

        return Redirect(Request.Headers["Referer"].ToString());
    }

    // GET: Product/Index
    public IActionResult Index()
    {
        ViewBag.Cart = hp.GetCart();
        var drinks = db.Drinks.Include(d => d.Category).Where(d => d.IsAvailable).ToList();

        return View(drinks);
    }

    // GET: Product/ShoppingCart
    public IActionResult ShoppingCart()
    {
        var cart = hp.GetCart();
        var items = cart.Values.ToList();

        foreach (var item in items)
        {
            var drink = db.Drinks.Find(item.DrinkID);
            if (drink != null)
            {
                item.Drink = drink;
                item.DrinkName = drink.Name;
                if (item.Size == "Large")
                {
                    item.Price = drink.PriceLarge;
                }
                else
                {
                    item.Price = drink.PriceRegular;
                }
                item.Subtotal = item.Quantity * item.Price;
            }
        }

        if (Request.IsAjax())
        {
            return PartialView("_ShoppingCart", items);
        }

        return View(items);
    }


    [HttpGet]
    [Authorize(Roles = "Member")]
    public IActionResult Checkout()
    {
        var cart = hp.GetCart();
        if (!cart.Any()) return RedirectToAction("ShoppingCart");

        var vm = new CheckoutViewModel
        {
            Items = cart.Values.ToList(),
            TotalAmount = cart.Sum(c =>
            {
                var drink = db.Drinks.Find(c.Key);
                if (drink == null) return 0;

                // Choose price based on size
                decimal unitPrice = c.Value.Size.ToLower() == "large" ? drink.PriceLarge : drink.PriceRegular;
                return c.Value.Quantity * unitPrice;
            })
        };

        return View(vm); // Views/Cart/Checkout.cshtml
    }


    public IActionResult Receipt(int id)
    {
        var order = db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Drink)
            .FirstOrDefault(o => o.OrderID == id);

        if (order == null) return RedirectToAction("Order");

        return View("~/Views/Cart/Receipt.cshtml", order);
    }


    public IActionResult OrderComplete(int id)
    {
        ViewBag.Id = id;
        return View();
    }

    // GET: Product/Order
    [Authorize(Roles = "Member")]
    public IActionResult Order()
    {
        var orders = db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Drink)
            .Where(o => o.MemberEmail == User.Identity!.Name) 
            .OrderByDescending(o => o.OrderID)
            .ToList();

        return View(orders);
    }

    // GET: Product/OrderDetail
    [Authorize(Roles = "Member")]
    public IActionResult OrderDetail(int id)
    {
        var order = db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Drink)
            .FirstOrDefault(o => o.OrderID == id && o.MemberEmail == User.Identity!.Name);

        if (order == null) return RedirectToAction("Order");

        return View(order);
    }

}