using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using MagazinOnline.Models.DTO;
using MagazinOnline.Models.Account;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using MagazinOnline.Data;
using MagazinOnline.Models;
using Microsoft.EntityFrameworkCore;


[Route("[controller]")]
[ApiController]
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (model == null || !ModelState.IsValid)
        {
            return BadRequest("Invalid registration data.");
        }

        var user = new IdentityUser
        {
            UserName = model.Username,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest($"User creation failed: {errors}");
        }

        var customer = new Customer
        {
            Name = model.Username,
            Email = model.Email
        };

        var context = new OnlineStoreContext();
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        return Ok("User and customer profile created successfully");
    }

    // Endpointul pentru Login
    [AllowAnonymous]
    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = GetToken(authClaims);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        return Unauthorized("Invalid login attempt");
    }

    [Authorize]
    [HttpPost]
    [Route("UpdateCustomerDetails")]
    public async Task<IActionResult> UpdateCustomerDetails([FromBody] CustomerDTO customerDTO)
    {
        if (customerDTO == null || !ModelState.IsValid)
        {
            return BadRequest("Invalid data");
        }

        var context = new OnlineStoreContext();
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Email == customerDTO.Email);

        if (customer == null)
        {
            return NotFound("Customer not found");
        }

        customer.Name = customerDTO.Name;
        customer.Address = customerDTO.Address;
        customer.Phone = customerDTO.Phone;

        await context.SaveChangesAsync();

        return Ok("Customer details updated successfully");
    }

    // Functia pentru generarea JWT
    private JwtSecurityToken GetToken(List<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddMinutes(60),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return token;
    }
}
