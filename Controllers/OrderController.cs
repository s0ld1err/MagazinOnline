using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MagazinOnline.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MagazinOnline.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OnlineStoreContext _context;

        public OrderController(OnlineStoreContext context)
        {
            _context = context;
        }

        // GET: orders/{customer_id}
        [Authorize]
        [HttpGet("{customerId}")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrdersForCustomer(int customerId)
        {
            var orders = await _context.Orders
                                       .Where(o => o.CustomerId == customerId)
                                       .Include(o => o.OrderItems)
                                       .ToListAsync();

            if (orders == null || orders.Count == 0)
            {
                return NotFound("No orders found for this customer.");
            }

            var response = orders.Select(o => new OrderResponse
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                OrderItems = o.OrderItems.Select(oi => new OrderItemResponse
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                TotalPrice = o.TotalPrice
            }).ToList();

            return Ok(response);
        }

        // GET: orders/{order_id}
        [Authorize]
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<OrderResponseDetailed>> GetOrderById(int orderId)
        {
            var order = await _context.Orders
                                      .Include(o => o.OrderItems)
                                      .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            var response = new OrderResponseDetailed
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                Phone = order.Phone,
                Email = order.Email,
                DeliveryAddress = order.DeliveryAddress,
                PaymentMethod = order.PaymentMethod,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice,
                OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

            return Ok(response);
        }


        // GET: orders
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponseDetailed>>> GetAllOrders()
        {
            var orders = await _context.Orders
                                       .Include(o => o.OrderItems)
                                       .ToListAsync();

            if (orders == null || orders.Count == 0)
            {
                return NotFound("No orders found.");
            }

            var response = orders.Select(o => new OrderResponseDetailed
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CustomerName = o.CustomerName,
                Phone = o.Phone,
                Email = o.Email,
                DeliveryAddress = o.DeliveryAddress,
                PaymentMethod = o.PaymentMethod,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                OrderItems = o.OrderItems.Select(oi => new OrderItemResponse
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            }).ToList();

            return Ok(response);
        }

    }
}


public class OrderResponse
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    public List<OrderItemResponse> OrderItems { get; set; }
}

public class OrderResponseDetailed
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string DeliveryAddress { get; set; }
    public string PaymentMethod { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    public List<OrderItemResponse> OrderItems { get; set; }
}

public class OrderItemResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}