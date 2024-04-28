using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MagazinOnline.Data;
using System.Linq;
using System.Threading.Tasks;
using MagazinOnline.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace MagazinOnline.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly OnlineStoreContext _context;

        public CartController(OnlineStoreContext context)
        {
            _context = context;
        }

        // GET: cart/{customerId}
        [Authorize]
        [HttpGet("adm/{customerId}")]
        public async Task<ActionResult<CartResponse>> GetCart(int customerId)
        {
            var cart = await _context.Carts
                                     .Include(c => c.CartItems)
                                     .ThenInclude(ci => ci.Product)
                                     .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                return NotFound("Cart not found for customer");
            }

            var cartResponse = new CartResponse
            {
                Id = cart.Id,
                CustomerId = customerId,
                CartItems = cart.CartItems.Select(ci => new CartItemResponse
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Price = ci.Product.Price * ci.Quantity,
                    Quantity = ci.Quantity
                }).ToList()
            };

            return Ok(cartResponse);
        }


        // POST: cart/{customerId}/add
        [Authorize]
        [HttpPost("{customerId}/add")]
        public async Task<IActionResult> AddToCart(int customerId, [FromBody] CartItemRequest request)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = customerId,
                    CartItems = new List<CartItem>()
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            return Ok("Product added to cart successfully");
        }

        // POST: cart/{customerId}/update-quantity
        [Authorize]
        [HttpPost("{customerId}/update-quantity")]
        public async Task<IActionResult> UpdateCartItemQuantity(int customerId, [FromBody] UpdateQuantityRequest request)
        {
            var cart = await _context.Carts.Include(c => c.CartItems)
                                           .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                return NotFound("Cart not found for customer");
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);

            if (cartItem == null)
            {
                return NotFound("CartItem not found in cart");
            }

            cartItem.Quantity += request.QuantityChange;

            if (cartItem.Quantity < 1)
            {
                _context.CartItems.Remove(cartItem);
            }

            await _context.SaveChangesAsync();

            return Ok("Cart item quantity updated successfully");
        }

        // POST: cart/{customerId}/remove
        [Authorize]
        [HttpPost("{customerId}/remove")]
        public async Task<IActionResult> RemoveFromCart(int customerId, [FromBody] int cartItemId)
        {
            var cart = await _context.Carts
                                           .Include(c => c.CartItems)
                                           .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                return NotFound("Cart not found for customer");
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem == null)
            {
                return NotFound("CartItem not found in cart");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok("Product removed from cart successfully");
        }

        // POST: cart/{customerId}/checkout
        [Authorize]
        [HttpPost("{customerId}/checkout")]
        public async Task<IActionResult> Checkout(int customerId, [FromBody] CheckoutRequest request)
        {

            if (!Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod))
            {
                return BadRequest("Invalid payment method.");
            }

            var cart = await _context.Carts.Include(c => c.CartItems)
                                           .ThenInclude(ci => ci.Product)
                                           .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                return NotFound("Cart not found for customer");
            }

            var customer = await _context.Customers
                                 .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
            {
                return NotFound("Customer not found");
            }

            var order = new Order
            {
                CustomerId = customerId,
                CustomerName = customer.Name,
                DeliveryAddress = customer.Address,
                PaymentMethod = request.PaymentMethod,
                Phone = customer.Phone,
                Email = customer.Email,
                OrderDate = DateTime.UtcNow,
                TotalPrice = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price),
                OrderItems = cart.CartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Ok("Checkout successful. Order has been created.");
        }
    }
}

// Request model for adding cart items
public class CartItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateQuantityRequest
{
    public int ProductId { get; set; }
    public int QuantityChange { get; set; }
}

public class CartResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public List<CartItemResponse> CartItems { get; set; }
}

public class CartItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public enum PaymentMethod
{
    Ramburs,
    Cash,
    CreditCard
}

public class CheckoutRequest
{
    public string PaymentMethod { get; set; }
}
