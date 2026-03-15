# Sistema Nativa — Autenticación y Autorización (P2)

---

## Flujo de Registro (CU01)

```
[UI 3-pasos] → POST /Auth/Register → AuthService.RegistrarAsync
    ↓ Validar contraseña (JS + C#)
    ↓ Normalizar cédula (1-XXXX-XXXX)
    ↓ INSERT Sujeto (Estado=Inactivo, Rol=Dueno)
    ↓ OtpService.GenerarAsync → BCrypt → INSERT OtpSesion (TTL 90s)
    ↓ EmailService.EnviarOtpAsync (N03-like)
    ↓ return { success: true } → JS redirect /Auth/VerificarOtp
```

## Flujo OTP (CU01 — parte 2)

```
POST /Auth/VerifyOtp → AuthService.VerificarOtpAsync
    ↓ Buscar OtpSesion activa (Usada=false, Intentos<3)
    ↓ Verificar TTL (Expiracion > UtcNow)
    ↓ BCrypt.Verify(otp, HashOtp)
    ├── Fallo → Intentos++ → si Intentos≥3: Estado=Bloqueado + email N03
    └── OK    → Usada=true + Sujeto.Estado=Activo + SignIn cookie
```

**Reenvío OTP:**
- Cooldown: `UltimoReenvio + 30s > now` → 429
- Límite: `ConteoReenvios >= 3` en ventana 5 min → bloquear

## Flujo Login (CU02)

```
POST /Auth/Login → AuthService.LoginAsync
    ↓ Buscar Sujeto por correo
    ↓ BCrypt.Verify(password, PasswordHash)
    ↓ Verificar Estado = Activo
    ↓ HttpContext.SignInAsync(claims: [UserId, Name, Email, Role])
    ↓ Redirect según rol: /Home/Index
```

## Recuperación de Contraseña (CU04)

**Generación token:**
```csharp
payload = $"{sujetoId}|{expiry.Ticks}"
hmac    = HMAC-SHA256(payload, HmacSecret)
token   = Base64(payload) + "." + Base64(hmac)
hash    = SHA256(token) → stored in Sujeto.PasswordResetHash
```

**Validación:**
1. Decodificar token → extraer payload + hmac
2. Recomputar HMAC → comparar (timing-safe)
3. Verificar SHA256(token) == PasswordResetHash
4. Verificar `expiry > now`
5. Limpiar hash (un solo uso)

## Roles y Autorización

| Rol | Valor enum | Rutas protegidas |
|---|---|---|
| Dueno | 1 | `/Dueno/*` |
| Ingeniero | 2 | `/Ingeniero/*` |
| Admin | 3 | `/Admin/*` |

Rutas públicas: `/Landing/`, `/Auth/`, `/Home/Error`

## Claims en Cookie

```csharp
ClaimTypes.NameIdentifier → sujetoId (int)
ClaimTypes.Name           → nombre completo
ClaimTypes.Email          → correo
ClaimTypes.Role           → "Dueno" | "Ingeniero" | "Admin"
```
