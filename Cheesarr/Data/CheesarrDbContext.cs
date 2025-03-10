using Cheesarr.Model;
using Microsoft.EntityFrameworkCore;

namespace Cheesarr.Data;

public class CheesarrDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<BookEntry> Books { get; set; }
    
    public DbSet<ProfileEntry> Profiles { get; set; }
    
    public DbSet<AuthorEntry> Authors { get; set; }
}