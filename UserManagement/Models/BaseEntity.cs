namespace UserManagement.Models;

public abstract class BaseEntity
{
    public int Id { get; set; }

    public int GetId()
    {
        return Id;
    }
}