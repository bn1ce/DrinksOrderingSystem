using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

#nullable disable warnings

namespace MenuOrderingSystem.Models.ViewModels
{
    public class LoginVM
    {
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class RegisterVM
    {
        [StringLength(100)]
        [EmailAddress]
        [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
        public string Email { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Compare("Password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string Confirm { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public IFormFile Photo { get; set; }

        [Phone]
        [StringLength(15)]
        public string PhoneNumber { get; set; } // Add this property to fix CS1061
    }
    public class UpdatePasswordVM
    {
        [StringLength(100, MinimumLength = 5)]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string Current { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string New { get; set; }

        [StringLength(100, MinimumLength = 5)]
        [Compare("New")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string Confirm { get; set; }
    }

    public class UpdateProfileVM
    {
        public string? Email { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public string? PhotoURL { get; set; }

        public IFormFile? Photo { get; set; }
    }

    public class ResetPasswordVM
    {
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }
    }
    public class DrinkMenuViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Drink> Drinks { get; set; } = new();
        public int SelectedCategoryId { get; set; }
    }

    public class CartItem
    {
        public Drink Drink { get; set; } = new();
        public int DrinkID { get; set; }
        public string DrinkName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string IceLevel { get; set; } = string.Empty;
        public string SugarLevel { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CheckoutViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }



}
