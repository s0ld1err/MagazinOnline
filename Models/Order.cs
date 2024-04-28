using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;


namespace MagazinOnline.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("CustomerId")]

        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        public string DeliveryAddress { get; set; }

        public string PaymentMethod { get; set; }

        public string Phone { get; set; }
        public string Email { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public decimal TotalPrice { get; set; }

        public DateTime OrderDate { get; set; }
    }
}
