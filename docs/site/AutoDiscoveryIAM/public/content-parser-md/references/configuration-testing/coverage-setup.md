---
[slug]: coverage-setup
[title]: Configuración de coverage
[description]: Cómo medir la cobertura de pruebas y generar sus reportes.
[keywords]: cobertura, coverage, ReportGenerator, runsettings
[readme_flavors]: github, npmjs
---

# Configuración de coverage

La configuración de coverage vive organizada así:

````code-block
```text
runsettings/
├── coverage.runsettings          # Configuración para desarrollo
├── ci-full.runsettings          # Configuración para CI
├── normal.runsettings           # Tests normales sin coverage
├── performance.runsettings      # Tests de performance
└── runsettings/
    └── coverage/                # Directorio de salida organizado
        ├── raw/                 # Datos raw de coverlet (GUID dirs)
        └── html/               # Reportes HTML generados
```
````

# Scripts de automatización

`run-tests-coverage-run.ps1` ejecuta tests con coverage y genera reportes HTML automáticamente:

````code-block
```powershell
.\run-tests-coverage-run.ps1
```
````

`run-tests-coverage-clean.ps1` limpia directorios GUID y reportes anteriores:

````code-block
```powershell
.\run-tests-coverage-clean.ps1
```
````

# Comandos manuales

Ejecutar tests con coverage:

````code-block
```bash
dotnet test --settings runsettings/coverage.runsettings --verbosity minimal
```
````

Generar el reporte HTML a partir de los resultados:

````code-block
```bash
reportgenerator -reports:"runsettings\runsettings\coverage\raw\*\coverage.cobertura.xml" -targetdir:"runsettings\runsettings\coverage\html" -reporttypes:"Html;HtmlSummary;Badges" -sourcedirs:"." -verbosity:Info
```
````

# Configuración de `coverage.runsettings`

````code-block
```xml
<RunConfiguration>
  <ResultsDirectory>runsettings\runsettings\coverage</ResultsDirectory>
  <OutputDirectory>runsettings\runsettings\coverage</OutputDirectory>
</RunConfiguration>

<DataCollectionRunSettings>
  <DataCollectors>
    <DataCollector friendlyName="XPlat code coverage">
      <Configuration>
        <Format>json,cobertura,lcov,teamcity,opencover</Format>
        <Exclude>[*Tests]*,[*]Program</Exclude>
      </Configuration>
    </DataCollector>
  </DataCollectors>
</DataCollectionRunSettings>
```
````

# Directorios GUID

Coverlet genera automáticamente directorios con nombres GUID para evitar conflictos entre ejecuciones concurrentes de tests. Los scripts de arriba incluyen limpieza automática de directorios GUID antiguos para mantener el workspace organizado. Cada directorio GUID contiene:

- `coverage.cobertura.xml` — para ReportGenerator
- `coverage.json` — formato JSON
- `coverage.info` — formato LCOV
- `coverage.opencover.xml` — formato OpenCover

# Buenas prácticas

1. Preferir los scripts (`run-tests-coverage-run.ps1`/`run-tests-coverage-clean.ps1`) sobre los comandos manuales.
2. Limpiar regularmente con `run-tests-coverage-clean.ps1`.
3. Usar `ci-full.runsettings` en pipelines de CI.
4. Usar `coverage.runsettings` en desarrollo local.

# Archivos de configuración

- `coverage.runsettings` — desarrollo con coverage completo.
- `ci-full.runsettings` — CI/CD con coverage y configuración robusta.
- `normal.runsettings` — tests rápidos sin coverage.
- `performance.runsettings` — tests de performance con timeouts ajustados.
