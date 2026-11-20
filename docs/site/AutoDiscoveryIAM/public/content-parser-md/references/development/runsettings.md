---
[slug]: runsettings
[title]: RunSettings (perfiles de test)
[description]: Perfiles de ejecución de pruebas y cuándo conviene usar cada uno.
[keywords]: runsettings, perfiles de pruebas, ci-full, cobertura
[readme_flavors]: github, npmjs
---

# 🎯 RunSettings Configuration


## 📁 Estructura

````code-block
```text
runsettings/
├── normal.runsettings          # ⚡ Desarrollo diario (sin performance, sin coverage)
├── performance.runsettings     # 🏃 Solo pruebas de performance
├── security.runsettings        # 🔒 Solo pruebas de seguridad ofensiva
├── coverage.runsettings        # 📊 Coverage detallado (sin performance)
├── ci-full.runsettings         # 🚀 CI/CD completo (todo + coverage)
├── appsettings.template.json   # 📧 Plantilla del aviso por correo (sin secretos, versionado)
├── appsettings.json            # 📧 Config real del aviso por correo (gitignored, opcional)
├── README.md                   # 📖 Esta documentación
└── coverage/                   # 📁 Directorio de salida para reportes
```
````

---

## 🚀 Comandos de Uso

### Desarrollo Diario (Más Rápido)

Ejecución rápida sin coverage ni performance:

````code-block
```powershell
dotnet test --settings runsettings/normal.runsettings
```
````

### Coverage Analysis

Coverage detallado sin pruebas de performance:

````code-block
```powershell
dotnet test --settings runsettings/coverage.runsettings
```
````

### Pruebas de Performance

Solo pruebas de performance:

````code-block
```powershell
dotnet test --settings runsettings/performance.runsettings
```
````

### Pruebas de Seguridad

Solo pruebas de seguridad ofensiva (JWT tampering, IDOR, 2FA, CORS, etc.):

````code-block
```powershell
dotnet test --settings runsettings/security.runsettings
```
````

Recomendado: usar el wrapper, que además corre el escaneo de dependencias vulnerables:

````code-block
```powershell
.\run-tests-with-report.ps1 -RunsettingsType "security"
```
````

### CI/CD Completo

Todas las pruebas + coverage completo (incluye seguridad, sin escaneo de dependencias):

````code-block
```powershell
dotnet test --settings runsettings/ci-full.runsettings
```
````

---

## ⚡ Scripts Recomendados

Agrega estos alias a tu perfil de PowerShell (`$PROFILE`):

````code-block
```powershell
# Funciones de testing
function tn { dotnet test --settings runsettings/normal.runsettings }      # Test Normal
function tc { dotnet test --settings runsettings/coverage.runsettings }   # Test Coverage
function tp { dotnet test --settings runsettings/performance.runsettings } # Test Performance
function ts { dotnet test --settings runsettings/security.runsettings }   # Test Security
function ta { dotnet test --settings runsettings/ci-full.runsettings }    # Test All
```
````

---

## 📊 Configuraciones por Archivo

### 🟢 normal.runsettings

- **Filtro**: `Category!=Performance`
- **Coverage**: ❌ No
- **Paralelismo**: ✅ Máximo
- **Timeout**: 10 minutos
- **Uso**: Desarrollo diario, TDD

### 🔵 coverage.runsettings

- **Filtro**: `Category!=Performance`
- **Coverage**: ✅ Sí (múltiples formatos)
- **Paralelismo**: ✅ Moderado (2 cores)
- **Timeout**: 30 minutos
- **Uso**: Análisis de cobertura

### 🟡 performance.runsettings

- **Filtro**: `Category=Performance`
- **Coverage**: ❌ No
- **Paralelismo**: ❌ No (1 core)
- **Timeout**: 60 minutos
- **Uso**: Benchmarks y análisis de rendimiento

### 🔒 security.runsettings

- **Filtro**: `Category=Security`
- **Coverage**: ❌ No (perfil de comportamiento, no de métricas)
- **Paralelismo**: 🟡 Moderado (2 cores) — hay pruebas de rate limiting/2FA sensibles a timing
- **Timeout**: 30 minutos
- **Uso**: Validar la suite de seguridad ofensiva de forma aislada. Ejecutada vía `run-tests-with-report.ps1`, además corre `dotnet list package --vulnerable --include-transitive` sobre la solución y guarda el resultado en `vulnerable-packages.log` dentro de la carpeta del run.

### 🔴 ci-full.runsettings

- **Filtro**: Sin filtros (todo)
- **Coverage**: ✅ Sí (todos los formatos)
- **Paralelismo**: ✅ Máximo
- **Timeout**: 120 minutos
- **Uso**: CI/CD, validación completa

---

## 🎯 Categorías de Pruebas

Las pruebas se categorizan usando el atributo `[Trait]`:

````code-block
```csharp
[Fact]
[Trait("Category", "Performance")]
public void Performance_Test_Example()
{
    // Prueba de performance
}

[Fact] // Sin categoría = prueba normal
public void Normal_Test_Example()
{
    // Prueba normal
}
```
````

---

## 📈 Reportes de Coverage

Los reportes se generan en:

- **Directorio**: `runsettings/coverage/`
- **Formatos**: Cobertura XML, JSON, LCOV, OpenCover
- **HTML**: Generar con ReportGenerator

### Generar Reporte HTML

Después de ejecutar coverage:

````code-block
```powershell
reportgenerator -reports:"runsettings/coverage/**/*.xml" -targetdir:"runsettings/coverage/html-report" -reporttypes:Html
```
````

---

## 📧 Aviso por correo al terminar (opcional)

`run-tests-with-report.ps1` puede avisar por correo al terminar **cualquier** `-RunsettingsType` (normal, coverage, performance, security, ci-full) — útil para `ci-full`/`performance`, que tardan varios minutos.

**Activación**: copia `appsettings.template.json` como `appsettings.json` (en esta misma carpeta) y completa tus datos reales de SMTP (`Mail:Smtp`), remitente (`Mail:From`) y a quién debe llegar el aviso (`Mail:ToDev:Email`/`Name`). `appsettings.json` está en `.gitignore` — nunca se commitea, cada desarrollador configura el suyo. **Además del archivo, cada corrida necesita el flag `-WithEmailReport`** — el archivo por sí solo no envía nada, para no spamear el dev loop diario sin querer:

````code-block
```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -WithEmailReport
```
````

El correo incluye el mismo resumen que se imprime en consola: resultados Unit/Integration (Superado/Omitido/Con error/Total/Duración), cobertura (si el perfil la genera) y los tramos de tiempo del wrapper, más el exit code y las rutas al reporte HTML y a la carpeta de resultados. **Se manda igual en éxito o en fallo** — el asunto y el cuerpo ya indican `EXITO`/`FALLO`, el envío nunca se condiciona al resultado de los tests. El envío es best-effort: si falla (SMTP mal configurado, sin red, etc.), se muestra una nota en consola y además queda registrado en `email-notification.log` (dentro de la carpeta de resultados de esa corrida) — nunca cambia el exit code de la corrida de tests.

`-WithoutAttachments` (combinable con `-WithEmailReport`) manda solo el resumen en texto, sin adjuntar el log/TRX/zip de cobertura — útil para diagnosticar si el relay SMTP está descartando el correo por tamaño/adjuntos:

````code-block
```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -WithEmailReport -WithoutAttachments
```
````

Si los adjuntos superan 15MB en total, el script los omite automáticamente (igual manda el resumen, con una nota) en vez de arriesgarse a que el relay rechace el correo completo.

---

## 🛠️ Troubleshooting

### Error: "No tests were found"

- Verifica que las pruebas tengan las categorías correctas
- Comprueba que el filtro coincida con tus pruebas

### Coverage vacío

- Asegúrate de que `coverlet.collector` esté instalado
- Verifica que los patrones Include/Exclude sean correctos

### Performance lenta

- Usa `normal.runsettings` para desarrollo diario
- Reserva `ci-full.runsettings` para validación completa

---

## 🔧 Configuración Personalizada

Para crear configuraciones personalizadas, copia uno de los archivos existentes y modifica:

- `TestCaseFilter`: Para filtrar pruebas específicas
- `MaxCpuCount`: Para controlar paralelismo
- `Include/Exclude`: Para ajustar coverage
- `TestSessionTimeout`: Para ajustar timeouts

---

## 📚 Referencias

- [Configurar ejecución de pruebas | Microsoft Docs](https://docs.microsoft.com/es-es/dotnet/core/testing/configure-run-tests)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [xUnit Configuration](https://xunit.net/docs/configuration-files)
