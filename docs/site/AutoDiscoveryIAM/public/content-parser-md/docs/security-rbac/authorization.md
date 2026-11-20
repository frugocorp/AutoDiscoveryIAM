---
[slug]: authorization
[title]: Autorización con niveles de acceso
[description]: Niveles de acceso, permisos descubiertos automáticamente y los dos ejes de autorización independientes.
[keywords]: autorización, permisos, roles, RBAC, niveles de acceso
---

# Autorización con niveles de acceso

Todo endpoint queda protegido por RBAC estándar salvo que se marque explícitamente con uno de estos atributos (vía `.WithMetadata(...)` en su `*EndPoint.cs`):

| Atributo                              | Quién pasa                                                                                                                                          | Uso típico                                                                                                                                                                    |
| ------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `[PermissionsNotRequired(reason)]`    | Cualquier usuario autenticado (o incluso sin autenticar, en endpoints públicos como login/registro)                                                 | Endpoints self-service: perfil propio, cambio de contraseña propia, 2FA propio, registro público — la autorización es "es tuyo", no basada en rol.                            |
| `[RequiresDevelopmentAccess(reason)]` | Solo `User.IsDevelopment == true`                                                                                                                   | Herramientas de diagnóstico internas. Ni `IsAdmin` ni `IsSuAdmin` alcanzan.                                                                                                   |
| `[RequiresSuAdminAccess(reason)]`     | `User.IsDevelopment == true` **o** `IsSuAdmin == true`                                                                                              | Las pocas operaciones que nunca deben depender de un rol: cambiar el `IsAdmin` de otro usuario, limpiar las relaciones ocultas de un rol, ejecutar un cambio de correo y enviar el correo de prueba de SMTP. `IsAdmin` no alcanza. |
| `[RequirePermissions(note)]`          | Igual que sin atributo                                                                                                                              | Solo documentación/intención explícita — en runtime se comporta exactamente igual que no tener ningún atributo. Puede asignarse sin problema al rol default de auto-registro. |
| _(sin atributo)_                      | Usuario con un rol que tenga el `Permission` de este endpoint asignado, **o** cualquier usuario con `IsAdmin`/`IsDevelopment`/`IsSuAdmin` en `true` | RBAC estándar. Los 3 flags especiales saltan esta validación por completo, sin importar roles/permisos.                                                                       |

**`IsAdmin`, `IsDevelopment` e `IsSuAdmin` son columnas booleanas en `User`**, evaluadas directamente por `AuthorizationMiddleware` -- con una consulta a la BD en cada request, no desde el JWT -- y no derivan de ningún rol ni permiso. Un usuario con el rol `AdminAutoDiscoveryIAM` (con todos los permisos del package) igual no puede llamar un endpoint `[RequiresSuAdminAccess]` si no tiene ese flag en `true`; y si se le revoca el flag a alguien, el efecto es inmediato en la siguiente petición, sin depender de que expire un token viejo. `IsDevelopment` e `IsSuAdmin` no tienen ningún endpoint: se marcan por acceso directo a la base de datos. `IsAdmin` además se puede cambiar con `PUT /users/{publicId}/admin-type`, que a su vez exige `[RequiresSuAdminAccess]`. Al ser el único punto del sistema sin RBAC detrás, `tests/.../SensitiveFlagMassAssignmentTests.cs` verifica que ningún endpoint público los termine leyendo del body por error. Tampoco las administra cualquiera con el permiso: editar una cuenta con flags (`PUT /users/{publicId}`, por ejemplo para cambiarle el email y luego pedir el reseteo de contraseña), aprobarla, rechazarla, reenviarle la activación o cambiarle el tipo Admin exige un nivel igual o superior (`IsDevelopment` solo otro `IsDevelopment`; `IsSuAdmin`/`IsAdmin`, un `IsSuAdmin` o `IsDevelopment`); si no, responde 403. Es la misma razón por la que tampoco existe un endpoint para marcar qué rol es el default de auto-registro (`Role.IsDefaultForNewUser`, como mucho una fila a la vez) — automatizar una asignación tan sensible, con el riesgo de marcar la fila equivocada, no vale el ahorro de una consulta manual.

## Roles sembrados por defecto

El package siembra dos roles al arrancar, siempre:

- **`AdminAutoDiscoveryIAM`** — administra la app desde el primer día, sin ningún flag especial: consulta usuarios, roles y permisos; crea, edita y restaura roles; les asigna y quita permisos; asigna roles a usuarios, y aprueba o rechaza registros. Puede darse a sí mismo o a otros roles cualquier permiso, incluidos los de tus propios endpoints después de sincronizar. No puede cambiar `IsAdmin`/`IsDevelopment`/`IsSuAdmin` ni correr el sync: eso sigue detrás de los flags.
- **`UserDefault`** — nace sin ningún permiso. Pensado para asignarse automáticamente a usuarios auto-registrados; el equipo dev le agrega los permisos que su app necesite. Puede eliminarse o renombrarse — si se reemplaza, basta con marcar `IsDefaultForNewUser=true` en el rol sustituto directamente en la base de datos.

La API rechaza asignar, vía `RolePermission`, un permiso `[RequiresDevelopmentAccess]`/`[RequiresSuAdminAccess]` a un rol que ya tiene `IsDefaultForNewUser=true`. **Esto no cubre el camino inverso**: como `IsDefaultForNewUser` solo se marca por acceso directo a la base de datos (sin endpoint, ver arriba), nada en la aplicación puede impedir que alguien marque como default un rol que _ya_ tenía esos permisos asignados desde antes. Es el mismo nivel de confianza que ya se le da a quien administra la base de datos para el resto de asignaciones sensibles — no es un hueco explotable por un usuario, solo una operación manual que hay que hacer con cuidado.

## Primer usuario con privilegios

No hay ningún admin real sembrado. Las cuentas de ejemplo con contraseña pública solo se crean si las pides con `SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true)` y el ambiente es `Development`/`Testing`; en cualquier otro ambiente se ignora el opt-in, y si esas cuentas existen activas se registra un log crítico al arrancar — ver [Credenciales de desarrollo por defecto](/references/development/dev-credentials)). El registro público (`POST /users`, o alta automática por login social) siempre crea usuarios sin rol y sin ningún flag especial. Para tener el primer administrador: que esa persona se registre con su correo real, y que quien administre la base de datos le asigne el rol `AdminAutoDiscoveryIAM` directamente en la base (ejemplo para SQL Server):

````code-block
```sql
INSERT INTO UserRoles (UserId, RoleId, AssignedAt, PublicId, CreatedAt, IsDeleted)
SELECT u.Id, r.Id, SYSUTCDATETIME(), NEWID(), SYSUTCDATETIME(), 0
FROM Users u CROSS JOIN Roles r
WHERE u.Email = 'tu-correo-real@ejemplo.com' AND r.Name = 'AdminAutoDiscoveryIAM';
```
````

`PublicId`, `AssignedAt`, `CreatedAt` e `IsDeleted` son obligatorios: un `INSERT` solo con `UserId`/`RoleId` falla.

A partir de ahí, esa cuenta administra a todos los demás usuarios desde la API.

**`IsSuAdmin` e `IsDevelopment` solo se marcan a mano, directamente en la base de datos**, y solo los marca quien la administra. Es deliberado: ninguna llamada a la API, cuenta comprometida o rol mal configurado puede otorgarlos. Dale `IsDevelopment` solo a quien vaya a correr `POST /permissions/sync`:

````code-block
```sql
UPDATE Users SET IsDevelopment = 1 WHERE Email = 'quien-sincroniza@ejemplo.com';
UPDATE Users SET IsSuAdmin = 1 WHERE Email = 'tu-correo-real@ejemplo.com';
```
````

---

## Regla de no escalada

Nadie otorga ni administra permisos que no tiene. Al guardar, la librería revisa que quien hace la operación tenga:

- **Al asignar o restaurar un rol a un usuario** (o reactivar un rol): todos los permisos de ese rol.
- **Al asignar o restaurar un permiso a un rol**: ese permiso.
- **Al cambiar el email de otro usuario**: todos los permisos de ese usuario, y que ese usuario no tenga flags. Sin esto, quien solo puede editar usuarios le cambiaría el email a un administrador y tomaría su cuenta con el restablecimiento de contraseña.

Si falta algo, la API responde 403 y no guarda nada. La regla aplica en cualquier camino (asignación individual, restauración u operación masiva), así que no depende de cómo se arme cada rol.

No aplica a cuentas con `IsAdmin`, `IsSuAdmin` o `IsDevelopment`, ni a quien tiene **todos** los permisos administrativos de la librería (el rol `AdminAutoDiscoveryIAM` completo): ese ya administra todo, y es quien otorga los permisos de tus propios endpoints después de cada sincronización, que al principio no tiene nadie. Lo que la regla restringe son los roles administrativos parciales. Por ejemplo, un rol de soporte que solo puede asignar roles no puede asignarse a sí mismo `AdminAutoDiscoveryIAM`.

## Permisos para tus propios endpoints

El secure-by-default también aplica a **tus** endpoints, así que cada uno necesita un `Permission`. La librería lo deriva del namespace de la clase que declara el handler del endpoint y lo crea cuando sincronizas.

### Convención de namespaces

`<CualquierRaíz>.Features.<Feature>.[Commands|Queries.]<Acción>` produce la clave `feature.accion_snake_case`. La raíz es libre, y la acción es siempre el **último** segmento, así que un slice anidado es un permiso propio:

| Namespace de la clase que declara el handler | Clave del permiso |
|---|---|
| `MiApi.Features.Pedidos.Commands.CrearPedido` | `pedidos.crear_pedido` |
| `Empresa.Tienda.Features.Pedidos.Queries.GetPedidos` | `pedidos.get_pedidos` |
| `MiApi.Features.Pedidos.Commands.CrearPedido.Borrador` | `pedidos.borrador` |
| `MiApi.Pedidos.Endpoints` (sin segmento `Features`) | ninguna: el sync la reporta, ver abajo |

Varias rutas mapeadas desde el mismo namespace comparten un solo permiso (un slice, grano grueso). Con una lambda en línea, cuenta la clase que contiene el `app.MapPost(...)`; con un method group (`app.MapPost("/pedidos", PedidoHandlers.Crear)`), cuenta la clase de ese método.

### Sincroniza después de cada despliegue

Cada vez que agregues o renombres endpoints, crea sus permisos con `POST /permissions/sync`. Solo puede llamarlo un usuario con `IsDevelopment == true`:

````code-block
```bash
curl -X POST https://tu-api/permissions/sync \
  -H "Authorization: Bearer <token de un usuario IsDevelopment>" \
  -H "Content-Type: application/json" \
  -d '{ "forceFullSync": false, "dryRun": false }'
```
````

- Mientras no se sincronice, un endpoint nuevo responde 403 a todos, salvo a los usuarios con `IsAdmin`/`IsDevelopment`/`IsSuAdmin`.
- Los permisos nuevos no pertenecen a ningún rol. Asígnalos con `PUT /roles/{rolePublicId}/permissions/{permissionPublicId}`, y el rol a los usuarios con `PUT /users/{userPublicId}/roles/{rolePublicId}`. `GET /permissions` lista los permisos con su `publicId`.
- Renombrar un slice cambia su clave: el permiso anterior se marca como obsoleto (nunca se borra) y se crea uno nuevo, que hay que volver a asignar.
- Con `"dryRun": true` ves qué cambiaría sin escribir nada.

### Qué reporta y qué rechaza el sync

- **`endpointsWithoutPermission`** en la respuesta lista los endpoints que exigen un permiso pero cuyo namespace no sigue la convención. Muévelos bajo `Features`, o márcalos con `[PermissionsNotRequired]`, `[RequiresDevelopmentAccess]` o `[RequiresSuAdminAccess]` si no deben depender de un rol.
- **Una clave duplicada hace fallar el sync con un 400** que nombra los dos namespaces. Pasa cuando dos slices distintos producen la misma clave, por ejemplo tu `MiApi.Features.Users.Commands.CreateUser` contra el `users.create_user` de la propia librería. Renombra tu feature o tu acción: si no, quien tuviera uno de los dos permisos entraría también al otro sin que nadie lo decida.
