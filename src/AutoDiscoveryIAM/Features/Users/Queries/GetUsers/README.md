# GetUsers Query (Vertical Slice)

Este slice implementa la consulta de usuarios bajo el patrón vertical slice y CQRS para el catálogo administrativo de asignaciones RBAC.

## Archivos principales
- `GetUsersQuery.cs`: Record CQRS para la consulta.
- `GetUsersHandler.cs`: Handler para la consulta GetUsers.
- `GetUsersRepository.cs`: Acceso a datos.
- `GetUsersResponseDto.cs`: DTO de respuesta.
- `GetUsersResponse.cs`: Wrapper de respuesta.

## Convenciones
- Cada archivo/clase inicia con GetUsers.
- No se reutilizan DTOs globales.
- Documentación en este README y carpeta Docs/.

## Ejemplo de respuesta
```json
{
  "success": true,
  "data": [
    {
      "publicId": "550e8400-e29b-41d4-a716-446655440001",
      "username": "admin",
      "email": "admin@example.com",
      "fullName": "Admin User",
      "isActive": true,
      "isAdmin": true,
      "isDevelopment": false,
      "isSuAdmin": false
    }
  ],
  "error": null,
  "errorInfo": null
}
```
