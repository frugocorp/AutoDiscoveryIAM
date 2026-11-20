# Documentación de GetRoles

Esta carpeta contiene documentación, ejemplos y convenciones para la query GetRoles, siguiendo el patrón de Posts/GetPosts adaptado al sistema RBAC.

## Estructura

### Response Examples  
- `ResponseDto/GetRolesResponseExampleSwagger.cs`: Ejemplo de respuesta exitosa con múltiples roles

## Características Específicas

- **Consulta RBAC**: Retorna todos los roles del sistema con información completa
- **Ordenamiento**: Por fecha de creación descendente (más recientes primero)
- **Logging**: Integración con `IRoleLogger` para trazabilidad
- **Swagger**: Documentación completa con ejemplos realistas
- **Auditoría**: Incluye campos de auditoría (CreatedAt, UpdatedAt, DeletedAt)
