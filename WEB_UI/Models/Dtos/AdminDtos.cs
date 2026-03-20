// ============================================================
// AdminDtos.cs — DTOs para las operaciones del módulo Admin
// Se usan como cuerpo (body) de las llamadas que WEB_UI hace
// a la API externa para gestionar usuarios y parámetros de pago.
// ============================================================

namespace WEB_UI.Models.Dtos;

// DTO para POST /api/admin/users/engineer
// Datos para crear una nueva cuenta de Ingeniero.
// La API crea el Sujeto con Rol = Ingeniero y Estado = Activo.
// A diferencia del registro de Dueño, el ingeniero no pasa por OTP;
// el admin le asigna directamente la contraseña inicial.
public record CrearIngenieroDto(
    string Nombre,
    string Apellido1,
    string Apellido2,
    string Cedula,
    string Correo,
    string Contrasena);

// DTO para PUT /api/admin/users/{id}
// Actualiza los datos personales de cualquier usuario del sistema.
// Solo se permite editar nombre y apellidos; no correo ni cédula.
public record EditarUsuarioDto(
    string Nombre,
    string Apellido1,
    string Apellido2);

// DTO para POST /api/admin/users/admin
// Datos para crear una nueva cuenta de Administrador.
// Mismo flujo que CrearIngenieroDto pero el Sujeto creado tendrá Rol = Admin.
// Solo los administradores existentes pueden crear otros administradores.
public record CrearAdminDto(
    string Nombre,
    string Apellido1,
    string Apellido2,
    string Cedula,
    string Correo,
    string Contrasena);

// DTO para POST /api/admin/payment-settings
// Crea un nuevo conjunto de parámetros de cálculo PSA.
// Al crearse, los parámetros anteriores quedan con Vigente = false.
// Opcion define si el cálculo usa la fórmula A o B (según el spec).
public record CrearParametrosDto(
    decimal PrecioBase,
    decimal PctVegetacion,
    decimal PctHidrologia,
    decimal PctNacional,
    decimal PctTopografia,
    decimal Tope,
    string  Opcion);
