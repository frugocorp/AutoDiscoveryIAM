# Documentación de Login (Command)

Incluye ejemplos y documentación para el comando Login.

## Estructura
- LoginCommand.cs: Record para los datos de inicio de sesión.
- LoginHandler.cs: Lógica de autenticación y generación de token.
- LoginResponseDto.cs: DTO de respuesta con token, expiración y refresh token.
- LoginResponse.cs: Wrapper CQRS para la respuesta de login.
- LoginEndPoint.cs: Endpoint Minimal API para login.
- Docs/: Ejemplo Swagger y documentación.

## Ejemplo de respuesta
```json
{
  "success": true,
  "data": {
    "data": {
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "expiresAt": "2025-05-23T22:00:00Z",
      "refreshToken": "refresh-token-ejemplo-1234567890"
    }
  },
  "error": null,
  "errorInfo": null
}
```
