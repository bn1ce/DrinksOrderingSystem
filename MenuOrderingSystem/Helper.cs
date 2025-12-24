using MenuOrderingSystem.Models;
using MenuOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using static System.Net.Mime.MediaTypeNames;

namespace Demo;

public class Helper
{
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;
    private readonly IConfiguration cf;

    public Helper(IWebHostEnvironment en,
                  IHttpContextAccessor ct,
                  IConfiguration cf)
    {
        this.en = en;
        this.ct = ct;
        this.cf = cf;
    }

    // ------------------------------------------------------------------------
    // Photo Upload Helper Functions
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 1.5 * 1024 * 1024)
        {
            return "Photo size cannot more than 1.5MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = SixLabors.ImageSharp.Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }

    public string SaveDrinkImage(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var folderPath = Path.Combine(en.WebRootPath, folder);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var path = Path.Combine(folderPath, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = SixLabors.ImageSharp.Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return $"{file}";
    }


    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }



    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
               == PasswordVerificationResult.Success;
    }

    public void SignIn(string email, string role, bool rememberMe)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        ct.HttpContext!.SignOutAsync();
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 1; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];
        }

        return password;
    }



    // ------------------------------------------------------------------------
    // Email Helper Functions
    // ------------------------------------------------------------------------

    public void SendEmail(MailMessage mail)
    {
        string user = cf["Smtp:User"] ?? "";
        string pass = cf["Smtp:Pass"] ?? "";
        string name = cf["Smtp:Name"] ?? "";
        string host = cf["Smtp:Host"] ?? "";
        int port = cf.GetValue<int>("Smtp:Port");

        // Force the "From" address to match the authenticated user
        // (Google requires this to match your login email)
        mail.From = new MailAddress(user, name);

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true, // Critical for Gmail
            Credentials = new NetworkCredential(user, pass),
        };

        try
        {
            smtp.Send(mail);
        }
        catch (Exception ex)
        {
            // TEMPORARY: Throw the error so you can see it on the browser screen
            throw new Exception("Email Failed: " + ex.Message);
        }
    }


    public MailMessage CreateReceiptMail(Order order)
    {
        // Create PDF receipt in memory
        var ms = new MemoryStream();
        var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36);

        // Prevent the stream from being closed when the document is closed
        var writer = PdfWriter.GetInstance(doc, ms);
        writer.CloseStream = false;

        doc.Open();

        var titleFont = FontFactory.GetFont("Times New Roman", 16, iTextSharp.text.Font.BOLD);
        var normalFont = FontFactory.GetFont("Times New Roman", 12, iTextSharp.text.Font.NORMAL);

        doc.Add(new Paragraph("CHAGEE Drink Store", titleFont));
        doc.Add(new Paragraph($"Receipt for Order #{order.OrderID}", normalFont));
        doc.Add(new Paragraph($"Date: {order.OrderTime:dd MMM yyyy HH:mm}", normalFont));
        doc.Add(new Paragraph($"Customer: {order.MemberEmail}", normalFont));
        doc.Add(new Paragraph(" ")); // empty line

        PdfPTable table = new PdfPTable(5) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 40f, 10f, 10f, 20f, 20f });

        table.AddCell("Drink");
        table.AddCell("Size");
        table.AddCell("Qty");
        table.AddCell("Unit Price");
        table.AddCell("Subtotal");

        foreach (var item in order.OrderItems)
        {
            // Determine unit price based on size
            decimal unitPrice = item.Size.ToLower() == "large" ? item.Drink.PriceLarge : item.Drink.PriceRegular;

            table.AddCell(item.Drink.Name);
            table.AddCell(item.Size);
            table.AddCell(item.Quantity.ToString());
            table.AddCell(unitPrice.ToString("C"));
            table.AddCell((item.Quantity * unitPrice).ToString("C"));
        }

        PdfPCell totalCell = new PdfPCell(new Phrase("Total", normalFont))
        {
            Colspan = 4,
            HorizontalAlignment = Element.ALIGN_RIGHT,
            Border = iTextSharp.text.Rectangle.NO_BORDER
        };
        table.AddCell(totalCell);
        table.AddCell(order.TotalAmount.ToString("C"));

        doc.Add(table);

        doc.Add(new Paragraph("\nThank you for choosing CHAGEE Drink Store! We hope to serve you again soon. ☕", normalFont));

        doc.Close();

        // Reset stream position
        ms.Position = 0;

        var mail = new MailMessage();
        mail.To.Add(order.MemberEmail);
        mail.Subject = $"Your Receipt - Order #{order.OrderID}";
        mail.IsBodyHtml = true;
        mail.Body = $"Dear Customer,<br/><br/>Thank you for your order #{order.OrderID}. Please find your receipt attached.<br/><br/>From, 🐱 Super Admin";

        var attachment = new Attachment(ms, $"Receipt_{order.OrderID}.pdf", "application/pdf");
        mail.Attachments.Add(attachment);

        return mail;
    }



    // ------------------------------------------------------------------------
    // DateTime Helper Functions
    // ------------------------------------------------------------------------

    // Return January (1) to December (12)
    public SelectList GetMonthList()
    {
        var list = new List<object>();

        for (int n = 1; n <= 12; n++)
        {
            list.Add(new
            {
                Id = n,
                Name = new DateTime(1, n, 1).ToString("MMMM"),
            });
        }

        return new SelectList(list, "Id", "Name");
    }

    // Return min to max years
    public SelectList GetYearList(int min, int max, bool reverse = false)
    {
        var list = new List<int>();

        for (int n = min; n <= max; n++)
        {
            list.Add(n);
        }

        if (reverse) list.Reverse();

        return new SelectList(list);
    }



    // ------------------------------------------------------------------------
    // Shopping Cart Helper Functions
    // ------------------------------------------------------------------------

    // Update the shopping cart structure to store CartItem objects instead of just quantities
    public Dictionary<int, CartItem> GetCart()
    {
        var cart = ct.HttpContext.Session.GetString("Cart");
        return string.IsNullOrEmpty(cart)
            ? new Dictionary<int, CartItem>()
            : JsonSerializer.Deserialize<Dictionary<int, CartItem>>(cart) ?? new Dictionary<int, CartItem>();
    }

    public void SetCart(Dictionary<int, CartItem> cart = null)
    {
        if (cart == null || !cart.Any())
        {
            ct.HttpContext.Session.Remove("Cart");
        }
        else
        {
            var json = JsonSerializer.Serialize(cart);
            ct.HttpContext.Session.SetString("Cart", json);
        }
    }
}
