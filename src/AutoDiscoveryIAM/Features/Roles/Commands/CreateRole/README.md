# CreateRole Command

Implementa la creación de roles bajo el patrón vertical slice para el sistema RBAC.

- Handler, Command, Repository, DI, Endpoint, Logger, Validación y Ejemplos Swagger.
- Sigue las convenciones y estructura estándar del proyecto.
- Utiliza las reglas de validación avanzadas específicas de Role.

## Características específicas

- **Validación de duplicados**: Verifica que no exista un rol con el mismo nombre.
- **Validación avanzada**: Usa `RoleValidationRules` con regex y tipos específicos.
- **Logging especializado**: Integra con `IRoleLogger` para trazabilidad.
- **Respuestas de negocio**: 422 para nombres duplicados, 400 para validación.
