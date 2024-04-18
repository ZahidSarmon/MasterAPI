
namespace Master.Domain;

public abstract class Audit<T> : IAudit
{
    public T Id { get; set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? ModifiedOn { get; private set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    public void SetCreatedOn(DateTime dateTime)
    {
        CreatedOn = dateTime;
    }

    public void SetModifiedOn(DateTime dateTime)
    {
        ModifiedOn = dateTime;
    }
}