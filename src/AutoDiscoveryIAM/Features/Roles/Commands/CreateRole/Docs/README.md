# Documentación de CreateRole

Esta carpeta contiene documentación, ejemplos y convenciones para el comando CreateRole, siguiendo el patrón de Posts/CreatePost adaptado al sistema RBAC.

## Estructura

### Request Examples
- `RequestDto/CreateRoleRequestDtoExample.cs`: Ejemplo de petición Swagger

### Response Examples  
- `ResponseDto/CreateRoleResponseExampleSwagger.cs`: Ejemplo de respuesta exitosa

## Características Específicas

- **Validación RBAC**: Usa `RoleValidationRules` con regex y tipos específicos
- **Duplicados**: Verifica que no exista un rol con el mismo nombre (422)
- **Logging**: Integración con `IRoleLogger` especializado
- **Swagger**: Documentación completa con links y ejemplos
