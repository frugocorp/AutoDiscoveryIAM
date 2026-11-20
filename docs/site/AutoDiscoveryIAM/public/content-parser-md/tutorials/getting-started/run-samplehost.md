---
[slug]: run-samplehost
[title]: Correr el SampleHost
[description]: Cómo ejecutar el host de ejemplo para explorar la API completa desde Swagger UI o Scalar.
[keywords]: SampleHost, Swagger UI, Scalar, dotnet run
[readme_flavors]: github, npmjs
---

# Correr el SampleHost

Restaura y compila:

````code-block
```bash
dotnet build
```
````

Aplica las migraciones (requiere una cadena de conexión válida en `appsettings.json`/`.env`):

````code-block
```bash
dotnet ef database update --project samples/AutoDiscoveryIAM.SampleHost --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
```
````

Corre la API de ejemplo (Swagger UI en la raíz, Scalar en `/scalar`):

````code-block
```bash
dotnet run --project samples/AutoDiscoveryIAM.SampleHost
```
````

Accede vía Swagger UI (raíz, `https://localhost:7001/`) o Scalar (`/scalar`) — ambas viven en `samples/AutoDiscoveryIAM.SampleHost`, no en la librería. Login vía API con las [credenciales de desarrollo por defecto](/references/development/dev-credentials):

````card-code-block
@description
Todas las respuestas de la API (login incluido) van envueltas en el sobre `ApiResponse<T>` -- `success`/`data`/`error`/`errorInfo`.

@command bash
curl -X POST "https://localhost:7001/auth/login" \
     -H "Content-Type: application/json" \
     -d '{
       "email": "admin@example.com",
       "password": "AdminPass123!"
     }'

@result<Resultado esperado> json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-08-02T15:30:00Z",
    "refreshToken": "8f14e45f-ceea-467e-9e1e-c9d3a3f2b1a0",
    "requiresTwoFactor": false,
    "twoFactorChallengeToken": null
  },
  "error": null,
  "errorInfo": null
}

@note<Nota>
Si el usuario tiene 2FA activo, `requiresTwoFactor` sale `true` y `token`/`expiresAt`/`refreshToken` salen `null` hasta verificar el segundo factor vía `POST /auth/2fa/verify` con el `twoFactorChallengeToken` recibido aquí. Con credenciales incorrectas, `success` sale `false` y `data` sale `null` -- el detalle del error va en `error`/`errorInfo`.
````

Para reiniciar la base de datos local desde cero:

````card-code-block
@description
Elimina la base de datos por completo (todas las tablas, todos los datos).

@command bash
dotnet ef database drop --project samples/AutoDiscoveryIAM.SampleHost --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext --force

@note<Nota>
`--force` omite la confirmación interactiva -- este comando es destructivo e irreversible, úsalo solo contra tu base de datos de desarrollo local, nunca contra una compartida o de producción.
````

Vuelve a aplicar las migraciones desde cero:

````code-block
```bash
dotnet ef database update --project samples/AutoDiscoveryIAM.SampleHost --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
```
````
