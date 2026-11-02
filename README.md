# Tenis-Ahora-Backend

API de Tenis Ahora en .NET 10 (ASP.NET Core + EF Core con SQLite). Es el port del backend de
prueba en Go: mismas rutas, mismos campos JSON y mismas reglas, así el front
(`Tenis-Ahora-Frontend`) trabaja sin cambios.

## Stack

| Pieza | Elección |
|---|---|
| Runtime | .NET 10 |
| HTTP | ASP.NET Core con controllers, CORS abierto, logs por request |
| Base de datos | SQLite con EF Core (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Auth | BCrypt para contraseñas, JWT HS256 |
| Datos iniciales | `src/TenisAhora.API/seed/seed.json`, el mismo dataset que el Modo Demo del front |

Capas: `Domain` (entidades, reglas propias de cada entidad, excepciones), `Application` (servicios
por módulo con sus DTOs e interfaces), `Infrastructure` (EF Core, repositorios, JWT, seed) y `API`
(controllers, middleware de errores, registro de módulos).

Lo anidado que vive con su registro (participantes, cuadro y fixture, tabla de posiciones, alumnos
con asistencia, disponibilidad de la cancha, título del profesor, ítems de recibos y facturas) se
guarda como JSON en una columna de texto. Los archivos de documentos van en la misma base.

## Puesta en marcha

Requisitos: SDK de .NET 10 y la herramienta `dotnet-ef` para las migraciones.

```sh
# la primera vez (y cada vez que cambia el modelo): generar la migración
dotnet ef migrations add Inicial -p src/TenisAhora.Infrastructure -s src/TenisAhora.API

cd src/TenisAhora.API
dotnet run              # http://localhost:5090/api
```

Al arrancar aplica las migraciones pendientes y, si la base está vacía, carga el seed.

### Variables de entorno (o archivo `.env` en la carpeta desde donde se ejecuta)

| Variable | Default | Uso |
|---|---|---|
| `PORT` | — | Puerto HTTP. Sin definir, usa el de `launchSettings.json` (5090). |
| `DATABASE_PATH` | `data/tenis_ahora.sqlite` | Archivo SQLite; se crea solo. |
| `SEED_PATH` | `seed/seed.json` | Datos iniciales; se cargan solo si la base está vacía. |
| `JWT_SECRET` | valor de desarrollo (`appsettings.json`) | Clave HS256 de los tokens. Tiene que tener al menos 32 caracteres. |
| `FRONTEND_DIST` | `../Tenis-Ahora-Frontend/dist` | Si existe `index.html` ahí, la API sirve el front en `/`. |

Una variable vacía cuenta como no definida. El token dura `Jwt:ExpiracionMinutos` (480 minutos).

### Usuarios de prueba

Todos los usuarios del seed tienen contraseña **`123456`**.

| Email | Rol |
|---|---|
| `admin@tenisahora.com` | Empleado (panel de administración) |
| `socio@tenisahora.com` | Socio |
| `profesor@tenisahora.com` | Profesor (da clases) |
| `entrenador@tenisahora.com`, `diego.paredes@tenisahora.com` | Entrenador (da entrenamientos) |
| cualquier otro de `seed.json` → `usuarios` | Socio |

Un usuario profesional tiene `role: "coach"` y un `coachId` que apunta a su ficha en `profesores`;
el rol del token (Profesor o Entrenador) sale del `type` de esa ficha. Los permisos de cada rol están
en `UsuarioActual` y en los servicios de cada módulo.

`POST /api/admin/reset` vacía la base y recarga el seed (solo rol Empleado; otro rol → 403).

## Deploy (Docker + VPS)

`.github/workflows/deploy.yml` corre en cada push a `main`: construye la imagen con
`ci/Dockerfile`, la copia por SSH al VPS (sin registry) y reemplaza el contenedor
`tenisahora-api` en la red `caddy-net`, sin exponer puertos. La base queda en
`/home/devops/tenisahora-api/data` y la configuración en `/home/devops/tenisahora-api/.env`
(montada en `/app/.env`).

Secrets del repo: `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY` y opcionalmente `VPS_PORT`.

En el VPS:

- Poner un `JWT_SECRET` propio en `/home/devops/tenisahora-api/.env` y reiniciar con
  `docker restart tenisahora-api`.
- Agregar al Caddyfile del Caddy que está en `caddy-net`:

  ```
  tenisahora-api.duckdns.org {
      reverse_proxy tenisahora-api:5090
  }
  ```

La API queda en `https://tenisahora-api.duckdns.org/api`.

## Pasarela de pagos simulada

`POST /api/pagos` autoriza el cobro con tarjeta de débito, tarjeta de crédito o QR de Mercado Pago
(RD11) sin mover dinero: solo acepta tarjetas de prueba (ficticias) y devuelve una transacción
`MP-########` aprobada, o 402 si se rechaza. Cada pago se usa una sola vez (seña, saldo o recibo) y
por el importe exacto. De la tarjeta se guardan solo la marca y los últimos 4 dígitos.

| Tipo | Marca | Número | Código | Vencimiento |
|---|---|---|---|---|
| Crédito | Visa | `4509 9535 6623 3704` | `123` | `11/30` |
| Crédito | Mastercard | `5031 7557 3453 0604` | `123` | `11/30` |
| Crédito | American Express | `3711 803032 57522` | `1234` | `11/30` |
| Débito | Visa | `4002 7686 9439 5619` | `123` | `11/30` |
| Débito | Mastercard | `5287 3383 1025 3304` | `123` | `11/30` |

El nombre del titular define el resultado, igual que en el entorno de pruebas de Mercado Pago:
`APRO` (o cualquier otro nombre) aprueba; `FUND` (saldo insuficiente), `SECU` (código de seguridad),
`EXPI` (vencimiento), `CALL`, `FORM` y `OTHE` rechazan; `CONT` queda pendiente y no confirma la operación.

## Endpoints

Todas las rutas van bajo `/api`. Salvo `health`, `auth/login` y `auth/registrar`, exigen
`Authorization: Bearer <token>`. Los errores vuelven como `{ "error": "mensaje" }` con
400 / 401 / 402 / 403 / 404 / 405 / 409 / 500.

### Públicos

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/health` | Estado, versión, versión de SQLite y conteo por colección. |
| POST | `/auth/login` | `{ email, password }` → `AuthResponseDto` con `token`. |
| POST | `/auth/registrar` | `{ nombre, apellido, email, password, direccion?, numeroTelefono?, dni? }` → 201. |

### CRUD por colección (`{col}`)

Colecciones: `usuarios`, `canchas`, `tipos-cancha`, `stock`, `reservas`, `torneos`, `ligas`,
`tipos-competencia`, `profesores`, `clases`, `clases-particulares`, `recibos`, `facturas`,
`reembolsos`, `descuentos`.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/{col}` | Lista completa (orden de inserción). |
| GET | `/{col}/{id}` | Un registro. |
| POST | `/{col}` | Alta. Genera `id` si falta y aplica los defaults/validaciones de la entidad. 201. |
| PUT | `/{col}/{id}` | Modificación por mezcla superficial (`{ ...actual, ...body }`), `id` inmutable. |
| DELETE | `/{col}/{id}` | Baja. `tipos-cancha` en uso → 409; `descuentos` se quita de los usuarios. |

Reglas de alta/modificación destacadas: `usuarios` (email único, `password` → `passwordHash`,
hash nunca se devuelve), `tipos-cancha` (nombre único, cupo ≤ 30), `stock` (recalcula disponible y
estado `disponible` / `agotado`; el total no baja de lo prestado en canchas), `clases` (cupo ≤ 30, RD07),
`clases-particulares` (sin dos turnos a la misma hora para el mismo alumno o profesional), `descuentos`
(código único en mayúsculas), `facturas` (calcula el total de los ítems), `reembolsos` (total/parcial),
`recibos` (exige `transactionId` de un pago aprobado y toma de él el medio de pago), `reservas` (PUT
solo con la reserva pendiente o confirmada; revalida horario, cancha y jugadores y recalcula fin, costo
y saldo; el estado y los cobros solo cambian por los endpoints de negocio).

### Reservas

Estados: `pendiente_pago` (registrada, sin seña) → `confirmada` (seña del 50 % abonada, RD01) →
`en_juego` (alquiler iniciado: la red, el tubo de pelotas y las raquetas salen del stock) →
`finalizada` (saldo abonado: el equipamiento vuelve al stock). Se cancela mientras está pendiente
(sin cargo) o confirmada (RD02-RD04). El costo sale de la tarifa de la cancha, no del cuerpo.

Validaciones del alta: hasta 2 horas, fecha y hora no pasadas, antelación máxima (RD05), 2 jugadores
(singles) o 4 (dobles) con nombre y DNI sin repetir, cancha sin mantenimiento ni bloqueos (RD06), sin
superposición con otra reserva de la cancha o de alguno de los jugadores, y stock de red, pelotas y
raquetas (TC_001).

### Negocio

| Método | Ruta | Cuerpo | Respuesta |
|---|---|---|---|
| GET | `/auth/yo` | — | claims del token |
| GET | `/socios` | — | padrón reducido `[{ id, name, dni, memberNumber }]` (cualquier usuario logueado) |
| GET | `/reservas/ocupacion` | — | turnos tomados de las reservas activas, sin datos de quién reservó (socios y personal) |
| POST | `/documentos` | `{ name, data }` (base64) | `{ id, name, mimeType, size }` (solo Empleado; PDF, JPG o PNG según el contenido, máx. 5 MB) |
| GET | `/documentos/{id}` | — | el archivo (no JSON), con su `Content-Type` |
| GET | `/configuracion` | — | `{ id, freeCancellationHours, lateCancellationRetentionPercent, maxAdvanceBookingDays, privateLessonHourlyRate }` (valores fijados por el club: 6 hs / 50 % / 30 días / $6000) |
| POST | `/admin/reset` | — | `{ success, documents }` (solo rol Empleado, 403 si no) |
| PATCH | `/canchas/{id}/estado` | `{ status, maintenanceNotes }` | cancha |
| PUT | `/canchas/{id}/disponibilidad` | `{ days, openTime, closeTime, slotMinutes, blockedRanges, notes }` | cancha |
| PATCH | `/stock/{id}/ajuste` | `{ totalStock }` | ítem |
| POST | `/pagos` | `{ paymentMethod, amount, concept, card?: { number, holder, expiry, cvv } }` | transacción aprobada (201) o rechazo (402) |
| POST | `/reservas` | `{ courtId, date, startTime, durationHours, playersType, participants }` | `{ data }` reserva pendiente de pago con `depositAmount` (ver validaciones en *Reservas*) |
| POST | `/reservas/{id}/pagar-sena` | `{ transactionId }` | `{ data, receipt }` reserva confirmada + recibo de seña (titular o personal) |
| POST | `/reservas/{id}/iniciar` | — | reserva en juego; descuenta el equipamiento del stock (409 sin stock o sin seña). Solo personal |
| POST | `/reservas/{id}/pagar-saldo` | `{ transactionId }` | `{ data, receipt }` reserva finalizada + recibo final; devuelve el equipamiento. Solo personal |
| POST | `/reservas/{id}/cancelar` | — | `{ data, isWithinFreeWindow, refund, penaltyReceipt }` (RD02–RD04; las horas se calculan con el inicio de la reserva y el plazo y la retención salen de `/configuracion`; pendiente de pago: sin reembolso ni cargo) |
| PUT | `/torneos/{id}/reglamento` · `/ligas/{id}/reglamento` | `{ internalRules?, internalRulesDocument? }` (`{ id }` de un documento; `null` lo quita) | competencia |
| POST | `/torneos/{id}/inscripciones` · `/ligas/{id}/inscripciones` | `{ name, dni, phone, partnerName, partnerDni }` | `{ success, participant }` (409 si no hay cupo o el DNI ya está inscripto) |
| DELETE | `/torneos/{id}/inscripciones/{pid}` · `/ligas/{id}/inscripciones/{pid}` | — | `{ success }` |
| POST | `/torneos/{id}/enfrentamientos` · `/ligas/{id}/enfrentamientos` | `{ round, p1, p2, court, date }` | `{ success, match }` |
| PUT | `/torneos/{id}/enfrentamientos/{mid}` · `/ligas/{id}/enfrentamientos/{mid}` | campos a modificar | `{ success, match }` |
| POST | `/ligas/{id}/enfrentamientos/{mid}/resultado` | `{ score, winner }` | `{ success, match, standings }` (3 pts por victoria, no duplica si ya estaba finalizado) |
| PUT | `/profesores/{id}/disponibilidad` | `{ availableHours }` | profesor |
| PUT | `/profesores/{id}/certificacion` | campos de `licenseDetails` (`document: { id }` para el título) | profesor (400 si el título queda incompleto, RD08) |
| POST | `/clases/{id}/alumnos` | `{ name, dni, phone }` | `{ success, student }` (409 si el cupo está completo, RD10, o el alumno ya está inscripto) |
| DELETE | `/clases/{id}/alumnos/{sid}` | — | `{ success }` |
| POST | `/clases/{id}/asistencia/alternar` | `{ studentId, sessionIndex }` | `{ success, classes }` (P → A → P) |
| POST | `/clases/{id}/asistencia/sesion` | `{ sessionIndex, status }` | `{ success, classes }` (`status` P o A) |
| POST | `/clases/{id}/asistencia/registrar` | `{ studentId, status }` | `{ success }` (`status` P o A) |
| POST | `/clases-particulares/{id}/cancelar` | — | `{ success, data }` |
| PUT | `/usuarios/{id}/descuentos` | `{ activeDiscounts: [códigos] }` | usuario |

## Pruebas

```sh
# con la API corriendo
python3 tests/smoke.py                      # http://localhost:5090/api
python3 tests/smoke.py http://otra-ip:5090/api
```

El script recorre autenticación, CRUD, reglas de dominio, pagos con tarjetas de prueba y los
endpoints de negocio (incluidos los casos TC_001 a TC_005 del TP) y deja la base reseteada al
terminar. La cancelación fuera de plazo se prueba solo si hay un turno dentro de las próximas 6 hs
en el horario de la cancha.
