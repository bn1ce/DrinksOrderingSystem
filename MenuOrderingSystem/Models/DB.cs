using MenuOrderingSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable warnings

namespace MenuOrderingSystem.Models
{
    public class DB : DbContext
    {
        public DB(DbContextOptions<DB> options) : base(options)
        {
            // DbSets
        }

        public DbSet<Drink> Drinks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

    }

    public class Drink
    {
        public int DrinkID { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public decimal PriceRegular { get; set; }
        public decimal PriceLarge { get; set; }

        public string ImageURL { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        // Foreign Key
        public int CategoryID { get; set; }

        // Navigation Property
        public virtual Category Category { get; set; } = null!;


    }

    public class Category
    {
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        // Navigation Property
        public virtual ICollection<Drink> Drinks { get; set; } = new List<Drink>();
    }

    public class Order
    {
        public int OrderID { get; set; }

        [Required]
        public string MemberEmail { get; set; }  // Foreign Key (Customer)

        [Required]
        public DateTime OrderTime { get; set; }

        [Precision(7, 2)]
        public decimal TotalAmount { get; set; }
        public string StripeSessionId { get; set; }
        public string Status { get; set; } = "Pending";

        // Navigation Properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Member Member { get; set; }

    }

    public class OrderItem
    {
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }
        public int DrinkID { get; set; }

        public string Size { get; set; } = string.Empty;
        public string SugarLevel { get; set; } = string.Empty;
        public string IceLevel { get; set; } = string.Empty;
        public int Quantity { get; set; }

        // Navigation
        public virtual Drink Drink { get; set; } = null!;
        public virtual Order Order { get; set; } = null!;
    }

    public class User
    {
        [Key, MaxLength(100)]
        public string Email { get; set; }
        [MaxLength(100)]
        public string Hash { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(12)]
        public string Phone { get; set; }
        public string Role => GetType().Name;
    }

    public class Admin : User
    {

    }

    public class Member : User
    {
        [MaxLength(100)]
        public string PhotoURL { get; set; }

        // Navigation Properties
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    }



    public class DrinkVM
    {
        public int DrinkID { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal PriceRegular { get; set; }

        [Required]
        public decimal PriceLarge { get; set; }

        public bool IsAvailable { get; set; }

        [Required]
        public int CategoryID { get; set; }

        // For image upload
        public IFormFile? Image { get; set; }

        // To display in Edit
        public string? ImageURL { get; set; }
    }



    public class Feedback
    {
        public int FeedbackID { get; set; }   // Primary Key

        [Required, MaxLength(100)]
        public string Email { get; set; }     // Foreign Key to User

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public DateTime FeedbackTime { get; set; } = DateTime.Now;

        [Range(1, 10)]
        public int Rating { get; set; }       // 1–10 rating

        [StringLength(500)]
        public string Comment { get; set; }

        // Navigation Property
        public virtual User User { get; set; }
    }


}