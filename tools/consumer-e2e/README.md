# Prueba de consumo y actualización (consumer-e2e)

`MiApi` es una app mínima que instala AutoDiscoveryIAM **como paquete NuGet**, igual que un proyecto real: namespace raíz de un segmento, endpoints propios bajo `Features`, un endpoint fuera de `Features` y otro de la plantilla de `dotnet new webapi`. Sirve para dos cosas antes de publicar una versión:

- **Instalación nueva** (`scenario.mjs`): sync, permisos propios, reporte de endpoints sin permiso, rol propio, 200 con permiso y 403 sin él.
- **Actualización** (`upgrade.mjs`): la versión anterior crea datos y roles; tras actualizar el paquete y migrar, todo sigue funcionando.

Necesitas Node 22 o superior, `dotnet-ef` y un SQL Server alcanzable. Todos los comandos son desde esta carpeta.

## 1. Preparar

```powershell
Copy-Item MiApi/example.appsettings.Development.json MiApi/appsettings.Development.json   # complétalo
dotnet pack ../../src/AutoDiscoveryIAM -c Release -p:Version=0.2.0-local.1 -o feed
$env:AdiamVersion = "0.2.0-local.1"        # la versión que acabas de empaquetar
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

Usa una versión única en cada empaquetado (`0.2.0-local.2`, `0.2.0-local.3`...); así NuGet nunca toma una copia vieja de su caché. `AdiamVersion` va como **variable de entorno** a propósito: MSBuild la lee como propiedad, y así sirve igual para `dotnet ef` (donde `-p` significa `--project`) y para `dotnet run`. Sin ella se intenta restaurar la versión por defecto de `Directory.Build.props` y `dotnet ef` falla con "Unable to retrieve project metadata".

## 2. Instalación nueva

```powershell
cd MiApi
dotnet restore   # dotnet ef no restaura solo: sin esto falla con "Unable to retrieve project metadata"
dotnet ef migrations add InitialCreate --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
dotnet ef database update --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
dotnet run --no-launch-profile --urls http://127.0.0.1:5099
```

En otra terminal: `node scenario.mjs` (y `node scenario.mjs collision` si agregas un endpoint que choque con una clave de la librería).

## 3. Actualización

1. Con la versión **publicada anterior** instalada (paso 2), corre `node upgrade.mjs before`.
2. Detén la app, empaqueta la versión nueva, cambia `$env:AdiamVersion` a esa versión y genera la migración de la actualización:

   ```powershell
   dotnet ef migrations add UpdateAdiam --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
   dotnet ef database update --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
   ```

   Revisa que la migración solo contenga lo que anuncia el CHANGELOG.
3. Arranca la app con la versión nueva y corre `node upgrade.mjs after`.

Las credenciales que usan los scripts son las de los usuarios de desarrollo: `MiApi` los pide con `SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true)`, y la librería solo los siembra en `Development`.

## 4. Limpiar

```powershell
dotnet ef database drop -f --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
Remove-Item -Recurse MiApi/Migrations, feed, packages
```
