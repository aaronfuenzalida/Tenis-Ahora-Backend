# Tenis Ahora — Backend

Backend del TP **"Tenis Ahora"** (Ingeniería de Software, UNAJ): API REST para la gestión de un complejo de tenis (usuarios, canchas, reservas, clases y stock).

**Stack:** .NET 10 · ASP.NET Core · Entity Framework Core · PostgreSQL (Supabase) · JWT + BCrypt

---

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git
- Postman (opcional, para probar los endpoints)

> No hace falta instalar ninguna base de datos: usamos una instancia PostgreSQL compartida en Supabase.

## Puesta en marcha

### 1. Clonar y compilar

```bash
git clone <url-del-repo>
cd tenis-ahora-backend
dotnet build
```

### 2. Configurar los secretos

Las credenciales (cadena de conexión y clave JWT) **no están en el repo** y nunca deben commitearse. Pedíselas a Aaron por privado y cargalas con `dotnet user-secrets` (se guardan en tu máquina, fuera del proyecto):

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<cadena-de-conexion>" --project src/TenisAhora.API
dotnet user-secrets set "Jwt:Key" "<clave-jwt>" --project src/TenisAhora.API
```

Para verificar que quedaron cargadas:

```bash
dotnet user-secrets list --project src/TenisAhora.API
```

### 3. Ejecutar

```bash
dotnet run --project src/TenisAhora.API
```

La API queda escuchando en `http://localhost:5090`.

### 4. Probar que funciona

Registrar un usuario:

```
POST http://localhost:5090/api/auth/registrar
Content-Type: application/json

{
  "nombre": "Juan",
  "apellido": "Pérez",
  "direccion": "Calle Falsa 123",
  "email": "juan@mail.com",
  "numeroTelefono": "1122334455",
  "password": "MiClave123"
}
```

Iniciar sesión (devuelve el token JWT):

```
POST http://localhost:5090/api/auth/login
Content-Type: application/json

{
  "email": "juan@mail.com",
  "password": "MiClave123"
}
```

Para los endpoints protegidos, mandá el token en el header: `Authorization: Bearer <token>`.

---

## Arquitectura

El proyecto sigue **Clean Architecture**: el código se divide en 4 proyectos (capas) y las dependencias apuntan siempre hacia adentro, hacia el dominio.

```
        ┌─────────────────────────────────┐
        │        TenisAhora.API           │  Controllers, middleware, configuración
        └───────┬───────────────┬─────────┘
                │               │
        ┌───────▼───────┐ ┌─────▼─────────────────┐
        │  Application  │ │    Infrastructure     │  EF Core, repositorios,
        │  (casos de    │◄┤  (detalles técnicos)  │  BCrypt, JWT
        │   uso)        │ └───────────────────────┘
        └───────┬───────┘
                │
        ┌───────▼───────┐
        │    Domain     │  Entidades, enums, excepciones de negocio
        └───────────────┘
```

| Proyecto | Qué contiene | Regla |
|---|---|---|
| `TenisAhora.Domain` | Entidades (`Usuario`), enums (`Rol`), excepciones de negocio | No depende de nada |
| `TenisAhora.Application` | Servicios con la lógica de los casos de uso, DTOs e **interfaces** (puertos) que definen qué necesita del exterior | Solo depende de Domain |
| `TenisAhora.Infrastructure` | **Implementaciones** de esas interfaces: repositorios con EF Core, hasher de contraseñas, generador de JWT | Implementa lo que Application pide |
| `TenisAhora.API` | Controllers (HTTP), middleware de errores, inyección de dependencias y arranque | Conecta todas las piezas |

**La idea central:** `Application` dice *qué* necesita (por ejemplo `IUsuarioRepository`), `Infrastructure` resuelve *cómo* (EF Core + PostgreSQL). Así la lógica de negocio no sabe qué base de datos hay detrás, y se puede testear o cambiar sin tocarla.

### Estructura de un módulo

Cada módulo (Auth, Canchas, etc.) repite el mismo esqueleto. Ejemplo con Auth:

```
Domain/
├── Entities/Usuario.cs
├── Enums/Rol.cs
└── Exceptions/EmailYaRegistradoException.cs

Application/Auth/
├── Dtos/            RegistrarUsuarioDto, LoginDto, AuthResponseDto
├── Interfaces/      IAuthService, IUsuarioRepository, IPasswordHasher, IJwtTokenGenerator
└── Services/        AuthService (la lógica de negocio)

Infrastructure/
├── Auth/            BCryptPasswordHasher, JwtTokenGenerator
└── Persistence/     TenisAhoraDbContext, UsuarioRepository, Configurations/

API/
├── Controllers/     AuthController
├── Extensions/      AuthModuleExtensions (registra el módulo en DI)
└── Middleware/      ManejoErroresMiddleware (excepciones → códigos HTTP)
```

Para agregar un módulo nuevo, copiá este esqueleto con tus propias entidades, DTOs, interfaces y servicios.

### Manejo de errores

Los servicios lanzan excepciones de dominio (por ejemplo `EmailYaRegistradoException`) y el middleware `ManejoErroresMiddleware` las traduce a códigos HTTP (409, 401, etc.). Los controllers solo expresan el camino feliz: nunca uses `try/catch` en un controller.

### Base de datos y migraciones

Usamos EF Core con migraciones. Si cambiás una entidad o su configuración:

```bash
dotnet ef migrations add <NombreDescriptivo> --project src/TenisAhora.Infrastructure --startup-project src/TenisAhora.API
dotnet ef database update --project src/TenisAhora.Infrastructure --startup-project src/TenisAhora.API
```

> ⚠️ La base es compartida por todo el equipo: creá y aplicá migraciones **solo desde `develop`** y avisá al grupo antes de aplicar.

---

## Flujo de trabajo con Git

- **`main`**: rama estable, siempre funciona.
- **`develop`**: rama de integración, acá se junta todo.
- **`feature/<modulo>-<descripcion>`**: una rama por funcionalidad, sale de `develop`.

Pasos para trabajar:

```bash
git switch develop
git pull
git switch -c feature/mi-modulo
# ... trabajar y commitear ...
git push
```

Después abrí un **Pull Request hacia `develop`** en GitHub. Cuando `develop` está probado, se abre un PR de `develop` → `main`.

**Commits:** usamos [Conventional Commits](https://www.conventionalcommits.org/es/):

```
feat(auth): agregar entidad Usuario y enum Rol
fix(canchas): corregir validación de capacidad
docs: actualizar README
chore: agregar paquete Npgsql
```

Cada commit debe compilar por sí solo (`dotnet build` en verde antes de commitear).
