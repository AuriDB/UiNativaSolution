namespace WEB_UI.Models.Dtos;

public record LoginDto(string Correo, string Contrasena);

public record RegisterDto(
    string Nombre,
    string Apellido1,
    string Apellido2,
    string Cedula,
    string Correo,
    string Contrasena,
    string ConfirmarContrasena);

public record VerifyOtpDto(string Correo, string Otp);

public record ResendOtpDto(string Correo);

public record ForgotPasswordDto(string Correo);

public record ResetPasswordDto(string Token, string NuevaContrasena);
