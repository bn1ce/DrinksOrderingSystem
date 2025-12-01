using MenuOrderingSystem.Models;
using MenuOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace MenuOrderingSystem.Controllers
{
    [Authorize(Roles = "Member")]
    public class CheckoutController : Controller
    {
        private readonly DB db;
        private readonly Helper hp;
        private const string CartSessionKey = "Cart";

        public CheckoutController(DB context, Helper helper)
        {
            db = context;
            hp = helper;
        }

        [HttpPost]
        public IActionResult CreateCheckoutSession()
        {
            // Retrieve cart from session
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            var cart = string.IsNullOrEmpty(cartJson)
                ? new Dictionary<string, CartItem>()
                : JsonSerializer.Deserialize<Dictionary<string, CartItem>>(cartJson);

            if (cart == null || !cart.Any())
            {
                return RedirectToAction("_ShoppingCart", "Cart");
            }

            // Stripe line items
            var lineItems = cart.Values.Select(item => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Price * 100m),
                    Currency = "myr",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{item.DrinkName} ({item.Size}, {item.SugarLevel}, {item.IceLevel})"
                    }
                },
                Quantity = item.Quantity
            }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = Url.Action("CheckoutConfirmed", "Checkout", null, Request.Scheme),
                CancelUrl = Url.Action("Cancel", "Checkout", null, Request.Scheme)
            };

            var service = new SessionService();
            var session = service.Create(options);

            // Save order in DB (pending)
            var order = new Order
            {
                MemberEmail = User.Identity!.Name,
                OrderTime = DateTime.Now,
                TotalAmount = cart.Sum(c => c.Value.Quantity * c.Value.Price),
                StripeSessionId = session.Id,
                Status = "Pending",
                OrderItems = cart.Select(c => new OrderItem
                {
                    DrinkID = c.Value.DrinkID,
                    Quantity = c.Value.Quantity,
                    Size = c.Value.Size,
                    SugarLevel = c.Value.SugarLevel,
                    IceLevel = c.Value.IceLevel
                }).ToList()
            };

            db.Orders.Add(order);
            db.SaveChanges();

            return Redirect(session.Url);
        }

        public IActionResult CheckoutConfirmed()
        {
            // Find the last pending order for this user
            var order = db.Orders
                          .Include(o => o.OrderItems)
                          .ThenInclude(oi => oi.Drink)
                          .Where(o => o.MemberEmail == User.Identity!.Name && o.Status == "Pending")
                          .OrderByDescending(o => o.OrderID)
                          .FirstOrDefault();

            if (order != null)
            {
                order.Status = "Paid";
                db.SaveChanges();

                // Clear cart session
                HttpContext.Session.Remove(CartSessionKey);

                // Send e-receipt email
                var mail = hp.CreateReceiptMail(order); // Create MailMessage with receipt
                hp.SendEmail(mail);                     // Send via SMTP

                return RedirectToAction("Receipt", "Cart", new { id = order.OrderID });
            }

            return NotFound();
        }

        public IActionResult Cancel()
        {
            return RedirectToAction("_ShoppingCart", "Cart");
        }

        [HttpPost("webhook")]
        public IActionResult StripeWebhook()
        {
            var json = new StreamReader(HttpContext.Request.Body).ReadToEnd();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    "your_webhook_secret"
                );

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    var order = db.Orders.FirstOrDefault(o => o.StripeSessionId == session.Id);
                    if (order != null)
                    {
                        order.Status = "Paid";
                        db.SaveChanges();
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"⚠️ Webhook error: {e.Message}");
                return BadRequest();
            }
        }
    }
}
