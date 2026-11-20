# RestoreRoleByPublicId - RBAC Role Restoration Slice

## Descripción General
Slice completo para la restauración de roles eliminados lógicamente en el sistema RBAC. Permite reactivar roles que fueron previamente eliminados mediante soft delete.

## Funcionalidades Principales

### ✅ Restauración de Roles
- Busca roles eliminados por PublicId
- Valida que el rol exista y esté efectivamente eliminado
- Verifica reglas de negocio para evitar conflictos

### ✅ Validaciones de Negocio
- **Existencia**: El rol debe existir y estar eliminado
- **Conflicto de nombres**: No debe existir otro rol activo con el mismo nombre
- **Integridad de datos**: Se verifican las reglas de dominio

### ✅ Auditoría Completa
- Registra quién restauró el rol (`RestoredBy`)
- Marca el timestamp de restauración (`RestoredAt`)
- Actualiza campos de soft delete (`IsDeleted = false`)
- Limpia campos de eliminación (`DeletedAt = null`, `DeletedBy = null`)

## Componentes del Slice

### Core Components
- **`RestoreRoleByPublicIdCommand`**: Comando con PublicId del rol
- **`RestoreRoleByPublicIdHandler`**: Lógica de negocio principal
- **`RestoreRoleByPublicIdRepository`**: Acceso a datos específico
- **`RestoreRoleByPublicIdValidator`**: Validaciones FluentValidation

### API Layer
- **`RestoreRoleByPublicIdEndPoint`**: Endpoint REST POST /roles/{publicId}/restore
- **`RestoreRoleByPublicIdRequestDto`**: DTO de entrada
- **`RestoreRoleByPublicIdResponse`**: Respuesta interna del handler
- **`RestoreRoleByPublicIdResponseDto`**: DTO de salida para API

### Infrastructure
- **`RestoreRoleByPublicIdDI`**: Configuración de inyección de dependencias
- **Logging**: Integración con IRoleLogger para auditoría
- **AutoDiscoveryIAMUnitOfWork**: Gestión transaccional

## Casos de Uso

### 1. Restauración Exitosa
```
POST /roles/{publicId}/restore
→ 200 OK: Rol restaurado exitosamente
```

### 2. Rol No Encontrado
```
POST /roles/{publicId}/restore
→ 404 Not Found: No existe rol eliminado con ese PublicId
```

### 3. Conflicto de Nombres
```
POST /roles/{publicId}/restore
→ 409 Conflict: Ya existe un rol activo con ese nombre
```

### 4. Validación Fallida
```
POST /roles/{publicId}/restore (con GUID inválido)
→ 400 Bad Request: Formato de PublicId inválido
```

## Flujo de Operación

1. **Validación**: FluentValidation verifica el formato del PublicId
2. **Búsqueda**: Repository busca el rol eliminado usando `IgnoreQueryFilters()`
3. **Verificación de Conflictos**: Valida que no exista rol activo con mismo nombre
4. **Restauración**: Actualiza campos de auditoría y estado del rol
5. **Persistencia**: AutoDiscoveryIAMUnitOfWork guarda los cambios en transacción
6. **Logging**: Registra la operación para auditoría
7. **Respuesta**: Retorna datos del rol restaurado

## Integración con Sistema RBAC

### Permisos
- **Permiso requerido**: `roles.restore_role_by_public_id`
- **Detección automática**: El sistema de reflexión detecta este endpoint
- **Autorización**: RequireAuthorization() en el endpoint

### Entidades
- **Role**: Entidad principal que se restaura
- **AuditableEntity**: Campos de auditoría (`RestoredAt`, `RestoredBy`)
- **Soft Delete**: Manejo de `IsDeleted`, `DeletedAt`, `DeletedBy`

## Documentación Swagger

### Ejemplos Configurados
- **Request**: GUID de ejemplo válido
- **Response Success**: Datos del rol restaurado
- **Response Errors**: Casos de error 404, 409, 400, 500

### Metadata OpenAPI
- Descripciones detalladas de la operación
- Ejemplos de request/response
- Códigos de estado HTTP documentados
- Tags y categorización apropiada

## Logging y Monitoreo

### Eventos Registrados
- `LogRestoreRoleAttempt`: Inicio de operación
- `LogRoleNotFoundByPublicId`: Rol no encontrado
- `LogRestoreRoleNameConflict`: Conflicto de nombres detectado
- `LogRoleRestored`: Restauración exitosa
- `LogRestoreRoleError`: Errores durante la operación

### Información de Auditoría
- PublicId del rol
- Nombre del rol
- Usuario que realiza la operación
- Timestamp de la operación
- Detalles de errores (si los hay)

## Consideraciones de Seguridad

### Autorización
- Endpoint protegido con `RequireAuthorization()`
- Requiere permiso específico del sistema RBAC
- Validación de usuario autenticado

### Validación de Entrada
- PublicId debe ser GUID válido
- Verificación de existencia del rol
- Validación de estado eliminado

### Integridad de Datos
- Transacciones para consistencia
- Verificación de reglas de negocio
- Prevención de conflictos de nombres

## Estado del Desarrollo

### ✅ Completado
- [x] Comando y Handler implementados
- [x] Repository con consultas específicas
- [x] Validaciones FluentValidation
- [x] Endpoint REST configurado
- [x] Documentación Swagger completa
- [x] Logging y auditoría
- [x] Integración con AutoDiscoveryIAMUnitOfWork
- [x] DTOs de request/response
- [x] Inyección de dependencias

### 🔄 Pendiente
- [ ] Integración en RolesDI y RolesEndPointMapping
- [ ] Pruebas unitarias
- [ ] Pruebas de integración
- [ ] Documentación adicional

## Próximos Pasos

1. **Integración**: Agregar al sistema principal de DI y mapping
2. **Testing**: Implementar pruebas unitarias y de integración
3. **Validación**: Ejecutar pruebas completas del flujo
4. **Documentación**: Completar documentación técnica

---

**Slice Status**: ✅ **COMPLETADO - Listo para integración**
**Issue Progress**: #027 RBAC CRUD Operations - 6/6 slices (100%)