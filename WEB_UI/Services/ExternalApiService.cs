using System.Text.Json;

namespace WEB_UI.Services;

public class ExternalApiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;
    private readonly ILogger<ExternalApiService> _logger;

    public ExternalApiService(HttpClient http, IConfiguration cfg, ILogger<ExternalApiService> logger)
    {
        _http   = http;
        _cfg    = cfg;
        _logger = logger;
    }

    public record ClimaData(double? Temperatura, double? Presion, string? Descripcion);
    public record ElevacionData(double? MetrosSobreNivelMar);

    /// <summary>Llama OpenWeather y OpenElevation en paralelo (Task.WhenAll).</summary>
    public async Task<(ClimaData clima, ElevacionData elevacion)> ObtenerDatosAsync(
        decimal lat, decimal lng)
    {
        var climaTask     = ObtenerClimaAsync((double)lat, (double)lng);
        var elevacionTask = ObtenerElevacionAsync((double)lat, (double)lng);

        await Task.WhenAll(climaTask, elevacionTask);
        return (climaTask.Result, elevacionTask.Result);
    }

    private async Task<ClimaData> ObtenerClimaAsync(double lat, double lng)
    {
        var apiKey  = _cfg["ExternalApis:OpenWeatherApiKey"] ?? "TU_KEY";
        var baseUrl = _cfg["ExternalApis:OpenWeatherBase"]   ?? "https://api.openweathermap.org/data/2.5";

        if (apiKey == "TU_KEY")
            return new ClimaData(null, null, "No disponible (clave API no configurada)");

        try
        {
            var url  = $"{baseUrl}/weather?lat={lat}&lon={lng}&appid={apiKey}&units=metric&lang=es";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var temp = root.GetProperty("main").GetProperty("temp").GetDouble();
            var pres = root.GetProperty("main").GetProperty("pressure").GetDouble();
            var desc = root.GetProperty("weather")[0].GetProperty("description").GetString();
            return new ClimaData(temp, pres, desc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenWeather falló para lat={Lat} lng={Lng}", lat, lng);
            return new ClimaData(null, null, "No disponible");
        }
    }

    private async Task<ElevacionData> ObtenerElevacionAsync(double lat, double lng)
    {
        var baseUrl = _cfg["ExternalApis:OpenElevationBase"] ?? "https://api.open-elevation.com/api/v1";
        try
        {
            var url  = $"{baseUrl}/lookup?locations={lat},{lng}";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc  = JsonDocument.Parse(json);
            var elev = doc.RootElement
                          .GetProperty("results")[0]
                          .GetProperty("elevation")
                          .GetDouble();
            return new ElevacionData(elev);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenElevation falló para lat={Lat} lng={Lng}", lat, lng);
            return new ElevacionData(null);
        }
    }
}
