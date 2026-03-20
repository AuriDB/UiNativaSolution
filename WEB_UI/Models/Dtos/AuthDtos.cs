// ============================================================
// AuthDtos.cs — DTOs para el flujo de autenticación
// Estos records se usan como cuerpo (body) de las llamadas
// POST que WEB_UI hace a la API externa para autenticar,
// registrar y recuperar contraseñas de usuarios.
// Son inmutables (record) y no tienen lógica de negocio.
// ============================================================

namespace WEB_UI.Models.Dtos;

// DTO para POST /api/auth/login
// Envía las credenciales del usuario para iniciar sesión.
// La API valida el correo, verifica el hash BCrypt y
// retorna una cookie de sesión o JWT si son correctas.
public record LoginDto(string Correo, string Contrasena);

// DTO para POST /api/auth/register
// Envía todos los datos necesarios para crear una nueva cuenta de Dueño.
// La API valida que la cédula y correo sean únicos, crea el Sujeto
// y envía el OTP al correo para verificarlo.
public record RegisterDto(
    string Nombre,
    string Apellido1,
    string Apellido2,
    string Cedula,
    string Correo,
    string Contrasena,
    string ConfirmarContrasena);

// DTO para POST /api/auth/verify-otp
// Envía el correo y el código OTP de 6 dígitos que el usuario recibió.
// La API verifica el hash del OTP, su vigencia y el número de intentos.
public record VerifyOtpDto(string Correo, string Otp);

// DTO para POST /api/auth/resend-otp
// Solicita un nuevo envío del código OTP al correo indicado.
// La API verifica el cooldown (tiempo mínimo entre reenvíos) y el
// límite máximo de reenvíos antes de generar y enviar uno nuevo.
public record ResendOtpDto(string Correo);

// DTO para POST /api/auth/forgot-password
// Inicia el flujo de recuperación de contraseña.
// La API busca el Sujeto por correo y envía un token de reset por email.
public record ForgotPasswordDto(string Correo);

// DTO para POST /api/auth/reset-password
// Completa el cambio de contraseña usando el token recibido por correo.
// La API valida que el token exista, no haya expirado, y actualiza
// el PasswordHash del Sujeto con el nuevo valor hasheado.
public record ResetPasswordDto(string Token, string NuevaContrasena);
