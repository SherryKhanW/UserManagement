using ProtoBuf;

namespace UserManagement.Grpc.Contracts;

[ProtoContract]
public class UserGrpcModel
{
    [ProtoMember(1)]
    public Guid PublicId { get; set; }

    [ProtoMember(2)]
    public string FirstName { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string LastName { get; set; } = string.Empty;

    [ProtoMember(4)]
    public string Email { get; set; } = string.Empty;

    [ProtoMember(5)]
    public string? PhoneNumber { get; set; }

    [ProtoMember(6)]
    public DateTime DateOfBirth { get; set; }

    [ProtoMember(7)]
    public DateTime CreatedAt { get; set; }

    [ProtoMember(8)]
    public bool IsActive { get; set; }

    [ProtoMember(9)]
    public decimal AccountBalance { get; set; }

    [ProtoMember(10)]
    public int LoginCount { get; set; }

    [ProtoMember(11)]
    public double ReputationScore { get; set; }

    [ProtoMember(12)]
    public int Role { get; set; }

    [ProtoMember(13)]
    public string? Country { get; set; }

    [ProtoMember(14)]
    public string? CountryCode { get; set; }
    
    [ProtoMember(15)]
    public int Id { get; set; }

    [ProtoMember(16)]
    public string Salt { get; set; } = string.Empty;

    [ProtoMember(17)]
    public string PasswordHash { get; set; } = string.Empty;
}

[ProtoContract]
public class UserDeviceGrpcModel
{
    [ProtoMember(1)]
    public int UserId { get; set; }

    [ProtoMember(2)]
    public string DeviceName { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string DeviceType { get; set; } = string.Empty;

    [ProtoMember(4)]
    public string DeviceToken { get; set; } = string.Empty;

    [ProtoMember(5)]
    public DateTime RegisteredAt { get; set; }

    [ProtoMember(6)]
    public bool IsActive { get; set; }
    
    [ProtoMember(7)]
    public int Id { get; set; }
}

[ProtoContract]
public class UserSessionGrpcModel
{
    [ProtoMember(1)]
    public int UserDeviceId { get; set; }

    [ProtoMember(2)]
    public string SessionToken { get; set; } = string.Empty;

    [ProtoMember(3)]
    public DateTime CreatedAt { get; set; }

    [ProtoMember(4)]
    public DateTime ExpiresAt { get; set; }

    [ProtoMember(5)]
    public bool IsActive { get; set; }
}

[ProtoContract]
public class GetUserRequest
{
    [ProtoMember(1)]
    public string? UserId { get; set; }

    [ProtoMember(2)]
    public string? Email { get; set; }
}

[ProtoContract]
public class UpsertUserRequest
{
    [ProtoMember(1)]
    public UserGrpcModel User { get; set; } = new();
}

[ProtoContract]
public class UpsertDeviceRequest
{
    [ProtoMember(1)]
    public UserDeviceGrpcModel UserDevice { get; set; } = new();
}

[ProtoContract]
public class UpsertSessionRequest
{
    [ProtoMember(1)]
    public UserSessionGrpcModel UserSession { get; set; } = new();
}

[ProtoContract]
public class GetUserResponse
{
    [ProtoMember(1)]
    public bool Success { get; set; }

    [ProtoMember(2)]
    public string? ErrorMessage { get; set; }

    [ProtoMember(3)]
    public UserGrpcModel? Data { get; set; }
}

[ProtoContract]
public class UpsertUserResponse
{
    [ProtoMember(1)]
    public bool Success { get; set; }

    [ProtoMember(2)]
    public string? ErrorMessage { get; set; }

    [ProtoMember(3)]
    public UserGrpcModel? Data { get; set; }
}

[ProtoContract]
public class UpsertDeviceResponse
{
    [ProtoMember(1)]
    public bool Success { get; set; }

    [ProtoMember(2)]
    public string? ErrorMessage { get; set; }

    [ProtoMember(3)]
    public UserDeviceGrpcModel? Data { get; set; }
}

[ProtoContract]
public class UpsertSessionResponse
{
    [ProtoMember(1)]
    public bool Success { get; set; }

    [ProtoMember(2)]
    public string? ErrorMessage { get; set; }

    [ProtoMember(3)]
    public UserSessionGrpcModel? Data { get; set; }
}
