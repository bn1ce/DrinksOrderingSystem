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
        // 1. Setup Document
        var ms = new MemoryStream();
        var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36);

        var writer = PdfWriter.GetInstance(doc, ms);
        writer.CloseStream = false;

        doc.Open();

        // 2. Define Brand Colors & Fonts
        var chageeGreen = new BaseColor(45, 90, 39);   // #2d5a27
        var chageeGold = new BaseColor(197, 160, 89);  // #c5a059
        var lightGray = new BaseColor(245, 245, 245);  // #f5f5f5
        var darkText = new BaseColor(30, 30, 30);

        // Use FontFactory to avoid constructor errors
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, chageeGreen);
        var subTitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, chageeGold);
        var tableHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, darkText);
        var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, darkText);

        // 3. HEADER SECTION (Logo + Title)
        PdfPTable headerTable = new PdfPTable(2);
        headerTable.WidthPercentage = 100;
        headerTable.SetWidths(new float[] { 1f, 2f });

        // -- Logo Cell --
        PdfPCell logoCell = new PdfPCell();
        logoCell.Border = iTextSharp.text.Rectangle.NO_BORDER;
        logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;

        // Load logo
        string logoPath = Path.Combine(en.WebRootPath, "images", "logo.png");
        if (File.Exists(logoPath))
        {
            var logo = iTextSharp.text.Image.GetInstance(logoPath);
            logo.ScaleToFit(80f, 80f);
            logoCell.AddElement(logo);
        }
        headerTable.AddCell(logoCell);

        // -- Company Info Cell --
        PdfPCell infoCell = new PdfPCell();
        infoCell.Border = iTextSharp.text.Rectangle.NO_BORDER;
        infoCell.HorizontalAlignment = Element.ALIGN_RIGHT;
        infoCell.AddElement(new Paragraph("CHAGEE OFFICIAL RECEIPT", titleFont) { Alignment = Element.ALIGN_RIGHT });
        infoCell.AddElement(new Paragraph("Premium Tea • Fresh Milk", subTitleFont) { Alignment = Element.ALIGN_RIGHT });
        infoCell.AddElement(new Paragraph($"Order ID: #{order.OrderID}", boldFont) { Alignment = Element.ALIGN_RIGHT });
        infoCell.AddElement(new Paragraph($"{order.OrderTime:dd MMM yyyy, hh:mm tt}", bodyFont) { Alignment = Element.ALIGN_RIGHT });
        headerTable.AddCell(infoCell);

        doc.Add(headerTable);

        doc.Add(new Paragraph(" "));
        doc.Add(new Paragraph(" "));

        // 4. CUSTOMER INFO
        doc.Add(new Paragraph($"Customer Email: {order.MemberEmail}", bodyFont));
        doc.Add(new Paragraph("Payment Status: PAID", boldFont));
        doc.Add(new Paragraph(" ", bodyFont));

        // 5. ITEMS TABLE
        PdfPTable table = new PdfPTable(5) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 40f, 15f, 10f, 15f, 20f });

        string[] headers = { "Item Description", "Size", "Qty", "Unit Price", "Subtotal" };
        foreach (var h in headers)
        {
            PdfPCell cell = new PdfPCell(new Phrase(h, tableHeaderFont));
            cell.BackgroundColor = chageeGreen;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 8f;
            cell.BorderColor = chageeGreen;
            table.AddCell(cell);
        }

        bool alternate = false;
        foreach (var item in order.OrderItems)
        {
            decimal unitPrice = item.Size.ToLower() == "large" ? item.Drink.PriceLarge : item.Drink.PriceRegular;
            BaseColor rowColor = alternate ? lightGray : BaseColor.WHITE;

            void AddCell(string text, int align)
            {
                PdfPCell c = new PdfPCell(new Phrase(text, bodyFont));
                c.BackgroundColor = rowColor;
                c.HorizontalAlignment = align;
                c.Padding = 6f;
                c.BorderColor = new BaseColor(230, 230, 230);
                table.AddCell(c);
            }

            AddCell(item.Drink.Name, Element.ALIGN_LEFT);
            AddCell(item.Size, Element.ALIGN_CENTER);
            AddCell(item.Quantity.ToString(), Element.ALIGN_CENTER);
            AddCell(unitPrice.ToString("C"), Element.ALIGN_RIGHT);
            AddCell((item.Quantity * unitPrice).ToString("C"), Element.ALIGN_RIGHT);

            alternate = !alternate;
        }

        // 6. TOTAL ROW
        PdfPCell blankCell = new PdfPCell(new Phrase(" ")) { Colspan = 3, Border = iTextSharp.text.Rectangle.NO_BORDER };
        table.AddCell(blankCell);

        PdfPCell totalLabel = new PdfPCell(new Phrase("Grand Total", boldFont));
        totalLabel.HorizontalAlignment = Element.ALIGN_RIGHT;
        totalLabel.Padding = 8f;
        totalLabel.Border = iTextSharp.text.Rectangle.NO_BORDER;
        table.AddCell(totalLabel);

        // FIX: Use FontFactory.GetFont here instead of new Font()
        var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, chageeGreen);
        PdfPCell totalValue = new PdfPCell(new Phrase(order.TotalAmount.ToString("C"), totalFont));
        totalValue.HorizontalAlignment = Element.ALIGN_RIGHT;
        totalValue.Padding = 8f;
        totalValue.Border = iTextSharp.text.Rectangle.BOTTOM_BORDER;
        totalValue.BorderColor = chageeGold;
        table.AddCell(totalValue);

        doc.Add(table);

        // 7. FOOTER
        doc.Add(new Paragraph(" "));
        doc.Add(new Paragraph(" "));

        // FIX: Use FontFactory.GetFont here too
        var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, iTextSharp.text.Font.NORMAL, BaseColor.GRAY);
        Paragraph footer = new Paragraph("Thank you for choosing CHAGEE!\nThis is a computer-generated receipt.", footerFont);
        footer.Alignment = Element.ALIGN_CENTER;
        doc.Add(footer);

        doc.Close();

        // 8. Attach and Send
        ms.Position = 0;
        var mail = new MailMessage();
        mail.To.Add(order.MemberEmail);
        mail.Subject = $"Receipt: Order #{order.OrderID} - CHAGEE";
        mail.IsBodyHtml = true;
        mail.Body = $@"
        <div style='font-family: Arial, sans-serif; color: #333;'>
            <h2 style='color: #2d5a27;'>Order Confirmed!</h2>
            <p>Dear Customer,</p>
            <p>Thank you for shopping with CHAGEE. Your payment was successful.</p>
            <p>Please find your official <b>PDF Receipt</b> attached.</p>
            <br>
            <p>Best regards,<br><b>CHAGEE Team</b></p>
        </div>";

        var attachment = new Attachment(ms, $"CHAGEE_Receipt_{order.OrderID}.pdf", "application/pdf");
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
