# GetRoles Query (Vertical Slice)

Este slice implementa la consulta de roles bajo el patrón vertical slice y CQRS para el sistema RBAC.

## Archivos principales
- `GetRolesQuery.cs`: Record CQRS para la consulta.
- `GetRolesHandler.cs`: Handler para la consulta GetRoles.
- `GetRolesRepository.cs`: Acceso a datos.
- `GetRolesResponseDto.cs`: DTO de respuesta.
- `GetRolesResponse.cs`: Wrapper de respuesta.

## Convenciones
- Cada archivo/clase inicia con GetRoles.
- No se reutilizan DTOs globales.
- Documentación en este README y carpeta Docs/.

## Ejemplo de respuesta
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Administrador",
      "description": "Rol con permisos completos del sistema",
      "isActive": true,
      "createdAt": "2025-01-01T10:00:00Z",
      "updatedAt": null,
      "deletedAt": null,
      "isDeleted": false
    }
  ],
  "error": null,
  "errorInfo": null
}
```
