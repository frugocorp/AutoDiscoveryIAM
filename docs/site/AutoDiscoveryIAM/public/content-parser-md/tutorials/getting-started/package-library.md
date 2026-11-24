---
[slug]: package-library
[title]: Empaquetar la librería desde tu fork
[description]: Cómo generar tu propio paquete NuGet de la librería a partir de un fork.
[keywords]: NuGet, dotnet pack, fork, empaquetar librería
[readme_flavors]: github, npmjs
---

# Empaquetar la librería desde tu fork y probarla en otro proyecto

Si clonaste o forkeaste este repo (código fuente, sin que el paquete esté publicado todavía en NuGet.org), el comando de [Instalación](/tutorials/getting-started/installation) (`dotnet add package FrugoCorp.AspNetCore.AutoDiscoveryIAM`) **no va a funcionar tal cual** — ese comando asume que el paquete ya está en NuGet.org, y todavía no lo está. Puedes generar tu propio `.nupkg` local y consumirlo desde un proyecto de prueba aparte exactamente igual que si viniera de un feed real. Este es el mismo flujo que ya se validó en vivo antes de considerar la librería lista para usarse.

**Requisito: SDK de .NET 10 instalado.** Verifica con `dotnet --version`; si no lo tienes, instálalo desde la [página oficial de descargas de .NET](https://dotnet.microsoft.com/download) antes de continuar.

## Genera el paquete

Hacia una carpeta que sirva como "feed" local (puede ser cualquier carpeta en tu máquina, dentro o fuera de este repo):

````code-block
```bash
dotnet pack src/AutoDiscoveryIAM/AutoDiscoveryIAM.csproj -c Release -o "C:\ruta\a\tu\feed-local"
```
````

> ⚠️ Si en vez de correr `dotnet pack` navegas manualmente a `src/AutoDiscoveryIAM/bin/Release/net10.0/`, vas a encontrar 4 archivos sueltos (`.dll`, `.pdb`, `.xml`, `.deps.json`) — esos son la compilación cruda, **no** el paquete instalable, y copiarlos a mano a otro proyecto no funciona (le faltarían todas las dependencias de la librería: EF Core, JWT, FluentValidation, etc.). El único archivo que necesitas es el `.nupkg` que genera `dotnet pack` en la carpeta que le indiques con `-o` — ese sí trae listada la lista completa de dependencias para que NuGet las resuelva solo.

## Declara el feed local

En tu proyecto de prueba, agrega un `NuGet.config` que declare esa carpeta como fuente:

````code-block
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="feed-local" value="C:\ruta\a\tu\feed-local" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```
````

## Instala desde tu feed local

Igual que en "Instalación" pero sin depender de una publicación real: [Agrega la librería a tu proyecto](/tutorials/getting-started/installation#agrega-la-libreria-a-tu-proyecto)

A partir de aquí, el resto es exactamente lo mismo que en [Instalación](/tutorials/getting-started/installation): registrar `AddAutoDiscoveryIAM()`/`UseAutoDiscoveryIAM()`, elegir tu proveedor de base de datos, generar tus migraciones y aplicar la configuración mínima obligatoria.

## Si repites la misma versión (limpia la caché de NuGet)

Si vuelves a correr `dotnet pack` con la misma versión después de cambiar algo en la librería, limpia la copia en caché de NuGet de **este paquete específico** antes de reinstalar, para que tu proyecto de prueba tome el contenido nuevo y no el que ya tenía guardado. Corre esto en una terminal (PowerShell en Windows) — no es parte de tu código, es limpieza de la caché de NuGet de tu máquina:

````code-block
```powershell
Remove-Item -Recurse -Force "$env:USERPROFILE\.nuget\packages\frugocorp.aspnetcore.autodiscoveryiam\<version>"
```
````

Reemplaza `<version>` por la versión real que empaquetaste (ej. `0.2.0`). Esto borra solo la copia en caché de esta librería — no toca la caché de ningún otro proyecto .NET en tu máquina.

> Si PowerShell responde `No se encuentra la ruta de acceso ... porque no existe`, no es un error que te bloquee — significa que esa caché ya estaba limpia (por ejemplo, si ya la habías borrado antes). Puedes seguir directo al siguiente paso.

**Después de esto** (haya borrado algo o no), vuelve a instalar el paquete para que tu proyecto tome el contenido nuevo desde tu feed local: [Agrega la librería a tu proyecto](/tutorials/getting-started/installation#agrega-la-libreria-a-tu-proyecto)

## El build funciona pero el editor sigue marcando error

Si repites la misma versión del paquete (ver punto anterior) y luego ves un tipo de la librería subrayado en rojo con *"The type or namespace name '...' could not be found"* — hay dos cachés **distintas e independientes** que pueden estar desactualizadas, y limpiar una no limpia la otra:

1. **El build server de MSBuild/Roslyn** (`VBCSCompiler`) — un proceso en segundo plano que el SDK de .NET deja vivo entre compilaciones para que la siguiente sea más rápida. Si repites la misma versión de un paquete, a veces sigue con el ensamblado viejo cargado en memoria y no se entera del cambio. Se soluciona con:

   ````code-block
   ```bash
   dotnet build-server shutdown
   ```
   ````

   Es seguro y no afecta nada más: no borra archivos, no toca NuGet, no afecta ninguna app que ya esté corriendo (`dotnet run`, `dotnet watch`, etc. son procesos de ejecución, no de compilación). Es una acción de toda la máquina, no solo de este proyecto — el único "costo" es que la próxima compilación de cualquier proyecto tuyo tarda un poco más al arrancar el compilador de nuevo.

2. **El servidor de lenguaje de tu editor** (OmniSharp o C# Dev Kit en VS Code) — es un proceso **separado** del anterior, con su propia copia en memoria del ensamblado para IntelliSense. Apagar el build server de MSBuild no lo toca. Si ya confirmaste que `dotnet build` desde terminal compila bien pero el editor sigue marcando el error, el problema es este, no el anterior. Arréglalo sin cerrar el proyecto:

   - `Ctrl+Shift+P` → **"Developer: Reload Window"** (recarga VS Code y todas sus extensiones).
   - Si sigue el subrayado: con la extensión clásica **C#**, `Ctrl+Shift+P` → **"OmniSharp: Restart OmniSharp"**; con **C# Dev Kit**, busca algo como **"Restart Language Server"**.
   - Cerrar y volver a abrir VS Code por completo también funciona (equivalente a "Reload Window", pero más lento).
