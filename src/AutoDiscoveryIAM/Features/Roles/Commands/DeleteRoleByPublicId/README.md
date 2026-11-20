# DeleteRoleByPublicId Slice

## 🎯 Propósito
Slice para eliminar lógicamente un rol existente por su PublicId, siguiendo el patrón CQRS y las convenciones del proyecto.

## 📋 Funcionalidad
- **Endpoint**: `DELETE /roles/{publicId:guid}`
- **Operación**: Eliminación lógica (soft delete) de roles
- **Seguridad**: Solo utiliza PublicId, nunca expone IDs internos
- **Validaciones**: Rol existe, no está eliminado, no tiene usuarios asignados

## 🔧 Componentes

### Core Components
- **DeleteRoleByPublicIdCommand**: Record inmutable con PublicId
- **DeleteRoleByPublicIdHandler**: Lógica de negocio y orquestación
- **DeleteRoleByPublicIdRepository**: Acceso a datos y validaciones
- **DeleteRoleByPublicIdEndPoint**: Configuración del endpoint REST

### DTOs y Responses
- **DeleteRoleByPublicIdRequestDto**: Solo PublicId desde ruta ([FromRoute])
- **DeleteRoleByPublicIdResponseDto**: PublicId y status de eliminación
- **DeleteRoleByPublicIdResponse**: Wrapper de respuesta con ApiResponse

### Validation & DI
- **DeleteRoleByPublicIdCommandValidator**: Validación de PublicId requerido
- **DeleteRoleByPublicIdDI**: Registro de dependencias

### Documentation
- **Docs/RequestDto/**: Ejemplos de request para Swagger
- **Docs/ResponseDto/**: Ejemplos de response para Swagger

## 🛡️ Validaciones de Negocio

### Pre-validaciones
1. **PublicId requerido**: Validación con FluentValidation
2. **Formato GUID**: Automático por ruta `{publicId:guid}`

### Validaciones de Handler
1. **Rol existe**: Debe existir en la base de datos
2. **No eliminado**: El rol no debe estar ya eliminado
3. **Sin usuarios**: No debe tener usuarios asignados activos

## 🔄 Flujo de Operación

```
1. Request → [FromRoute] PublicId
2. Validator → PublicId required
3. Repository → GetRoleByPublicIdAsync()
4. Validations → Exists + Not deleted + No users
5. Repository → DeleteRoleAsync() [Soft delete]
6. AutoDiscoveryIAMUnitOfWork → SaveChangesAsync()
7. Response → { PublicId, Status: "Eliminado" }
```

## 🚫 Errores Posibles
- **400 Bad Request**: Validación falla (PublicId requerido)
- **401 Unauthorized**: Sin autenticación
- **404 Not Found**: Rol no encontrado
- **409 Conflict**: Rol tiene usuarios asignados
- **422 Unprocessable Entity**: Rol ya eliminado

## 🔗 Integración

### Dependency Injection
Registrado en `RolesDI.AddDeleteRoleByPublicIdDI()`

### Endpoint Mapping
Mapeado en `RolesEndPointMapping.MapDeleteRoleByPublicIdEndpoint()`

### RBAC Permission
Sistema de reflection detectará automáticamente:
```
roles.delete_role_by_public_id
```

## 📝 Documentación Swagger
- Ejemplos completos de request/response
- Códigos de error documentados
- Links a endpoints relacionados
- OpenAPI metadata avanzada

---

**🎯 Slice completamente implementado siguiendo convenciones del proyecto y patrón establecido por UpdateRoleByPublicId y Posts.**