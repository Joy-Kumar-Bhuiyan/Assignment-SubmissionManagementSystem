namespace BackendApi.Services;

public class PasswordService
{
    private const int WorkFactor = 12;
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(
        "This-is-only-a-login-timing-sentinel",
        workFactor: WorkFactor);

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    public string HashPassword(string password)
    {
        return Hash(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public bool VerifyAgainstDummyHash(string password) => Verify(password, DummyHash);

    public bool NeedsRehash(string passwordHash) => BCrypt.Net.BCrypt.PasswordNeedsRehash(passwordHash, WorkFactor);
}
