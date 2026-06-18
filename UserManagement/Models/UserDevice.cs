namespace UserManagement.Models;

public class UserDevice : BaseEntity
{
    public int UserId { get; set; }          // FK column
    public User User { get; set; } = null!;  // navigation property

    public string DeviceName { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string DeviceToken { get; set; } = "";
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}