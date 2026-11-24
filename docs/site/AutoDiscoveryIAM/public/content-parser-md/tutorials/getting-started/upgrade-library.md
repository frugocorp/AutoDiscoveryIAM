---
[slug]: upgrade-library
[title]: Actualizar AutoDiscoveryIAM
[description]: Cómo actualizar el paquete en una app que ya lo usa, sin perder datos, roles ni asignaciones.
[keywords]: actualizar, migraciones, NuGet, versión, AutoDiscoveryIAM
---

# Actualizar AutoDiscoveryIAM

La librería no trae migraciones: su modelo vive en el paquete y las migraciones son de tu app, así que actualizarla es el mismo flujo de EF Core que ya usas para tus propias entidades.

1. Lee las [notas de la versión](https://github.com/frugocorp/AutoDiscoveryIAM/releases) (las mismas del CHANGELOG del repositorio), empezando por la sección `Breaking`. Mientras la versión sea `0.y.z`, cualquier versión menor puede traer cambios incompatibles.
2. Actualiza el paquete: `dotnet add package FrugoCorp.AspNetCore.AutoDiscoveryIAM --version <nueva>`.
3. Genera una migración y revísala. Debe contener solo los cambios de esquema que anuncia el CHANGELOG, o ninguno:

   ````code-block
   ```bash
   dotnet ef migrations add UpdateAutoDiscoveryIAM --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
   dotnet ef database update
   ```
   ````

4. Despliega. Al arrancar, `SeedAutoDiscoveryIAMDefaultDataAsync()` aplica una sola vez por base de datos los cambios de **datos** del paquete (los registra en la tabla `AutoDiscoveryIAMUpgrades`) y después agrega al rol `AdminAutoDiscoveryIAM` los permisos por defecto nuevos. Los roles, asignaciones y usuarios existentes quedan como estaban.
5. Sincroniza los permisos para que los endpoints nuevos de la librería obtengan el suyo: `POST /permissions/sync`, o `app.SyncAutoDiscoveryIAMPermissionsAsync()` desde tu pipeline de despliegue (ver [Autorización](/docs/security-rbac/authorization)).

El paquete mantiene estables las claves de permiso entre versiones. Si algún día hay que renombrar un slice de la librería, la propia actualización mueve sus asignaciones de rol a la clave nueva, para que nadie pierda acceso.

## Qué garantiza cada versión

- **Cambios incompatibles anunciados.** Todo lo que deja de funcionar igual que en la versión anterior aparece en la sección `Breaking` del CHANGELOG, con el tipo de cambio (código fuente, binario, comportamiento o esquema), a quién afecta y qué hacer. Si una entrada no te aplica, la actualización no te pide nada más que la migración.
- **API pública verificada por herramienta.** Cada paquete se compara contra la última versión publicada con la validación de paquete del SDK de .NET: una ruptura de la API pública que no esté declarada detiene la publicación. Que se anuncie no depende de que alguien la recuerde.
- **Probada como la usarías tú.** Antes de publicar, el paquete se instala en una app externa igual que en la tuya, en una base nueva y actualizando una base creada con la versión anterior.
- **Claves de permiso y datos estables.** Una clave de permiso no cambia sin un paso de actualización que mueva sus asignaciones, y la actualización nunca borra roles, permisos ni usuarios.
