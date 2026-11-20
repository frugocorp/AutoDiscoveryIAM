---
[slug]: testing
[title]: Pruebas
[description]: Cómo están organizadas las pruebas del proyecto y cómo ejecutarlas.
[keywords]: pruebas, xUnit, pruebas de integración, dotnet test
[readme_flavors]: github, npmjs
---

# Pruebas

Corre toda la suite, sin cobertura ni reporte HTML:

````code-block
```bash
dotnet test
```
````

`run-tests-with-report.ps1` es la forma recomendada de correr la suite — envuelve `dotnet test` con un perfil `.runsettings` con nombre y genera un reporte HTML al final:

````code-block
```powershell
.\run-tests-with-report.ps1 -RunsettingsType "normal"
```
````

Hay 4 perfiles ([`runsettings/*.runsettings`](/references/development/runsettings)) porque cada uno resuelve una necesidad distinta — no es redundancia, cada combinación de filtro/cobertura/paralelismo tiene un costo de tiempo distinto:

| Perfil        | Tiempo aprox. | Qué corre                           | Cobertura                             | Paralelismo                                            | Cuándo usarlo                                                                    |
| ------------- | ------------- | ----------------------------------- | ------------------------------------- | ------------------------------------------------------ | -------------------------------------------------------------------------------- |
| `normal`      | 2-3 min       | Todo excepto `Category=Performance` | ❌ No                                 | Máximo (todos los cores)                               | Desarrollo diario — feedback más rápido mientras programas.                      |
| `coverage`    | 3-4 min       | Todo excepto `Category=Performance` | ✅ Sí (json/lcov/opencover/cobertura) | Moderado (2 hilos)                                     | Validar cobertura sin pagar el costo de las pruebas de performance.              |
| `performance` | 6-8 min       | Solo `Category=Performance`         | ❌ No                                 | Ninguno (1 hilo, para mediciones estables/comparables) | Benchmark y detectar regresiones de rendimiento.                                 |
| `ci-full`     | 10-12 min     | Todo (normales + performance)       | ✅ Sí (todos los formatos)            | Máximo                                                 | Validación final antes de merge/release — el que se corre antes de cerrar un PR. |

`run-tests-with-report.ps1` acepta estas banderas:

| Bandera                | Tipo                                                   | Default                  | Qué hace                                                                                                                                 |
| ---------------------- | ------------------------------------------------------ | ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `-RunsettingsType`     | string                                                 | `"ci-full"`              | Cuál de los 4 perfiles de la tabla de arriba usar.                                                                                       |
| `-OpenReportInBrowser` | switch                                                 | apagado                  | Abre el reporte HTML generado apenas termina la corrida — sin esta bandera, el reporte se genera igual, solo no se abre solo.            |
| `-Browser`             | string                                                 | navegador default del SO | Ejecutable/nombre del navegador a usar en vez del default (ej. `"msedge"`) — solo tiene efecto junto con `-OpenReportInBrowser`.         |
| `-NoBuild`             | switch                                                 | apagado                  | Salta la recompilación previa (usa los binarios ya compilados).                                                                          |
| `-Verbosity`           | `quiet`\|`minimal`\|`normal`\|`detailed`\|`diagnostic` | `"normal"`               | Nivel de detalle del output de `dotnet test` (`detailed` incluye el output de `ITestOutputHelper`, útil para depurar un test que falla). |

Validación completa, con el reporte abierto al terminar:

````code-block
```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -OpenReportInBrowser
```
````

Reintentar el reporte sin recompilar, viendo el detalle de cada test:

````code-block
```powershell
.\run-tests-with-report.ps1 -RunsettingsType "normal" -NoBuild -Verbosity "detailed"
```
````

Los 3 scripts de test viven en la raíz con el mismo prefijo (`run-tests-*.ps1`) para encontrarse juntos en el explorador de archivos: `run-tests-with-report.ps1` (el de arriba), y `run-tests-coverage-run.ps1`/`run-tests-coverage-clean.ps1` (generación/limpieza de reportes de cobertura más acotada, ver [Configuración de coverage](/references/configuration-testing/coverage-setup)).

---
