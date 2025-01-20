using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bloqqer.Database.Interceptors;
public class TimestampInterceptor : SaveChangesInterceptor
{
    public TimestampInterceptor() { }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetTimestamps(DbContext? dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));

        var entries = dbContext.ChangeTracker.Entries<BloqqerEntityBase>();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.ModifiedAt = utcNow;
                entry.Entity.DeletedAt = entry.Entity.IsDeleted ? utcNow : null;
            }
            else if (entry.State is EntityState.Modified)
            {
                entry.Entity.ModifiedAt = utcNow;
            }
            else if (entry.State is EntityState.Deleted)
            {
                entry.State = EntityState.Modified; // Always soft-delete entities
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = utcNow;
            }
        }
    }
}