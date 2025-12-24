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

    [HttpPost]
    public IActionResult RemoveFromCart(int DrinkID)
    {
        // 1. Clear State to prevent UI glitches
        ModelState.Clear();

        // 2. Get current cart
        var cart = hp.GetCart();

        // 3. Remove the item
        if (cart.ContainsKey(DrinkID))
        {
            cart.Remove(DrinkID);
            hp.SetCart(cart); 
        }

        // 4. Re-fetch and Hydrate for display
        var items = cart.Values.ToList();

        foreach (var item in items)
        {
            var drink = db.Drinks.Find(item.DrinkID);
            if (drink != null)
            {
                item.Drink = drink;
                item.DrinkName = drink.Name;
                item.Price = item.Size == "Large" ? drink.PriceLarge : drink.PriceRegular;
                item.Subtotal = item.Quantity * item.Price;
            }
        }

        return PartialView("_ShoppingCart", items);
    }

    // POST: Product/UpdateCart
    [HttpPost]
    public IActionResult UpdateCart(int drinkId, string size, string iceLevel, string sugarLevel, int quantity)
    {
        ModelState.Clear();

        var cart = hp.GetCart();

        var drink = db.Drinks.Find(drinkId);
        if (drink == null) return Redirect(Request.Headers["Referer"].ToString());

        // 1. Calculate Price based on Size
        decimal price = 0;
        if (size == "Regular")
            price = drink.PriceRegular;
        else if (size == "Large")
            price = drink.PriceLarge;

        // 2. Update or Remove Logic
        if (quantity >= 1 && quantity <= 10)
        {
            // Update/Add the item
            cart[drinkId] = new CartItem
            {
                DrinkID = drinkId,
                DrinkName = drink.Name,
                Size = size,
                IceLevel = iceLevel,
                SugarLevel = sugarLevel,
                Quantity = quantity,
                Price = price,
                Subtotal = quantity * price
            };

            // Only set the notification if it's NOT an AJAX update (to avoid spamming toasts)
            if (!Request.IsAjax())
            {
                TempData["CartMessage"] = $"{drink.Name} ({size}) updated!";
            }
        }
        else
        {
            // Remove if quantity is invalid or 0
            cart.Remove(drinkId);
        }

        // 3. Save to Session
        hp.SetCart(cart);

        // 4. Check for AJAX (The Fix)
        if (Request.IsAjax())
        {
            // Re-fetch the list to display in the partial view
            var items = cart.Values.ToList();

            // Re-populate Drink details (Images, Names) for the UI
            foreach (var item in items)
            {
                var d = db.Drinks.Find(item.DrinkID);
                if (d != null)
                {
                    item.Drink = d; // Necessary for ImageURL in the View
                    item.DrinkName = d.Name;

                    // Ensure price consistency for display
                    item.Price = item.Size == "Large" ? d.PriceLarge : d.PriceRegular;
                    item.Subtotal = item.Quantity * item.Price;
                }
            }

            // Return the Partial View (Updates the table only, no page reload)
            return PartialView("_ShoppingCart", items);
        }

        // Fallback for non-JS browsers
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

        // Re-populate drink details (Images, Names) for the Checkout view
        foreach (var item in cart.Values)
        {
            var drink = db.Drinks.Find(item.DrinkID);
            if (drink != null)
            {
                item.Drink = drink; // This loads the ImageURL
                                    // Ensure price consistency
                item.Price = item.Size == "Large" ? drink.PriceLarge : drink.PriceRegular;
                item.Subtotal = item.Quantity * item.Price;
            }
        }

        var vm = new CheckoutViewModel
        {
            Items = cart.Values.ToList(),
            TotalAmount = cart.Sum(c => c.Value.Subtotal)
        };

        return View(vm);
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