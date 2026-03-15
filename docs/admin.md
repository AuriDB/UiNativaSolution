# Sistema Nativa — Módulo Admin (P7)

---

## CU05 — Crear Ingeniero

**Endpoint:** `POST /Admin/Usuarios/Crear`
**Campos:** nombre, apellido1, apellido2, cedula, correo, contraseña temporal
**Rol asignado:** `Ingeniero`
**Estado inicial:** `Activo` (sin OTP, Admin los activa directamente)
**Email N?:** No definido en spec — Admin informa manualmente

---

## CU06 — Editar Usuario

**Endpoint:** `POST /Admin/Usuarios/Editar/{id}`
**Campos editables:** nombre, apellido1, apellido2 (NO cédula, NO correo)
**Permite:** Reactiva usuario Bloqueado → Estado=Activo

---

## CU07 — Inactivar Usuario

**Endpoint:** `POST /Admin/Usuarios/Inactivar/{id}`
**Cascade si es Ingeniero:**
1. Buscar fincas `WHERE IdIngeniero = id AND Estado = EnRevision`
2. Cambiarlas a `Estado = Pendiente, IdIngeniero = null` (regresan a FIFO)
**Email N13:** al usuario inactivado

---

## CU27/28 — Parámetros de Pago

**Regla fundamental:** NUNCA `UPDATE ParametrosPago`. Solo `INSERT` nueva fila.

### Opción A — Solo fincas nuevas
```
INSERT ParametrosPago(Vigente=true, ...)
UPDATE ParametrosPago SET Vigente=false WHERE Id != nuevoId
// Planes activos existentes: intocables
```

### Opción B — Afecta pagos Pendientes existentes
```
// Igual que Opción A, más:
FOR EACH PlanPago activo:
    nuevoMonto = CalculadoraService.Calcular(finca, nuevosParametros)
    UPDATE PagosMensuales SET Monto=nuevoMonto WHERE IdPlan=plan.Id AND Estado=Pendiente
    // Estado=Ejecutado: NUNCA tocar
    Email N14 al Dueño (nuevo monto mensual)
```

---

## Vistas Admin

| Vista | Ruta | Descripción |
|---|---|---|
| `Admin/Usuarios.cshtml` | `/Admin/Usuarios` | Ag-Grid CRUD completo |
| `Admin/Parametros.cshtml` | `/Admin/Parametros` | Form Opción A/B |

---

## Seguridad

- `[Authorize(Roles = "Admin")]` en todo `AdminController`
- Admin NO puede inactivarse a sí mismo
- Admin NO puede cambiar roles (solo crear con Rol=Ingeniero)
