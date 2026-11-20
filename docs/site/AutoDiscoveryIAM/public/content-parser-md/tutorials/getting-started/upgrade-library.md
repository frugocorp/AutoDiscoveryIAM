---
[slug]: upgrade-library
[title]: Actualizar AutoDiscoveryIAM
[description]: Cómo actualizar el paquete en una app que ya lo usa, sin perder datos, roles ni asignaciones.
[keywords]: actualizar, migraciones, NuGet, versión, AutoDiscoveryIAM
---

# Actualizar AutoDiscoveryIAM

La librería no trae migraciones: su modelo vive en el paquete y las migraciones son de tu app, así que actualizarla es el mismo flujo de EF Core que ya usas para tus propias entidades.

1. Lee las [notas de la versión](https://github.com/frugocorp/AutoDiscoveryIAM/releases) (las mismas del CHANGELOG del repositorio). Mientras la versión sea `0.y.z`, cualquier versión menor puede traer cambios incompatibles.
2. Actualiza el paquete: `dotnet add package FrugoCorp.AspNetCore.AutoDiscoveryIAM --version <nueva>`.
3. Genera una migración y revísala. Debe contener solo los cambios de esquema que anuncia el CHANGELOG, o ninguno:

   ````code-block
   ```bash
   dotnet ef migrations add UpdateAutoDiscoveryIAM --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
   dotnet ef database update
   ```
   ````

4. Despliega. Al arrancar, `SeedAutoDiscoveryIAMDefaultDataAsync()` aplica una sola vez por base de datos los cambios de **datos** del paquete (los registra en la tabla `AutoDiscoveryIAMUpgrades`) y después agrega al rol `AdminAutoDiscoveryIAM` los permisos por defecto nuevos. Los roles, asignaciones y usuarios existentes quedan como estaban.
5. Corre `POST /permissions/sync` para que los endpoints nuevos de la librería obtengan su permiso.

El paquete mantiene estables las claves de permiso entre versiones. Si algún día hay que renombrar un slice de la librería, la propia actualización mueve sus asignaciones de rol a la clave nueva, para que nadie pierda acceso.

Para probar una versión nueva en tu proyecto real antes de publicarla, empaquétala a un feed local con una versión preliminar única (`0.2.0-local.1`, `0.2.0-local.2`...) e instálala desde ahí, como se explica en [Empaquetar la librería desde tu fork y probarla en otro proyecto](/tutorials/getting-started/package-library).
