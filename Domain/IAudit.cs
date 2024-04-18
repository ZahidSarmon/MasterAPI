namespace Master.Domain;

public interface IAudit
{
    DateTime CreatedOn { get; }
    DateTime? ModifiedOn { get; }

    void SetCreatedOn(DateTime dateTime);

    void SetModifiedOn(DateTime dateTime);
}
