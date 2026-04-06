using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DDHSTORE.Models
{
    public class CheckoutViewModel
    {
        // User
        public int UserId { get; set; }
        public string Username { get; set; }

        // Checkout Type
        public bool IsCartCheckout { get; set; }

        // Multi-item Support
        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();

        // Backward Compatibility for Single Product (still used in form or Buy Now)
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải >= 1")]
        public int Quantity { get; set; }

        public int StockQuantity { get; set; }

        public decimal TotalAmount => IsCartCheckout ? Items.Sum(i => i.Price * i.Quantity) : Price * Quantity;

        // Payment
        [Required(ErrorMessage = "Chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        // Address
        public int? AddressId { get; set; } // Nếu dùng địa chỉ có sẵn
        public List<Address> UserAddresses { get; set; } = new List<Address>();

        // Thêm địa chỉ mới
        public string RecipientName { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Detail { get; set; }
        public bool SaveAddress { get; set; } = true;
    }

    public class CheckoutItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
    }
}