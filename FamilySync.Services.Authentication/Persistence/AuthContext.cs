using FamilySync.Models.Authentication.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FamilySync.Services.Authentication.Persistence;

public class AuthContext : IdentityDbContext<UserIdentity, IdentityRole<Guid>, Guid>
{
    public AuthContext(DbContextOptions<AuthContext> options) : base(options)
    {
    }
    
    

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

      
    }
}