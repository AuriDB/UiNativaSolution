namespace WEB_UI.Models.Dtos;

public record CrearIngenieroDto(
    string Nombre,
    string Apellido1,
    string Apellido2,
    string Cedula,
    string Correo,
    string Contrasena);

public record EditarUsuarioDto(
    string Nombre,
    string Apellido1,
    string Apellido2);

public record CrearAdminDto(
    string Nombre,
    string Apellido1,
    string Apellido2,
    string Cedula,
    string Correo,
    string Contrasena);

public record CrearParametrosDto(
    decimal PrecioBase,
    decimal PctVegetacion,
    decimal PctHidrologia,
    decimal PctNacional,
    decimal PctTopografia,
    decimal Tope,
    string Opcion);   // "A" | "B"
