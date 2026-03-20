namespace WEB_UI.Models.Dtos;

public record RegistroFincaDto(
    decimal Hectareas,
    decimal Vegetacion,
    decimal Hidrologia,
    decimal Topografia,
    bool EsNacional,
    decimal Lat,
    decimal Lng);

public record EditarFincaDto(
    decimal Hectareas,
    decimal Vegetacion,
    decimal Hidrologia,
    decimal Topografia,
    bool EsNacional,
    decimal Lat,
    decimal Lng,
    string? Observaciones);

public record RegistrarIbanDto(
    string Banco,
    string TipoCuenta,
    string Titular,
    string Iban);

public record DictamenDto(string Tipo, string? Observaciones);

public record TomarFincaDto(string RowVersion);
