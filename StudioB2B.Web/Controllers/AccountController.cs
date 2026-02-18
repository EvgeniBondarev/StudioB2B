using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Web.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ITenantProvider tenantProvider,
        ITenantDbContextFactory dbContextFactory,
        ILogger<AccountController> logger)
    {
        _tenantProvider = tenantProvider;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] bool rememberMe = false,
        [FromForm] string? returnUrl = null)
    {
        if (!_tenantProvider.IsResolved)
        {
            return Redirect("/login?error=tenant");
        }

        try
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();

            // Create UserManager manually
            var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore
                .UserStore<ApplicationUser, ApplicationRole, TenantDbContext, Guid>(dbContext);
            var hasher = new PasswordHasher<ApplicationUser>();
            var normalizer = new UpperInvariantLookupNormalizer();

            var validators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
            var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };

            using var userManager = new UserManager<ApplicationUser>(
                userStore,
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                hasher,
                validators,
                passwordValidators,
                normalizer,
                new IdentityErrorDescriber(),
                null!,
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<ApplicationUser>>());

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: user {Email} not found", email);
                return Redirect("/login?error=invalid");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: user {Email} is inactive", email);
                return Redirect("/login?error=inactive");
            }

            var isValidPassword = await userManager.CheckPasswordAsync(user, password);
            if (!isValidPassword)
            {
                _logger.LogWarning("Login failed: invalid password for {Email}", email);
                return Redirect("/login?error=invalid");
            }

            // Create SignInManager and sign in
            var signInManager = CreateSignInManager(userManager, dbContext);
            await signInManager.SignInAsync(user, rememberMe);

            _logger.LogInformation("User {Email} logged in successfully", email);

            return Redirect(returnUrl ?? "/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", email);
            return Redirect("/login?error=error");
        }
    }

    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!_tenantProvider.IsResolved)
        {
            return Redirect("/");
        }

        try
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();

            var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore
                .UserStore<ApplicationUser, ApplicationRole, TenantDbContext, Guid>(dbContext);
            var hasher = new PasswordHasher<ApplicationUser>();
            var normalizer = new UpperInvariantLookupNormalizer();

            using var userManager = new UserManager<ApplicationUser>(
                userStore,
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                hasher,
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                normalizer,
                new IdentityErrorDescriber(),
                null!,
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<ApplicationUser>>());

            var signInManager = CreateSignInManager(userManager, dbContext);
            await signInManager.SignOutAsync();

            _logger.LogInformation("User logged out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
        }

        return Redirect("/");
    }

    private SignInManager<ApplicationUser> CreateSignInManager(
        UserManager<ApplicationUser> userManager,
        TenantDbContext dbContext)
    {
        var contextAccessor = HttpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();
        var claimsFactory = new UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(
            userManager,
            new RoleManager<ApplicationRole>(
                new Microsoft.AspNetCore.Identity.EntityFrameworkCore
                    .RoleStore<ApplicationRole, TenantDbContext, Guid>(dbContext),
                Array.Empty<IRoleValidator<ApplicationRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoleManager<ApplicationRole>>()),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()));

        return new SignInManager<ApplicationUser>(
            userManager,
            contextAccessor,
            claimsFactory,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SignInManager<ApplicationUser>>(),
            new Microsoft.AspNetCore.Authentication.AuthenticationSchemeProvider(
                Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Authentication.AuthenticationOptions())),
            new DefaultUserConfirmation<ApplicationUser>());
    }
}
