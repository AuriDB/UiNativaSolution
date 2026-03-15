namespace WEB_UI.Services;

public class OtpService
{
    private static readonly Random _rng = new();

    public string GenerarOtp()
    {
        return _rng.Next(100_000, 999_999).ToString();
    }

    public string HashOtp(string otp)
    {
        return BCrypt.Net.BCrypt.HashPassword(otp, workFactor: 12);
    }

    public bool VerificarOtp(string otp, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(otp, hash); }
        catch { return false; }
    }
}
