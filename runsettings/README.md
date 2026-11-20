<on_site>
# 🎯 RunSettings Configuration

<a id="índice"></a>
## Índice

- [Página de inicio (README principal)](../README.md)
- [Estructura](#estructura)
- [Comandos de Uso](#comandos-de-uso)
  - [Desarrollo Diario (Más Rápido)](#desarrollo-diario)
  - [Coverage Analysis](#coverage-analysis)
  - [Pruebas de Performance](#pruebas-de-performance)
  - [Pruebas de Seguridad](#pruebas-de-seguridad)
  - [CI/CD Completo](#cicd-completo)
- [Scripts Recomendados](#scripts-recomendados)
- [Configuraciones por Archivo](#configuraciones-por-archivo)
  - [normal.runsettings](#normal-runsettings)
  - [coverage.runsettings](#coverage-runsettings)
  - [performance.runsettings](#performance-runsettings)
  - [security.runsettings](#security-runsettings)
  - [ci-full.runsettings](#ci-full-runsettings)
- [Categorías de Pruebas](#categorías-de-pruebas)
- [Reportes de Coverage](#reportes-de-coverage)
  - [Generar Reporte HTML](#generar-reporte-html)
- [Aviso por correo al terminar (opcional)](#aviso-por-correo)
- [Troubleshooting](#troubleshooting)
- [Configuración Personalizada](#configuración-personalizada)
- [Referencias](#referencias)

Esta carpeta contiene las configuraciones de ejecución de pruebas para el proyecto.

---

<a id="estructura"></a>
## 📁 Estructura

[REGRESAR AL ÍNDICE](#índice)

---

```
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

---

<a id="comandos-de-uso"></a>
## 🚀 Comandos de Uso

[REGRESAR AL ÍNDICE](#índice)

---

<a id="desarrollo-diario"></a>
### Desarrollo Diario (Más Rápido)

---

```powershell
# Ejecución rápida sin coverage ni performance
dotnet test --settings runsettings/normal.runsettings
```

<a id="coverage-analysis"></a>
### Coverage Analysis

---

```powershell
# Coverage detallado sin pruebas de performance
dotnet test --settings runsettings/coverage.runsettings
```

<a id="pruebas-de-performance"></a>
### Pruebas de Performance

---

```powershell
# Solo pruebas de performance
dotnet test --settings runsettings/performance.runsettings
```

<a id="pruebas-de-seguridad"></a>
### Pruebas de Seguridad

---

```powershell
# Solo pruebas de seguridad ofensiva (JWT tampering, IDOR, 2FA, CORS, etc.)
dotnet test --settings runsettings/security.runsettings

# Recomendado: usar el wrapper, que además corre el escaneo de dependencias vulnerables
.\run-tests-with-report.ps1 -RunsettingsType "security"
```

<a id="cicd-completo"></a>
### CI/CD Completo

---

```powershell
# Todas las pruebas + coverage completo (incluye seguridad, sin escaneo de dependencias)
dotnet test --settings runsettings/ci-full.runsettings
```

---

<a id="scripts-recomendados"></a>
## ⚡ Scripts Recomendados

[REGRESAR AL ÍNDICE](#índice)

---

Agrega estos alias a tu perfil de PowerShell (`$PROFILE`):

```powershell
# Funciones de testing
function tn { dotnet test --settings runsettings/normal.runsettings }      # Test Normal
function tc { dotnet test --settings runsettings/coverage.runsettings }   # Test Coverage
function tp { dotnet test --settings runsettings/performance.runsettings } # Test Performance
function ts { dotnet test --settings runsettings/security.runsettings }   # Test Security
function ta { dotnet test --settings runsettings/ci-full.runsettings }    # Test All
```

---

<a id="configuraciones-por-archivo"></a>
## 📊 Configuraciones por Archivo

[REGRESAR AL ÍNDICE](#índice)

---

<a id="normal-runsettings"></a>
### 🟢 normal.runsettings

---

- **Filtro**: `Category!=Performance`
- **Coverage**: ❌ No
- **Paralelismo**: ✅ Máximo
- **Timeout**: 10 minutos
- **Uso**: Desarrollo diario, TDD

<a id="coverage-runsettings"></a>
### 🔵 coverage.runsettings

---

- **Filtro**: `Category!=Performance`
- **Coverage**: ✅ Sí (múltiples formatos)
- **Paralelismo**: ✅ Moderado (2 cores)
- **Timeout**: 30 minutos
- **Uso**: Análisis de cobertura

<a id="performance-runsettings"></a>
### 🟡 performance.runsettings

---

- **Filtro**: `Category=Performance`
- **Coverage**: ❌ No
- **Paralelismo**: ❌ No (1 core)
- **Timeout**: 60 minutos
- **Uso**: Benchmarks y análisis de rendimiento

<a id="security-runsettings"></a>
### 🔒 security.runsettings

---

- **Filtro**: `Category=Security`
- **Coverage**: ❌ No (perfil de comportamiento, no de métricas)
- **Paralelismo**: 🟡 Moderado (2 cores) — hay pruebas de rate limiting/2FA sensibles a timing
- **Timeout**: 30 minutos
- **Uso**: Validar la suite de seguridad ofensiva de forma aislada. Ejecutada vía `run-tests-with-report.ps1`, además corre `dotnet list package --vulnerable --include-transitive` sobre la solución y guarda el resultado en `vulnerable-packages.log` dentro de la carpeta del run.

<a id="ci-full-runsettings"></a>
### 🔴 ci-full.runsettings

---

- **Filtro**: Sin filtros (todo)
- **Coverage**: ✅ Sí (todos los formatos)
- **Paralelismo**: ✅ Máximo
- **Timeout**: 120 minutos
- **Uso**: CI/CD, validación completa

---

<a id="categorías-de-pruebas"></a>
## 🎯 Categorías de Pruebas

[REGRESAR AL ÍNDICE](#índice)

---

Las pruebas se categorizan usando el atributo `[Trait]`:

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

---

<a id="reportes-de-coverage"></a>
## 📈 Reportes de Coverage

[REGRESAR AL ÍNDICE](#índice)

---

Los reportes se generan en:

- **Directorio**: `runsettings/coverage/`
- **Formatos**: Cobertura XML, JSON, LCOV, OpenCover
- **HTML**: Generar con ReportGenerator

<a id="generar-reporte-html"></a>
### Generar Reporte HTML

---

```powershell
# Después de ejecutar coverage
reportgenerator -reports:"runsettings/coverage/**/*.xml" -targetdir:"runsettings/coverage/html-report" -reporttypes:Html
```

---

<a id="aviso-por-correo"></a>
## 📧 Aviso por correo al terminar (opcional)

[REGRESAR AL ÍNDICE](#índice)

---

`run-tests-with-report.ps1` puede avisar por correo al terminar **cualquier** `-RunsettingsType` (normal, coverage, performance, security, ci-full) — útil para `ci-full`/`performance`, que tardan varios minutos.

**Activación**: copia `appsettings.template.json` como `appsettings.json` (en esta misma carpeta) y completa tus datos reales de SMTP (`Mail:Smtp`), remitente (`Mail:From`) y a quién debe llegar el aviso (`Mail:ToDev:Email`/`Name`). `appsettings.json` está en `.gitignore` — nunca se commitea, cada desarrollador configura el suyo. **Además del archivo, cada corrida necesita el flag `-WithEmailReport`** — el archivo por sí solo no envía nada, para no spamear el dev loop diario sin querer:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -WithEmailReport
```

El correo incluye el mismo resumen que se imprime en consola: resultados Unit/Integration (Superado/Omitido/Con error/Total/Duración), cobertura (si el perfil la genera) y los tramos de tiempo del wrapper, más el exit code y las rutas al reporte HTML y a la carpeta de resultados. **Se manda igual en éxito o en fallo** — el asunto y el cuerpo ya indican `EXITO`/`FALLO`, el envío nunca se condiciona al resultado de los tests. El envío es best-effort: si falla (SMTP mal configurado, sin red, etc.), se muestra una nota en consola y además queda registrado en `email-notification.log` (dentro de la carpeta de resultados de esa corrida) — nunca cambia el exit code de la corrida de tests.

`-WithoutAttachments` (combinable con `-WithEmailReport`) manda solo el resumen en texto, sin adjuntar el log/TRX/zip de cobertura — útil para diagnosticar si el relay SMTP está descartando el correo por tamaño/adjuntos:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -WithEmailReport -WithoutAttachments
```

Si los adjuntos superan 15MB en total, el script los omite automáticamente (igual manda el resumen, con una nota) en vez de arriesgarse a que el relay rechace el correo completo.

---

<a id="troubleshooting"></a>
## 🛠️ Troubleshooting

[REGRESAR AL ÍNDICE](#índice)

---

### Error: "No tests were found"

---

- Verifica que las pruebas tengan las categorías correctas
- Comprueba que el filtro coincida con tus pruebas

### Coverage vacío

---

- Asegúrate de que `coverlet.collector` esté instalado
- Verifica que los patrones Include/Exclude sean correctos

### Performance lenta

---

- Usa `normal.runsettings` para desarrollo diario
- Reserva `ci-full.runsettings` para validación completa

---

<a id="configuración-personalizada"></a>
## 🔧 Configuración Personalizada

[REGRESAR AL ÍNDICE](#índice)

---

Para crear configuraciones personalizadas, copia uno de los archivos existentes y modifica:

- `TestCaseFilter`: Para filtrar pruebas específicas
- `MaxCpuCount`: Para controlar paralelismo
- `Include/Exclude`: Para ajustar coverage
- `TestSessionTimeout`: Para ajustar timeouts

---

<a id="referencias"></a>
## 📚 Referencias

[REGRESAR AL ÍNDICE](#índice)

---

- [Configurar ejecución de pruebas | Microsoft Docs](https://docs.microsoft.com/es-es/dotnet/core/testing/configure-run-tests)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [xUnit Configuration](https://xunit.net/docs/configuration-files)
