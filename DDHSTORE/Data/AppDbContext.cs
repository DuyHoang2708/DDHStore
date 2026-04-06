
using DDHSTORE.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DDHSTORE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 🔥 DbSet (map tất cả bảng)
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
   
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Cart> Carts { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔥 ROLE
            modelBuilder.Entity<Role>().ToTable("ROLE");

            // 🔥 USERS
            modelBuilder.Entity<User>()
                .ToTable("USERS")
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            // 🔥 CATEGORY
            modelBuilder.Entity<Category>().ToTable("CATEGORY");

            // 🔥 BRAND
            modelBuilder.Entity<Brand>().ToTable("BRAND");

            // 🔥 PRODUCT
            modelBuilder.Entity<Product>()
                .ToTable("PRODUCT")
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId);

            // 🔥 PRODUCT_COLOR
            modelBuilder.Entity<ProductColor>()
                .ToTable("PRODUCT_COLOR")
                .HasOne(pc => pc.Product)
                .WithMany(p => p.Colors)
                .HasForeignKey(pc => pc.ProductId);

     

            // 🔥 ORDERS
            modelBuilder.Entity<Order>()
                .ToTable("ORDERS")
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Address)
                .WithMany()
                .HasForeignKey(o => o.AddressId);

            // 🔥 ORDER_DETAIL
            modelBuilder.Entity<OrderDetail>()
                .ToTable("ORDER_DETAIL")
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId);

            // 🔥 PAYMENT (1-1)
            modelBuilder.Entity<Payment>()
                .ToTable("PAYMENT")
                .HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId);
        }
    }
}