using System.ServiceModel;

namespace UserManagement.Grpc.Contracts;

[ServiceContract]
public interface IUserGrpcService
{
    [OperationContract]
    Task<GetUserResponse> GetUserAsync(GetUserRequest request);

    [OperationContract]
    Task<UpsertUserResponse> UpsertUserAsync(UpsertUserRequest request);

    [OperationContract]
    Task<UpsertDeviceResponse> UpsertDeviceAsync(UpsertDeviceRequest request);

    [OperationContract]
    Task<UpsertSessionResponse> UpsertSessionAsync(UpsertSessionRequest request);
}