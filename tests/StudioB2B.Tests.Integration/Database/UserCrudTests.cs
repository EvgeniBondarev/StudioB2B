using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Profiles;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class UserCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    private static readonly IMapper Mapper = new ServiceCollection()
        .AddLogging()
        .AddAutoMapper(cfg => cfg.AddProfile(new UserMappingProfile()))
        .BuildServiceProvider()
        .GetRequiredService<IMapper>();

    public UserCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateUser_WithNoPermissions_Succeeds()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var email = $"user_{Guid.NewGuid():N}@test.com";
        var dto = new CreateUserDto(email, "Иван", "Тестов", null, "Pass123!", []);

        var (success, error, _) = await ctx.CreateUserAsync(dto, Mapper, requireEmailActivation: false);

        success.Should().BeTrue(error);

        var normalizedEmail = email.ToLowerInvariant();
        var user = await ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        user.Should().NotBeNull();
        user.FirstName.Should().Be("Иван");
        user.LastName.Should().Be("Тестов");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsFalse()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var dto = new CreateUserDto(email, "А", "Б", null, "Pass1!", []);

        var (ok1, _, _) = await ctx.CreateUserAsync(dto, Mapper, requireEmailActivation: false);
        var (ok2, err2, _) = await ctx.CreateUserAsync(dto, Mapper, requireEmailActivation: false);

        ok1.Should().BeTrue();
        ok2.Should().BeFalse();
        err2.Should().Contain("существует");
    }

    [Fact]
    public async Task UpdateUser_ChangesFirstName_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var email = $"upd_{Guid.NewGuid():N}@test.com";
        var createDto = new CreateUserDto(email, "Старое", "Фамилия", null, "Pass1!", []);
        await ctx.CreateUserAsync(createDto, Mapper, requireEmailActivation: false);

        var normalizedEmail = email.ToLowerInvariant();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Email == normalizedEmail);

        var updateDto = new UpdateUserDto("НовоеИмя", "Фамилия", null, true, []);
        var (ok, _) = await ctx.UpdateUserAsync(user.Id, updateDto, Mapper);

        ok.Should().BeTrue();

        var updated = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        updated.FirstName.Should().Be("НовоеИмя");
    }

    [Fact]
    public async Task DeleteUser_SoftDeletes()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var email = $"del_{Guid.NewGuid():N}@test.com";
        var dto = new CreateUserDto(email, "А", "Б", null, "Pass1!", []);
        await ctx.CreateUserAsync(dto, Mapper, requireEmailActivation: false);

        var normalizedEmail = email.ToLowerInvariant();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Email == normalizedEmail);

        var (ok, _) = await ctx.DeleteUserAsync(user.Id);
        ok.Should().BeTrue();

        var found = await ctx.Users.AsNoTracking().AnyAsync(u => u.Id == user.Id);
        found.Should().BeFalse("soft-deleted user must be filtered out");

        var foundWithIgnore = await ctx.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(u => u.Id == user.Id);
        foundWithIgnore.Should().BeTrue("user should still exist with IgnoreQueryFilters");
    }
}

