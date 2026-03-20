<<<<<<< HEAD
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WEB_UI.Models.Dtos;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin) => _admin = admin;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Dashboard ────────────────────────────────────────────────────────────
    [HttpGet("Admin/Dashboard")]
    public async Task<IActionResult> Dashboard()
        => Json(await _admin.GetDashboardAdminAsync());

    // ── CU08 Ver Usuarios ────────────────────────────────────────────────────
    [Route("Admin/Usuarios")]
    public IActionResult Usuarios() => View("~/Views/Admin/Usuarios.cshtml");

    [HttpGet("Admin/Usuarios/Data")]
    public async Task<IActionResult> UsuariosData()
    {
        var data = await _admin.ListarUsuariosAsync();
        return Json(data);
    }

    // ── CU05 Crear Ingeniero ─────────────────────────────────────────────────
    [HttpPost("Admin/Usuarios/Crear")]
    public async Task<IActionResult> CrearIngeniero([FromBody] CrearIngenieroDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _admin.CrearIngenieroAsync(
            dto.Nombre, dto.Apellido1, dto.Apellido2,
            dto.Cedula, dto.Correo, dto.Contrasena);

        return Json(new { success = ok, message = mensaje });
    }

    // ── Crear Admin ──────────────────────────────────────────────────────────
    [HttpPost("Admin/Usuarios/CrearAdmin")]
    public async Task<IActionResult> CrearAdmin([FromBody] CrearAdminDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _admin.CrearAdminAsync(
            dto.Nombre, dto.Apellido1, dto.Apellido2,
            dto.Cedula, dto.Correo, dto.Contrasena);

        return Json(new { success = ok, message = mensaje });
    }

    // ── CU06 Editar Usuario ──────────────────────────────────────────────────
    [HttpPost("Admin/Usuarios/Editar/{id:int}")]
    public async Task<IActionResult> EditarUsuario(int id, [FromBody] EditarUsuarioDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _admin.EditarUsuarioAsync(id, dto.Nombre, dto.Apellido1, dto.Apellido2);
        return Json(new { success = ok, message = mensaje });
    }

    // ── Reactivar Usuario ────────────────────────────────────────────────────
    [HttpPost("Admin/Usuarios/Reactivar/{id:int}")]
    public async Task<IActionResult> ReactivarUsuario(int id)
    {
        var (ok, mensaje) = await _admin.ReactivarAsync(id);
        return Json(new { success = ok, message = mensaje });
    }

    // ── CU07 Inactivar Usuario ────────────────────────────────────────────────
    [HttpPost("Admin/Usuarios/Inactivar/{id:int}")]
    public async Task<IActionResult> InactivarUsuario(int id)
    {
        var (ok, mensaje) = await _admin.InactivarAsync(id, UserId);
        return Json(new { success = ok, message = mensaje });
    }

    // ── CU28/27 Parámetros ────────────────────────────────────────────────────
    [Route("Admin/Parametros")]
    public async Task<IActionResult> Parametros()
    {
        ViewBag.Vigente = await _admin.GetVigenteAsync();
        return View("~/Views/Admin/Parametros.cshtml");
    }

    [HttpGet("Admin/Parametros/Data")]
    public async Task<IActionResult> ParametrosData()
    {
        var data = await _admin.ListarParametrosAsync();
        return Json(data);
    }

    [HttpPost("Admin/Parametros")]
    public async Task<IActionResult> CrearParametros([FromBody] CrearParametrosDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _admin.CrearParametrosAsync(
            dto.PrecioBase, dto.PctVegetacion, dto.PctHidrologia,
            dto.PctNacional, dto.PctTopografia, dto.Tope,
            dto.Opcion, UserId);

        return Json(new { success = ok, message = mensaje });
    }
}
=======
﻿using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class AdminController : Controller
    {
        private IActionResult? CheckSession()
        {
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            //    return RedirectToAction("Index", "Login");
            //if (HttpContext.Session.GetString("UserRole") != "Admin")
            //    return RedirectToAction("Index", "Home");
            return null;
        }

        public IActionResult Dashboard()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Users()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult UserDetail(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.UserId = id;
            return View();
        }

        public IActionResult Properties()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult PropertyDetail(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.FincaId = id;
            return View();
        }

        public IActionResult PaymentSettings()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult AuditLog()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Reports()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Profile()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }
    }
}
>>>>>>> 8938498ba942204ca8456128102b364380d3999e
