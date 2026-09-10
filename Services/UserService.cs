public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Guid> RegisterAsync(
        string fullName,
        string email,
        string password,
        string role)
    {
        var passwordHash =
            BCrypt.Net.BCrypt.HashPassword(password);

        return await _userRepository.CreateUserAsync(
            fullName,
            email,
            passwordHash,
            role);
    }

    public async Task<User?> LoginAsync(
    string email,
    string password)
{
    var user =
        await _userRepository.GetUserByEmailAsync(email);

    if (user is null)
        return null;

    var passwordValid =
        BCrypt.Net.BCrypt.Verify(
            password,
            user.PasswordHash);

    if (!passwordValid)
        return null;

    return user;
}

}