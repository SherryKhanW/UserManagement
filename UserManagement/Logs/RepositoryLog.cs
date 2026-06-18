namespace UserManagement.Logs;

public class RepositoryLog
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string RepositoryName { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string MethodName { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public string? ErrorMessage { get; set; }
}