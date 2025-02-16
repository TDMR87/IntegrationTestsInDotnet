namespace Bloqqer.Database;

public class BloqqerDbContext(DbContextOptions<BloqqerDbContext> options) : DbContext(options)
{
    public required DbSet<Article> Articles { get; set; }
    public required DbSet<User> Users { get; set; }
    public required DbSet<UserRegistrationConfirmation> UserRegistrationConfirmations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BloqqerDbContext).Assembly);

        ApplyGlobalQueryFilters(modelBuilder);
    }

    private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
    }
}

public static class DbContextExtensions
{
    public static IQueryable<T> If<T>(this IQueryable<T> query, bool predicate, Func<IQueryable<T>, IQueryable<T>> filter)
    {
        return predicate ? filter(query) : query;
    }
}