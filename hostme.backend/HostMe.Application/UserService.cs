using HostMe.Domain.Entities;
using HostMe.Domain.Repositories;
using HostMe.Domain.Security;
using HostMe.Domain.Services;
using HostMe.Domain.Services.Models;
using HostMe.Domain.Constants;

namespace HostMe.Application;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ArgumentException(ErrorMessages.User.UsernameRequired);
        
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException(ErrorMessages.User.EmailRequired);
        
        if (!IsValidEmail(request.Email))
            throw new ArgumentException(ErrorMessages.User.EmailInvalid);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException(ErrorMessages.User.PasswordLength);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var existingEmailUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingEmailUser != null)
            throw new InvalidOperationException(ErrorMessages.User.EmailTaken);

        var existingUsernameUser = await _userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);
        if (existingUsernameUser != null)
            throw new InvalidOperationException(ErrorMessages.User.UsernameTaken);

        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException(ErrorMessages.User.EmailRequired);

        if (!IsValidEmail(request.Email))
            throw new ArgumentException(ErrorMessages.User.EmailInvalid);

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException(ErrorMessages.User.PasswordRequired);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null)
            throw new ArgumentException(ErrorMessages.User.InvalidCredentials);

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            throw new ArgumentException(ErrorMessages.User.InvalidCredentials);

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginResult(token, new UserDto(user.Id, user.Username, user.Email, user.CreatedAt));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
