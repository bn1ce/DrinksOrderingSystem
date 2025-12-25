using MenuOrderingSystem.Models;
using MenuOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace MenuOrderingSystem.Controllers { 

    public class AccountController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;


        public AccountController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        // POST: Account/Login
        [HttpPost]
        public IActionResult Login(LoginVM vm, string? returnURL)
        {
            var user = db.Users.Find(vm.Email);

            if (user == null || !hp.VerifyPassword(user.PasswordHash, vm.Password))
            {
                ModelState.AddModelError("", "Login credentials not matched.");
                return View(vm);
            }

            // login OK
            hp.SignIn(user.Email, user.Role, vm.RememberMe);
            TempData["Info"] = "Login successfully.";

            // if user originally tried accessing a protected page
            if (!string.IsNullOrEmpty(returnURL))
                return Redirect(returnURL);

            // ROLE-BASED REDIRECT
            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            // fallback
            return RedirectToAction("Index", "Home");
        }


        // GET: Account/Logout
        public IActionResult Logout(string? returnURL)
        {
            TempData["Info"] = "Logout successfully.";

            hp.SignOut();

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied(string? returnURL)
        {
            return View();
        }



        // ------------------------------------------------------------------------
        // Others
        // ------------------------------------------------------------------------

        // GET: Account/CheckEmail
        public bool CheckEmail(string email)
        {
            return !db.Users.Any(u => u.Email == email);
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        public IActionResult Register(RegisterVM vm)
        {
            if (ModelState.IsValid("Email") &&
                db.Users.Any(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Duplicated Email.");
            }

            if (ModelState.IsValid("Photo"))
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }

            if (ModelState.IsValid)
            {
                db.Members.Add(new()
                {
                    Email = vm.Email,
                    PasswordHash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    PhotoURL = hp.SavePhoto(vm.Photo, "photos"),
                    Phone = vm.PhoneNumber
                });
                db.SaveChanges();

                TempData["Info"] = "Register successfully. Please login.";
                return RedirectToAction("Login");
            }

            return View(vm);
        }

        // GET: Account/UpdatePassword
        [Authorize]
        public IActionResult UpdatePassword()
        {
            return View();
        }

        // POST: Account/UpdatePassword
        [Authorize]
        [HttpPost]
        public IActionResult UpdatePassword(UpdatePasswordVM vm)
        {
            var u = db.Users.Find(User.Identity!.Name);
            if (u == null) return RedirectToAction("Index", "Home");

            if (!hp.VerifyPassword(u.PasswordHash, vm.Current))
            {
                ModelState.AddModelError("Current", "Current Password not matched.");
            }

            if (ModelState.IsValid)
            {
                u.PasswordHash = hp.HashPassword(vm.New);
                db.SaveChanges();

                TempData["Info"] = "Password updated.";
                return RedirectToAction();
            }

            return View();
        }

        // GET: Account/UpdateProfile
        [Authorize(Roles = "Member")]
        public IActionResult UpdateProfile()
        {
            var m = db.Set<Member>().Find(User.Identity!.Name);
            if (m == null) return RedirectToAction("Index", "Home");

            var vm = new UpdateProfileVM
            {
                Email = m.Email,
                Name = m.Name,
                PhotoURL = m.PhotoURL,
            };

            return View(vm);
        }

        // POST: Account/UpdateProfile
        [Authorize(Roles = "Member")]
        [HttpPost]
        public IActionResult UpdateProfile(UpdateProfileVM vm)
        {
            // Replace all instances of:
            // var m = db.Members.Find(User.Identity!.Name);

            // With:
            var m = db.Set<Member>().Find(User.Identity!.Name);
            if (m == null) return RedirectToAction("Index", "Home");

            if (vm.Photo != null)
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }

            if (ModelState.IsValid)
            {
                m.Name = vm.Name;

                if (vm.Photo != null)
                {
                    hp.DeletePhoto(m.PhotoURL, "photos");
                    m.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
                }

                db.SaveChanges();

                TempData["Info"] = "Profile updated.";
                return RedirectToAction();
            }

            vm.Email = m.Email;
            vm.PhotoURL = m.PhotoURL;
            return View(vm);
        }

        // GET: Account/ResetPassword
        public IActionResult ResetPassword()
        {
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordVM vm)
        {
            var u = db.Users.Find(vm.Email);

            if (u == null)
            {
                ModelState.AddModelError("Email", "Email not found.");
            }

            if (ModelState.IsValid)
            {
                string password = hp.RandomPassword();

                u!.PasswordHash = hp.HashPassword(password);
                db.SaveChanges();

                // Send reset password email
                SendResetPasswordEmail(u, password);

                TempData["Info"] = $"Password reset. Check your email.";
                return RedirectToAction();
            }

            return View();
        }

        private void SendResetPasswordEmail(User u, string password)
        {
            var mail = new MailMessage();
            mail.To.Add(new MailAddress(u.Email, u.Name));
            mail.Subject = "Reset Password";
            mail.IsBodyHtml = true;

            var url = Url.Action("Login", "Account", null, "https");

            // 1. GET FILENAME
            string? photoFile = u switch
            {
                Admin a => a.PhotoURL,
                Member m => m.PhotoURL,
                _ => null
            };

            // 2. BUILD PATH
            string path = "";
            if (!string.IsNullOrEmpty(photoFile))
            {
                path = Path.Combine(en.WebRootPath, "photos", photoFile);
            }

            // 3. FALLBACK TO LOGO
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                path = Path.Combine(en.WebRootPath, "images", "logo.png");
            }

            // 4. ATTACH
            if (System.IO.File.Exists(path))
            {
                var att = new Attachment(path);
                att.ContentId = "photo";
                mail.Attachments.Add(att);
            }

            // 5. BODY
            mail.Body = $@"
        <div style='text-align: center; font-family: Arial, sans-serif; padding: 20px;'>
            <img src='cid:photo' style='width: 150px; height: 150px; object-fit: cover; border-radius: 50%; border: 3px solid #c5a059;'>
            <h2 style='color: #1a3c1e;'>Password Reset</h2>
            <p style='color: #333;'>Dear {u.Name},</p>
            <p style='color: #555;'>Your password has been successfully reset to:</p>
            <div style='background: #f4f6f8; display: inline-block; padding: 15px 30px; border-radius: 8px; margin: 10px 0;'>
                <h1 style='color: #c5a059; margin: 0; letter-spacing: 2px;'>{password}</h1>
            </div>
            <p style='color: #555;'>
                Please <a href='{url}' style='color: #1a3c1e; font-weight: bold;'>login here</a> with your new password.
            </p>
            <br>
            <hr style='border: 0; border-top: 1px solid #eee;'>
            <p style='color: #999; font-size: 12px; margin-top: 20px;'>
                Best regards,<br><b>CHAGEE Team</b>
            </p>
        </div>
    ";

            hp.SendEmail(mail);
        }

        // GET: Account/Profile
        [Authorize]
        public IActionResult Profile()
        {
            if (User.IsInRole("Member"))
            {
                var m = db.Set<Member>().Find(User.Identity!.Name);
                if (m == null) return RedirectToAction("Index", "Home");

                return View("MemberProfile", m);
            }
            else if (User.IsInRole("Admin"))
            {
                var a = db.Set<Admin>().Find(User.Identity!.Name);
                if (a == null) return RedirectToAction("Index", "Home");

                return View("AdminProfile", a);
            }

            return RedirectToAction("Index", "Home");
        }

    }

}
