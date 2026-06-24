using Microsoft.EntityFrameworkCore;
using UserManagement.Grpc.Contracts;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Repositories;
namespace UserManagement.Grpc.Services;

public class UserGrpcService : IUserGrpcService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserGrpcService> _logger;
    private readonly IRepository<UserDevice> _userDeviceRepository;
    private readonly IRepository<UserSession> _userSessionRepository;
    
    public UserGrpcService(
        AppDbContext dbContext,
        ILogger<UserGrpcService> logger,
        IRepository<UserDevice> userDeviceRepository,
        IRepository<UserSession> userSessionRepository)
    {
        _dbContext = dbContext;
        _logger = logger;
        _userDeviceRepository = userDeviceRepository;
        _userSessionRepository = userSessionRepository;
    }
    
    private static UserGrpcModel MapUserToGrpcModel(User user)
    {
        var userCountry = user.UserCountries.FirstOrDefault();

        return new UserGrpcModel
        {
            PublicId = user.PublicId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            AccountBalance = user.AccountBalance,
            LoginCount = user.LoginCount,
            ReputationScore = user.ReputationScore,
            Role = (int)user.Role,
            Country = userCountry?.Country?.Name,
            CountryCode = userCountry?.Country?.Code,
            Id = user.Id,
            Salt = user.Salt,
            PasswordHash = user.PasswordHash
        };
    }

    private static UserDeviceGrpcModel MapDeviceToGrpcModel(UserDevice device)
    {
        return new UserDeviceGrpcModel
        {
            UserId = device.UserId,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            DeviceToken = device.DeviceToken,
            RegisteredAt = device.RegisteredAt,
            IsActive = device.IsActive,
            Id = device.Id
        };
    }

    private static UserSessionGrpcModel MapSessionToGrpcModel(UserSession session)
    {
        return new UserSessionGrpcModel
        {
            UserDeviceId = session.UserDeviceId,
            SessionToken = session.SessionToken,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            IsActive = session.IsActive
        };
    }
    
    public async Task<GetUserResponse> GetUserAsync(GetUserRequest request)
    {
        Console.WriteLine("GetUserAsync was called");

        try
        {
            _logger.LogInformation("Getting user. UserId: {UserId}, Email: {Email}",
                request.UserId, request.Email);

            User? user = null;

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var query = _dbContext.Users
                    .Include(u => u.UserCountries)
                    .ThenInclude(uc => uc.Country);

                if (int.TryParse(request.UserId, out var internalId))
                {
                    user = await query.FirstOrDefaultAsync(u => u.Id == internalId);
                }
                else if (Guid.TryParse(request.UserId, out var publicId))
                {
                    user = await query.FirstOrDefaultAsync(u => u.PublicId == publicId);
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user = await _dbContext.Users
                    .Include(u => u.UserCountries)
                    .ThenInclude(uc => uc.Country)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
            }

            if (user == null)
            {
                return new GetUserResponse
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            return new GetUserResponse
            {
                Success = true,
                ErrorMessage = null,
                Data = MapUserToGrpcModel(user)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            _logger.LogError(ex, "Error while getting user");

            return new GetUserResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while getting user"
            };
        }
    }

    public async Task<UpsertUserResponse> UpsertUserAsync(UpsertUserRequest request)
{
    try
    {
        var incomingUser = request.User;

        _logger.LogInformation("Upserting user. PublicId: {PublicId}, Email: {Email}",
            incomingUser.PublicId, incomingUser.Email);

        if (string.IsNullOrWhiteSpace(incomingUser.Email))
        {
            return new UpsertUserResponse
            {
                Success = false,
                ErrorMessage = "Email is required"
            };
        }

        var user = await _dbContext.Users
            .Include(u => u.UserCountries)
                .ThenInclude(uc => uc.Country)
            .FirstOrDefaultAsync(u => u.Email == incomingUser.Email);

        if (user == null)
        {
            user = new User
            {
                PublicId = incomingUser.PublicId == Guid.Empty ? Guid.NewGuid() : incomingUser.PublicId,
                FirstName = incomingUser.FirstName,
                LastName = incomingUser.LastName,
                Email = incomingUser.Email,
                PhoneNumber = incomingUser.PhoneNumber,
                DateOfBirth = DateTime.SpecifyKind(incomingUser.DateOfBirth, DateTimeKind.Utc),
                CreatedAt = DateTime.SpecifyKind(incomingUser.CreatedAt, DateTimeKind.Utc),
                IsActive = incomingUser.IsActive,
                AccountBalance = incomingUser.AccountBalance,
                LoginCount = incomingUser.LoginCount,
                ReputationScore = incomingUser.ReputationScore,
                Role = (UserRole)incomingUser.Role,
                Salt = incomingUser.Salt,
                PasswordHash = incomingUser.PasswordHash,
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            user.FirstName = incomingUser.FirstName;
            user.LastName = incomingUser.LastName;
            user.PhoneNumber = incomingUser.PhoneNumber;
            user.DateOfBirth = DateTime.SpecifyKind(incomingUser.DateOfBirth, DateTimeKind.Utc);;
            user.IsActive = incomingUser.IsActive;
            user.AccountBalance = incomingUser.AccountBalance;
            user.LoginCount = incomingUser.LoginCount;
            user.ReputationScore = incomingUser.ReputationScore;
            user.Role = (UserRole)incomingUser.Role;
            user.Salt = incomingUser.Salt;
            user.PasswordHash = incomingUser.PasswordHash;
        }

        if (!string.IsNullOrWhiteSpace(incomingUser.Country))
        {
            var country = await _dbContext.Countries
                .FirstOrDefaultAsync(c => c.Name == incomingUser.Country || c.Code == incomingUser.CountryCode);

            if (country == null)
            {
                country = new Country
                {
                    Name = incomingUser.Country!,
                    Code = incomingUser.CountryCode!
                };

                await _dbContext.Countries.AddAsync(country);
                await _dbContext.SaveChangesAsync();
            }

            var userCountry = await _dbContext.UserCountries
                .FirstOrDefaultAsync(uc => uc.UserId == user.Id);

            if (userCountry == null)
            {
                await _dbContext.UserCountries.AddAsync(new UserCountry
                {
                    UserId = user.Id,
                    CountryId = country.Id
                });
            }
            else
            {
                userCountry.CountryId = country.Id;
            }
        }

        await _dbContext.SaveChangesAsync();

        user = await _dbContext.Users
            .Include(u => u.UserCountries)
                .ThenInclude(uc => uc.Country)
            .FirstAsync(u => u.Email == incomingUser.Email);

        return new UpsertUserResponse
        {
            Success = true,
            ErrorMessage = null,
            Data = MapUserToGrpcModel(user)
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error while upserting user");

        return new UpsertUserResponse
        {
            Success = false,
            ErrorMessage = "An unexpected error occurred while upserting user"
        };
    }
}

    public async Task<UpsertDeviceResponse> UpsertDeviceAsync(UpsertDeviceRequest request)
{
    try
    {
        var incomingDevice = request.UserDevice;

        _logger.LogInformation("Upserting device. UserId: {UserId}, DeviceToken: {DeviceToken}",
            incomingDevice.UserId, incomingDevice.DeviceToken);

        if (incomingDevice.UserId <= 0)
        {
            return new UpsertDeviceResponse
            {
                Success = false,
                ErrorMessage = "UserId is required"
            };
        }

        if (string.IsNullOrWhiteSpace(incomingDevice.DeviceToken))
        {
            return new UpsertDeviceResponse
            {
                Success = false,
                ErrorMessage = "Device token is required"
            };
        }

        var userExists = await _dbContext.Users
            .AnyAsync(u => u.Id == incomingDevice.UserId);

        if (!userExists)
        {
            return new UpsertDeviceResponse
            {
                Success = false,
                ErrorMessage = "User not found"
            };
        }

        var device = await _dbContext.UserDevices
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.DeviceToken == incomingDevice.DeviceToken);

        if (device == null)
        {
            device = new UserDevice
            {
                UserId = incomingDevice.UserId,
                DeviceName = incomingDevice.DeviceName,
                DeviceType = incomingDevice.DeviceType,
                DeviceToken = incomingDevice.DeviceToken,
                RegisteredAt = DateTime.UtcNow,
                IsActive = incomingDevice.IsActive
            };

            await _userDeviceRepository.CreateAsync(device);
        }
        else
        {
            device.UserId = incomingDevice.UserId;
            device.DeviceName = incomingDevice.DeviceName;
            device.DeviceType = incomingDevice.DeviceType;
            device.IsActive = incomingDevice.IsActive;
        }

        await _userDeviceRepository.UpdateAsync(device);

        device = await _dbContext.UserDevices
            .Include(d => d.User)
            .FirstAsync(d => d.Id == device.Id);

        return new UpsertDeviceResponse
        {
            Success = true,
            ErrorMessage = null,
            Data = MapDeviceToGrpcModel(device)
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error while upserting device");

        return new UpsertDeviceResponse
        {
            Success = false,
            ErrorMessage = "An unexpected error occurred while upserting device"
        };
    }
}

    public async Task<UpsertSessionResponse> UpsertSessionAsync(UpsertSessionRequest request)
{
    try
    {
        var incomingSession = request.UserSession;

        _logger.LogInformation("Upserting session. UserDeviceId: {UserDeviceId}, SessionToken: {SessionToken}",
            incomingSession.UserDeviceId, incomingSession.SessionToken);

        if (incomingSession.UserDeviceId <= 0)
        {
            return new UpsertSessionResponse
            {
                Success = false,
                ErrorMessage = "UserDeviceId is required"
            };
        }

        if (string.IsNullOrWhiteSpace(incomingSession.SessionToken))
        {
            return new UpsertSessionResponse
            {
                Success = false,
                ErrorMessage = "Session token is required"
            };
        }

        var userDevice = await _dbContext.UserDevices
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == incomingSession.UserDeviceId);

        if (userDevice == null)
        {
            return new UpsertSessionResponse
            {
                Success = false,
                ErrorMessage = "User device not found"
            };
        }

        var session = await _dbContext.UserSessions
            .Include(s => s.User)
            .Include(s => s.UserDevice)
            .FirstOrDefaultAsync(s => s.SessionToken == incomingSession.SessionToken);

        if (session == null)
        {
            session = new UserSession
            {
                User = userDevice.User,
                UserDeviceId = incomingSession.UserDeviceId,
                SessionToken = incomingSession.SessionToken,
                CreatedAt = DateTime.SpecifyKind(incomingSession.CreatedAt, DateTimeKind.Utc),
                ExpiresAt = DateTime.SpecifyKind(incomingSession.ExpiresAt, DateTimeKind.Utc),
                IsActive = incomingSession.IsActive
            };

            await _userSessionRepository.CreateAsync(session);
        }
        else
        {
            session.User = userDevice.User;
            session.UserDeviceId = incomingSession.UserDeviceId;
            session.ExpiresAt = incomingSession.ExpiresAt;
            session.IsActive = incomingSession.IsActive;
            session.CreatedAt = DateTime.SpecifyKind(incomingSession.CreatedAt, DateTimeKind.Utc);
            session.ExpiresAt = DateTime.SpecifyKind(incomingSession.ExpiresAt, DateTimeKind.Utc);
        }

        await _userSessionRepository.UpdateAsync(session);

        session = await _dbContext.UserSessions
            .Include(s => s.User)
            .Include(s => s.UserDevice)
            .FirstAsync(s => s.Id == session.Id);

        return new UpsertSessionResponse
        {
            Success = true,
            ErrorMessage = null,
            Data = MapSessionToGrpcModel(session)
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error while upserting session");

        return new UpsertSessionResponse
        {
            Success = false,
            ErrorMessage = "An unexpected error occurred while upserting session"
        };
    }
}
}