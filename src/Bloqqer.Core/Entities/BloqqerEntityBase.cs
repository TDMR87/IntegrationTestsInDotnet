namespace Bloqqer.Core.Entities;

public abstract class BloqqerEntityBase
{
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
