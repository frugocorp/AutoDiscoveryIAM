# Test Runner Script with Live Output and Flexible RunSettings
#
# Guía rápida para elegir RunSettings:
#
# "normal" (MÁS RÁPIDA para desarrollo diario)
#   - Tiempo: 2-3 minutos aprox (depende de la máquina y cantidad de tests)
#   - Filtro: Category!=Performance
#   - Ejecuta pruebas normales (unitarias + integración NO-performance)
#   - NO ejecuta pruebas de rendimiento/masivas marcadas como Performance
#   - Sin collectors de cobertura => menor sobrecarga
#   - Paralelismo máximo (todos los cores)
#   - Úsala para feedback rápido mientras desarrollas
#
# "coverage" (ANÁLISIS de cobertura, velocidad intermedia)
#   - Tiempo: 3-4 minutos aprox (depende de la máquina y cantidad de tests)
#   - Filtro: Category!=Performance
#   - Ejecuta pruebas normales (unitarias + integración NO-performance)
#   - NO ejecuta pruebas de rendimiento/masivas de categoría Performance
#   - Sí genera cobertura detallada (cobertura/json/lcov/opencover)
#   - Paralelismo moderado (2 hilos) para balancear precisión y tiempo
#   - Úsala cuando quieras validar cobertura sin correr performance
#
# "performance" (SOLO rendimiento / puro masivo de performance)
#   - Tiempo: 6-8 minutos aprox (depende de la máquina y cantidad de tests)
#   - Filtro: Category=Performance
#   - Ejecuta únicamente pruebas de performance/masivas etiquetadas como Performance
#   - NO ejecuta pruebas funcionales normales
#   - Sin paralelismo (1 hilo) para mediciones más estables y comparables
#   - Incluye DotNetEventCounters para telemetría de rendimiento
#   - Úsala para benchmark, tuning y regresiones de performance
#
# "security" (SOLO pruebas de seguridad ofensiva)
#   - Filtro: Category=Security
#   - Ejecuta únicamente pruebas de seguridad (JWT tampering, IDOR, 2FA, CORS, etc.)
#   - Sin cobertura (perfil de comportamiento, no de métricas)
#   - Además del test run, corre "dotnet list package --vulnerable" sobre la solución
#     y vuelca el resultado al log de ejecución
#   - Úsala para validar la suite de seguridad de forma aislada, sin correr todo lo demás
#
# "ci-full" (MÁS COMPLETA, usualmente la más lenta)
#   - Tiempo: 6-8 minutos aprox (depende de la máquina)
#   - Sin filtro: ejecuta todo (normales + performance/masivas + seguridad)
#   - Sí genera cobertura para CI (cobertura + json, deterministic)
#   - Paralelismo alto (todos los cores)
#   - Ideal para validación final antes de merge/release y pipelines CI/CD
#
# Regla práctica:
#   - Quiero velocidad: normal
#   - Quiero cobertura sin performance: coverage
#   - Quiero solo rendimiento/masivo: performance
#   - Quiero solo pruebas de seguridad: security
#   - Quiero validación total: ci-full
#
# Usage:
#   cd <ruta-del-repo>;.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -OpenReportInBrowser
#   cd <ruta-del-repo>;.\run-tests-with-report.ps1 -RunsettingsType "coverage" -OpenReportInBrowser
#   cd <ruta-del-repo>;.\run-tests-with-report.ps1 -RunsettingsType "normal" -OpenReportInBrowser
#   cd <ruta-del-repo>;.\run-tests-with-report.ps1 -RunsettingsType "performance" -OpenReportInBrowser
#   cd <ruta-del-repo>;.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -NoBuild
#   cd <ruta-del-repo>;.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -Verbosity "detailed"
#
# Verbosity levels (dotnet test --verbosity):
#   quiet    - Solo errores y advertencias
#   minimal  - Resultado final de cada test
#   normal   - (default) Nombre + resultado de cada test
#   detailed - Nombre + resultado + output de ITestOutputHelper
#   diagnostic - Todo el output interno del runner
#
# Aviso por correo al terminar (opcional, ver runsettings/README.md):
#   Copia runsettings/appsettings.template.json a runsettings/appsettings.json (gitignored) con
#   tus datos SMTP reales. Requiere ADEMAS pasar -WithEmailReport en cada corrida -- el archivo de
#   config por si solo no envia nada, para no spamear el dev loop diario sin querer.
#   -WithoutAttachments: manda solo el resumen en texto, sin adjuntar log/TRX/zip de cobertura
#   (util para diagnosticar si el relay SMTP esta descartando el correo por tamano/adjuntos).

param(
    [string]$RunsettingsType = "ci-full",
    [switch]$OpenReportInBrowser,
    [string]$Browser,
    [switch]$NoBuild = $false,
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$Verbosity = "normal",
    [switch]$WithEmailReport,
    [switch]$WithoutAttachments
)

$rootPath = Get-Location
$runsettingsDir = "$rootPath\runsettings"
$runsettingsPath = "$runsettingsDir\$RunsettingsType.runsettings"

# Validate that the selected runsettings exists
if (-not (Test-Path $runsettingsPath)) {
    Write-Host "ERROR: RunSettings file not found: $runsettingsPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Available runsettings:" -ForegroundColor Yellow
    Get-ChildItem -Path $runsettingsDir -Filter "*.runsettings" | ForEach-Object {
        Write-Host "  - $($_.BaseName)" -ForegroundColor Cyan
    }
    exit 1
}

# Validate runsettings content early to avoid ambiguous test runner failures
try {
    $runsettingsRaw = Get-Content -Path $runsettingsPath -Raw -ErrorAction Stop
    if ([string]::IsNullOrWhiteSpace($runsettingsRaw)) {
        throw "The runsettings file is empty."
    }

    $runsettingsXml = [xml]$runsettingsRaw
    if (-not $runsettingsXml.RunSettings) {
        throw "Root element <RunSettings> was not found."
    }
}
catch {
    Write-Host "ERROR: Invalid RunSettings file: $runsettingsPath" -ForegroundColor Red
    Write-Host "Details: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Setup output directories under runsettings/coverage/<type>/<timestamp>/
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$coverageBaseDir = "$runsettingsDir\coverage"
$reportDir = "$coverageBaseDir\$RunsettingsType\$timestamp"
$logFile = "$reportDir\execution.log"
$unitTrxFile = "$reportDir\results-unit.trx"
$integrationTrxFile = "$reportDir\results-integration.trx"
$htmlReportDir = "$reportDir\html"
$htmlIndexPath = "$htmlReportDir\index.html"

# Create directories
@($reportDir, $htmlReportDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

function Format-TestDuration {
    param(
        [TimeSpan]$Duration
    )

    $totalMilliseconds = [int64][Math]::Round($Duration.TotalMilliseconds)
    if ($totalMilliseconds -lt 0) {
        $totalMilliseconds = 0
    }

    $totalSeconds = [int64][Math]::Floor($totalMilliseconds / 1000)
    $hours = [int64][Math]::Floor($totalSeconds / 3600)
    $remainingSecondsAfterHours = $totalSeconds % 3600
    $minutes = [int64][Math]::Floor($remainingSecondsAfterHours / 60)
    $seconds = $remainingSecondsAfterHours % 60
    $milliseconds = $totalMilliseconds % 1000

    if ($hours -ge 1) {
        return ('{0} h {1} m {2} s {3} ms' -f $hours, $minutes, $seconds, $milliseconds)
    }

    if ($minutes -ge 1) {
        return ('{0} m {1} s {2} ms' -f $minutes, $seconds, $milliseconds)
    }

    if ($seconds -ge 1) {
        return ('{0} s {1} ms' -f $seconds, $milliseconds)
    }

    return ('{0} ms' -f $milliseconds)
}

function Format-TestDurationSeconds {
    param(
        [TimeSpan]$Duration
    )

    $totalSeconds = $Duration.TotalSeconds
    if ($totalSeconds -lt 0) {
        $totalSeconds = 0
    }

    return [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '{0:0.000} s', $totalSeconds)
}

function Format-CheckpointTimestamp {
    param(
        [datetime]$Timestamp
    )

    return $Timestamp.ToString('yyyy-MM-dd HH:mm:ss.fff')
}

function Format-CheckpointTimeShort {
    param(
        [datetime]$Timestamp
    )

    return $Timestamp.ToString('HH:mm:ss.fff')
}

function Write-PhaseMarker {
    param(
        [string]$Name,
        [datetime]$Timestamp
    )

    if (-not $script:PhaseEvents) {
        $script:PhaseEvents = [System.Collections.Generic.List[object]]::new()
    }

    $script:PhaseEvents.Add([pscustomobject]@{
        Name = $Name
        Timestamp = $Timestamp
    })

    Write-Host ('*' * 90) -ForegroundColor DarkGray
    Write-Host "PHASE: $Name | " -NoNewline -ForegroundColor Yellow
    Write-Host (Format-CheckpointTimeShort -Timestamp $Timestamp) -ForegroundColor Green
    Write-Host ('*' * 90) -ForegroundColor DarkGray
}

function New-Checkpoint {
    param(
        [string]$Name,
        [datetime]$Start,
        [datetime]$End,
        [string]$Source = 'Wrapper'
    )

    if ($End -lt $Start) {
        return $null
    }

    return [pscustomobject]@{
        Name = $Name
        Start = $Start
        End = $End
        Duration = $End - $Start
        Source = $Source
    }
}

function Get-TrxSummary {
    param(
        [string]$TrxPath,
        [string]$Label,
        [int]$ExitCode = 0,
        [datetime]$FallbackStart = [datetime]::MinValue,
        [datetime]$FallbackFinish = [datetime]::MinValue
    )

    if (-not (Test-Path $TrxPath)) {
        if ($ExitCode -ne 0 -and $FallbackStart -ne [datetime]::MinValue -and $FallbackFinish -ne [datetime]::MinValue) {
            return [pscustomobject]@{
                Label = $Label
                Total = 0
                Passed = 0
                Failed = 0
                Skipped = 0
                StartTime = $FallbackStart
                FinishTime = $FallbackFinish
                Duration = $FallbackFinish - $FallbackStart
                HasTrx = $false
                ExitCode = $ExitCode
                Note = 'TRX no generado'
            }
        }

        return $null
    }

    $trx = [xml](Get-Content $TrxPath)
    $counters = $trx.TestRun.ResultSummary.Counters
    $times = $trx.TestRun.Times

    $start = [datetimeoffset]$times.start
    $finish = [datetimeoffset]$times.finish

    return [pscustomobject]@{
        Label = $Label
        Total = [int]$counters.total
        Passed = [int]$counters.passed
        Failed = [int]$counters.failed
        Skipped = if ([string]::IsNullOrWhiteSpace([string]$counters.skipped)) { 0 } else { [int]$counters.skipped }
        StartTime = $start.LocalDateTime
        FinishTime = $finish.LocalDateTime
        Duration = $finish - $start
        HasTrx = $true
        ExitCode = $ExitCode
        Note = $null
    }
}

function Write-TestSummaryLine {
    param(
        [psobject]$Summary
    )

    if (-not $Summary) {
        return
    }

    if (-not $Summary.HasTrx) {
        Write-Host "$($Summary.Label) - " -NoNewline -ForegroundColor Magenta
        Write-Host "$($Summary.Note)" -ForegroundColor Red -NoNewline
        Write-Host ', ' -NoNewline
        Write-Host "ExitCode: $($Summary.ExitCode)" -ForegroundColor Red -NoNewline
        Write-Host ', ' -NoNewline
        Write-Host "Duracion: $(Format-TestDurationSeconds -Duration $Summary.Duration)" -ForegroundColor Gray
        return
    }

    Write-Host "$($Summary.Label) - " -NoNewline -ForegroundColor Magenta

    $passedColor = if ($Summary.Passed -gt 0) { 'Green' } else { 'White' }
    $skippedColor = if ($Summary.Skipped -gt 0) { 'Blue' } else { 'White' }
    $failedColor = if ($Summary.Failed -gt 0) { 'Red' } else { 'White' }

    Write-Host "Superado: $($Summary.Passed)" -ForegroundColor $passedColor -NoNewline
    Write-Host ', ' -NoNewline
    Write-Host "Omitido: $($Summary.Skipped)" -ForegroundColor $skippedColor -NoNewline
    Write-Host ', ' -NoNewline
    Write-Host "Con error: $($Summary.Failed)" -ForegroundColor $failedColor -NoNewline
    Write-Host ', ' -NoNewline
    Write-Host "Total: $($Summary.Total)" -ForegroundColor DarkYellow -NoNewline
    Write-Host ', ' -NoNewline
    Write-Host "Duracion: $(Format-TestDurationSeconds -Duration $Summary.Duration)" -ForegroundColor Gray
}

function Write-GeneralSummary {
    param(
        [System.Collections.IEnumerable]$Checkpoints,
        [System.Collections.IEnumerable]$PhaseEvents,
        [psobject]$UnitSummary,
        [psobject]$IntegrationSummary
    )

    Write-Host ('=' * 90) -ForegroundColor DarkGray
    Write-Host '[SUMMARY GENERAL]' -ForegroundColor Yellow
    Write-Host ('=' * 90) -ForegroundColor DarkGray

    Write-Host '  TRAMOS WRAPPER (SUMABLES):' -ForegroundColor Cyan

    foreach ($checkpoint in $Checkpoints) {
        if (-not $checkpoint) {
            continue
        }

        if ($checkpoint.Name -eq 'TOTAL') {
            Write-Host '  ' -NoNewline
            Write-Host '>>> ' -NoNewline -ForegroundColor Yellow
            Write-Host $checkpoint.Name -NoNewline -ForegroundColor Yellow -BackgroundColor DarkBlue
            Write-Host ' | ' -NoNewline -ForegroundColor Yellow
            Write-Host (Format-CheckpointTimeShort -Timestamp $checkpoint.Start) -NoNewline -ForegroundColor Cyan -BackgroundColor DarkBlue
            Write-Host ' | ' -NoNewline -ForegroundColor Yellow
            Write-Host (Format-CheckpointTimeShort -Timestamp $checkpoint.End) -NoNewline -ForegroundColor Cyan -BackgroundColor DarkBlue
            Write-Host ' | ' -NoNewline -ForegroundColor Yellow
            Write-Host (Format-TestDurationSeconds -Duration $checkpoint.Duration) -NoNewline -ForegroundColor White -BackgroundColor DarkBlue
            Write-Host ' | ' -NoNewline -ForegroundColor Yellow
            Write-Host (Format-TestDuration -Duration $checkpoint.Duration) -NoNewline -ForegroundColor White -BackgroundColor DarkBlue
            Write-Host ' <<<' -ForegroundColor Yellow
        }
        else {
            Write-Host '  ' -NoNewline
            Write-Host $checkpoint.Name -NoNewline -ForegroundColor White
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-CheckpointTimeShort -Timestamp $checkpoint.Start) -NoNewline -ForegroundColor Green
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-CheckpointTimeShort -Timestamp $checkpoint.End) -NoNewline -ForegroundColor Green
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-TestDurationSeconds -Duration $checkpoint.Duration) -NoNewline -ForegroundColor Cyan
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-TestDuration -Duration $checkpoint.Duration) -ForegroundColor Gray
        }
    }

    $totalCheckpoint = $Checkpoints | Where-Object { $_ -and $_.Name -eq 'TOTAL' } | Select-Object -First 1
    $sumCheckpoints = [timespan]::Zero
    foreach ($checkpoint in $Checkpoints) {
        if (-not $checkpoint -or $checkpoint.Name -eq 'TOTAL') {
            continue
        }

        $sumCheckpoints += $checkpoint.Duration
    }

    if ($totalCheckpoint) {
        $delta = $totalCheckpoint.Duration - $sumCheckpoints
        $deltaMilliseconds = [int64][Math]::Round($delta.TotalMilliseconds)

        if ($deltaMilliseconds -gt 1000) {
            Write-Host '  ' -ForegroundColor White
            Write-Host '  VALIDACION (DELTA > 1s):' -ForegroundColor Yellow
            Write-Host '    Diferencia detectada: ' -NoNewline -ForegroundColor White
            Write-Host (Format-TestDurationSeconds -Duration $delta) -ForegroundColor Cyan
        }
    }

    if ($UnitSummary -or $IntegrationSummary) {
        Write-Host '  ' -ForegroundColor White
        Write-Host '  REFERENCIA TRX (NO SUMA AL TOTAL WRAPPER):' -ForegroundColor Cyan

        if ($UnitSummary) {
            Write-Host '    UNITARIAS TRX | ' -NoNewline -ForegroundColor White
            Write-Host (Format-CheckpointTimeShort -Timestamp $UnitSummary.StartTime) -NoNewline -ForegroundColor Green
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-CheckpointTimeShort -Timestamp $UnitSummary.FinishTime) -NoNewline -ForegroundColor Green
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-TestDurationSeconds -Duration $UnitSummary.Duration) -NoNewline -ForegroundColor Cyan
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-TestDuration -Duration $UnitSummary.Duration) -ForegroundColor Gray
        }

        if ($IntegrationSummary) {
            Write-Host '    INTEGRACION TRX | ' -NoNewline -ForegroundColor White
            Write-Host (Format-CheckpointTimeShort -Timestamp $IntegrationSummary.StartTime) -NoNewline -ForegroundColor Green
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-CheckpointTimeShort -Timestamp $IntegrationSummary.FinishTime) -NoNewline -ForegroundColor Green
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-TestDurationSeconds -Duration $IntegrationSummary.Duration) -NoNewline -ForegroundColor Cyan
            Write-Host ' | ' -NoNewline -ForegroundColor DarkGray
            Write-Host (Format-TestDuration -Duration $IntegrationSummary.Duration) -ForegroundColor Gray
        }
    }

    Write-Host ('=' * 90) -ForegroundColor DarkGray
}

function Format-CoveragePercent {
    param(
        [double]$Value
    )

    return [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '{0:0.00}%', $Value)
}

function Get-CoverageSummaryFromReportGenerator {
    param(
        [string]$SummaryPath
    )

    if (-not (Test-Path $SummaryPath)) {
        return $null
    }

    $content = Get-Content -Path $SummaryPath -ErrorAction SilentlyContinue
    if (-not $content) {
        return $null
    }

    $lineCovered = 0
    $lineUncovered = 0
    $lineCoverable = 0
    $lineTotal = 0
    $branchCovered = 0
    $branchTotal = 0

    foreach ($line in $content) {
        if ($line -match '^\s*Covered lines:\s*(\d+)') {
            $lineCovered = [int]$matches[1]
        }
        elseif ($line -match '^\s*Uncovered lines:\s*(\d+)') {
            $lineUncovered = [int]$matches[1]
        }
        elseif ($line -match '^\s*Coverable lines:\s*(\d+)') {
            $lineCoverable = [int]$matches[1]
        }
        elseif ($line -match '^\s*Total lines:\s*(\d+)') {
            $lineTotal = [int]$matches[1]
        }
        elseif ($line -match '^\s*Covered branches:\s*(\d+)') {
            $branchCovered = [int]$matches[1]
        }
        elseif ($line -match '^\s*Total branches:\s*(\d+)') {
            $branchTotal = [int]$matches[1]
        }
    }

    if ($lineCoverable -le 0 -and $branchTotal -le 0) {
        return $null
    }

    $linePercent = if ($lineCoverable -gt 0) { ($lineCovered * 100.0) / $lineCoverable } else { 0.0 }
    $branchUncovered = [Math]::Max(0, $branchTotal - $branchCovered)
    $branchPercent = if ($branchTotal -gt 0) { ($branchCovered * 100.0) / $branchTotal } else { 0.0 }

    return [pscustomobject]@{
        Source = 'ReportGenerator'
        LineCovered = $lineCovered
        LineUncovered = $lineUncovered
        LineCoverable = $lineCoverable
        LineTotal = $lineTotal
        LinePercent = $linePercent
        BranchCovered = $branchCovered
        BranchUncovered = $branchUncovered
        BranchCoverable = $branchTotal
        BranchTotal = $branchTotal
        BranchPercent = $branchPercent
    }
}

function Get-CoverageSummaryFromCobertura {
    param(
        [System.Collections.IEnumerable]$CoverageFiles
    )

    if (-not $CoverageFiles) {
        return $null
    }

    $lineCovered = 0
    $lineCoverable = 0
    $branchCovered = 0
    $branchCoverable = 0

    foreach ($file in $CoverageFiles) {
        try {
            $xml = [xml](Get-Content -Path $file.FullName -Raw -ErrorAction Stop)
            $coverageNode = $xml.coverage

            if ($coverageNode.'lines-covered') {
                $lineCovered += [int]$coverageNode.'lines-covered'
            }

            if ($coverageNode.'lines-valid') {
                $lineCoverable += [int]$coverageNode.'lines-valid'
            }

            if ($coverageNode.'branches-covered') {
                $branchCovered += [int]$coverageNode.'branches-covered'
            }

            if ($coverageNode.'branches-valid') {
                $branchCoverable += [int]$coverageNode.'branches-valid'
            }
        }
        catch {
            # Ignore malformed or unreadable coverage files and continue aggregating others.
            continue
        }
    }

    if ($lineCoverable -le 0 -and $branchCoverable -le 0) {
        return $null
    }

    $lineUncovered = [Math]::Max(0, $lineCoverable - $lineCovered)
    $linePercent = if ($lineCoverable -gt 0) { ($lineCovered * 100.0) / $lineCoverable } else { 0.0 }

    $branchUncovered = [Math]::Max(0, $branchCoverable - $branchCovered)
    $branchPercent = if ($branchCoverable -gt 0) { ($branchCovered * 100.0) / $branchCoverable } else { 0.0 }

    return [pscustomobject]@{
        Source = 'Cobertura'
        LineCovered = $lineCovered
        LineUncovered = $lineUncovered
        LineCoverable = $lineCoverable
        LineTotal = $lineCoverable
        LinePercent = $linePercent
        BranchCovered = $branchCovered
        BranchUncovered = $branchUncovered
        BranchCoverable = $branchCoverable
        BranchTotal = $branchCoverable
        BranchPercent = $branchPercent
    }
}

function Remove-DataSeedsFromCoverage {
    param(
        [string[]]$CoverageFiles
    )

    if (-not $CoverageFiles) {
        return
    }

    foreach ($coverageFile in $CoverageFiles) {
        if (-not (Test-Path $coverageFile)) {
            continue
        }

        try {
            # Load and parse the XML
            [xml]$coberturaXml = Get-Content -Path $coverageFile
            
            # Find and remove all classes with name containing "Data.Seeds"
            $classesToRemove = $coberturaXml.SelectNodes("//class[contains(@name, 'Data.Seeds')]")
            
            foreach ($class in $classesToRemove) {
                $class.ParentNode.RemoveChild($class) | Out-Null
            }
            
            # Save the modified XML
            $coberturaXml.Save($coverageFile)
            $removedCount = $classesToRemove.Count
            Write-Host "  Removed $removedCount Data.Seeds entries from: $([System.IO.Path]::GetFileName($coverageFile))" -ForegroundColor Gray
        }
        catch {
            Write-Host "  WARNING: Could not process coverage file: $coverageFile" -ForegroundColor Yellow
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

function Write-CoverageSummary {
    param(
        [psobject]$CoverageSummary
    )

    if (-not $CoverageSummary) {
        return
    }

    $lineCoverageColor = if ($CoverageSummary.LinePercent -gt 0) { 'Green' } else { 'Red' }
    $branchCoverageColor = if ($CoverageSummary.BranchPercent -gt 0) { 'Green' } else { 'Red' }
    $lineCoveredColor = if ($CoverageSummary.LineCovered -gt 0) { 'Blue' } else { 'White' }
    $branchCoveredColor = if ($CoverageSummary.BranchCovered -gt 0) { 'Blue' } else { 'White' }
    $lineUncoveredColor = if ($CoverageSummary.LineUncovered -gt 0) { 'Red' } else { 'White' }
    $branchUncoveredColor = if ($CoverageSummary.BranchUncovered -gt 0) { 'Red' } else { 'White' }

    Write-Host 'Line - ' -NoNewline -ForegroundColor Magenta
    Write-Host "Coverage: $(Format-CoveragePercent -Value $CoverageSummary.LinePercent)" -NoNewline -ForegroundColor $lineCoverageColor
    Write-Host ', ' -NoNewline
    Write-Host "Covered: $($CoverageSummary.LineCovered)" -NoNewline -ForegroundColor $lineCoveredColor
    Write-Host ', ' -NoNewline
    Write-Host "Uncovered: $($CoverageSummary.LineUncovered)" -NoNewline -ForegroundColor $lineUncoveredColor
    Write-Host ', ' -NoNewline
    Write-Host "Coverable: $($CoverageSummary.LineCoverable)" -NoNewline -ForegroundColor DarkYellow
    Write-Host ', ' -NoNewline
    Write-Host "Total: $($CoverageSummary.LineTotal)" -ForegroundColor Gray

    Write-Host 'Branch - ' -NoNewline -ForegroundColor Magenta
    Write-Host "Coverage: $(Format-CoveragePercent -Value $CoverageSummary.BranchPercent)" -NoNewline -ForegroundColor $branchCoverageColor
    Write-Host ', ' -NoNewline
    Write-Host "Covered: $($CoverageSummary.BranchCovered)" -NoNewline -ForegroundColor $branchCoveredColor
    Write-Host ', ' -NoNewline
    Write-Host "Uncovered: $($CoverageSummary.BranchUncovered)" -NoNewline -ForegroundColor $branchUncoveredColor
    Write-Host ', ' -NoNewline
    Write-Host "Coverable: $($CoverageSummary.BranchCoverable)" -NoNewline -ForegroundColor DarkYellow
    Write-Host ', ' -NoNewline
    Write-Host "Total: $($CoverageSummary.BranchTotal)" -ForegroundColor Gray
}

function Get-TestSummaryPlainText {
    param(
        [psobject]$Summary
    )

    if (-not $Summary) {
        return $null
    }

    if (-not $Summary.HasTrx) {
        return "$($Summary.Label) - $($Summary.Note), ExitCode: $($Summary.ExitCode), Duracion: $(Format-TestDurationSeconds -Duration $Summary.Duration)"
    }

    return "$($Summary.Label) - Superado: $($Summary.Passed), Omitido: $($Summary.Skipped), Con error: $($Summary.Failed), Total: $($Summary.Total), Duracion: $(Format-TestDurationSeconds -Duration $Summary.Duration)"
}

function Get-CoverageSummaryPlainText {
    param(
        [psobject]$CoverageSummary
    )

    if (-not $CoverageSummary) {
        return $null
    }

    $lines = @(
        "Source: $($CoverageSummary.Source)",
        "Line - Coverage: $(Format-CoveragePercent -Value $CoverageSummary.LinePercent), Covered: $($CoverageSummary.LineCovered), Uncovered: $($CoverageSummary.LineUncovered), Coverable: $($CoverageSummary.LineCoverable), Total: $($CoverageSummary.LineTotal)",
        "Branch - Coverage: $(Format-CoveragePercent -Value $CoverageSummary.BranchPercent), Covered: $($CoverageSummary.BranchCovered), Uncovered: $($CoverageSummary.BranchUncovered), Coverable: $($CoverageSummary.BranchCoverable), Total: $($CoverageSummary.BranchTotal)"
    )

    return $lines -join "`r`n"
}

function Get-GeneralSummaryPlainText {
    param(
        [System.Collections.IEnumerable]$Checkpoints,
        [psobject]$UnitSummary,
        [psobject]$IntegrationSummary
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('  TRAMOS WRAPPER (SUMABLES):')

    foreach ($checkpoint in $Checkpoints) {
        if (-not $checkpoint) {
            continue
        }

        if ($checkpoint.Name -eq 'TOTAL') {
            $lines.Add("  >>> $($checkpoint.Name) | $(Format-CheckpointTimeShort -Timestamp $checkpoint.Start) | $(Format-CheckpointTimeShort -Timestamp $checkpoint.End) | $(Format-TestDurationSeconds -Duration $checkpoint.Duration) | $(Format-TestDuration -Duration $checkpoint.Duration) <<<")
        }
        else {
            $lines.Add("  $($checkpoint.Name) | $(Format-CheckpointTimeShort -Timestamp $checkpoint.Start) | $(Format-CheckpointTimeShort -Timestamp $checkpoint.End) | $(Format-TestDurationSeconds -Duration $checkpoint.Duration) | $(Format-TestDuration -Duration $checkpoint.Duration)")
        }
    }

    if ($UnitSummary -or $IntegrationSummary) {
        $lines.Add('  ')
        $lines.Add('  REFERENCIA TRX (NO SUMA AL TOTAL WRAPPER):')

        if ($UnitSummary) {
            $lines.Add("    UNITARIAS TRX | $(Format-CheckpointTimeShort -Timestamp $UnitSummary.StartTime) | $(Format-CheckpointTimeShort -Timestamp $UnitSummary.FinishTime) | $(Format-TestDurationSeconds -Duration $UnitSummary.Duration) | $(Format-TestDuration -Duration $UnitSummary.Duration)")
        }

        if ($IntegrationSummary) {
            $lines.Add("    INTEGRACION TRX | $(Format-CheckpointTimeShort -Timestamp $IntegrationSummary.StartTime) | $(Format-CheckpointTimeShort -Timestamp $IntegrationSummary.FinishTime) | $(Format-TestDurationSeconds -Duration $IntegrationSummary.Duration) | $(Format-TestDuration -Duration $IntegrationSummary.Duration)")
        }
    }

    return ($lines -join "`r`n")
}

# Aviso por correo al terminar CUALQUIER runsettings, opcional: solo se activa si existe
# runsettings/appsettings.json (gitignored, ver appsettings.template.json). Sin ese archivo la
# funcion no hace nada -- no afecta a otros forks/desarrolladores que no lo configuren.
function Send-TestNotificationEmail {
    param(
        [string]$RunsettingsType,
        [int]$ExitCode,
        [timespan]$Duration,
        [psobject]$UnitSummary,
        [psobject]$IntegrationSummary,
        [psobject]$CoverageSummary,
        [System.Collections.IEnumerable]$Checkpoints,
        [string]$ReportDir,
        [string]$HtmlIndexPath,
        [switch]$WithoutAttachments
    )

    $configPath = "$rootPath\runsettings\appsettings.json"
    if (-not (Test-Path $configPath)) {
        return
    }

    # Registro dedicado del intento de envio: independiente de la consola, para que un fallo no se
    # pierda entre el volumen de salida de perfiles largos (ci-full/performance).
    $notificationLogPath = Join-Path $ReportDir 'email-notification.log'
    function Write-EmailNotificationLog {
        param([string]$Message)
        $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
        Add-Content -Path $notificationLogPath -Value $line
    }

    # Tope de tamano total de adjuntos: algunos relays SMTP rechazan el mensaje completo (sin aviso
    # claro) si se pasan de su limite. Si el conjunto de artifacts lo supera, se envia el resumen de
    # todas formas, sin adjuntos, en vez de perder el aviso por completo.
    $maxAttachmentsBytes = 15MB

    try {
        $config = Get-Content -Path $configPath -Raw | ConvertFrom-Json
        $mail = $config.Mail

        if (-not $mail -or -not $mail.Smtp -or -not $mail.ToDev -or [string]::IsNullOrWhiteSpace($mail.ToDev.Email)) {
            Write-Host '[NOTE] runsettings/appsettings.json incompleto (falta Mail:Smtp o Mail:ToDev:Email); se omite el aviso por correo.' -ForegroundColor Yellow
            return
        }

        $status = if ($ExitCode -eq 0) { 'EXITO' } else { 'FALLO' }
        $subject = "Tests '$RunsettingsType' - $(Format-TestDuration -Duration $Duration) - $status - [AutoDiscoveryIAM]"

        $bodyLines = New-Object System.Collections.Generic.List[string]
        $bodyLines.Add(('=' * 90))
        $bodyLines.Add('TEST RUN COMPLETED')
        $bodyLines.Add("Exit Code: $ExitCode")
        $bodyLines.Add("Duration: $(Format-TestDurationSeconds -Duration $Duration) | $(Format-TestDuration -Duration $Duration)")
        $bodyLines.Add('[NOTE] Diferencias de pocos ms frente a TRX son normales: TRX mide la ventana interna del runner de pruebas, mientras Wrapper mide el flujo completo (inicio/fin del comando, IO y cierre).')
        $bodyLines.Add(('=' * 90))
        $bodyLines.Add('')
        $bodyLines.Add(('=' * 90))
        $bodyLines.Add('[SUMMARY]')
        $bodyLines.Add(('=' * 90))
        $bodyLines.Add('TEST')

        $unitText = Get-TestSummaryPlainText -Summary $UnitSummary
        if ($unitText) {
            $bodyLines.Add($unitText)
        }

        $integrationText = Get-TestSummaryPlainText -Summary $IntegrationSummary
        if ($integrationText) {
            $bodyLines.Add($integrationText)
        }

        if ($CoverageSummary) {
            $bodyLines.Add('COVERAGE')
            $bodyLines.Add((Get-CoverageSummaryPlainText -CoverageSummary $CoverageSummary))
        }

        $bodyLines.Add('')
        $bodyLines.Add(('=' * 90))
        $bodyLines.Add('[SUMMARY GENERAL]')
        $bodyLines.Add(('=' * 90))
        $bodyLines.Add((Get-GeneralSummaryPlainText -Checkpoints $Checkpoints -UnitSummary $UnitSummary -IntegrationSummary $IntegrationSummary))
        $bodyLines.Add(('=' * 90))
        $bodyLines.Add('')
        $bodyLines.Add("Exit code: $ExitCode")
        $bodyLines.Add("Reporte HTML: $HtmlIndexPath")
        $bodyLines.Add("Carpeta de resultados: $ReportDir")

        $body = ($bodyLines -join "`r`n")

        # Adjuntos: mismos artifacts que ya se listan en consola, usando su ruta física real.
        # El reporte HTML de cobertura es una carpeta completa (index.html + assets) -- se comprime
        # a un .zip autocontenido para poder adjuntarlo como un solo archivo.
        # -WithoutAttachments: manda solo el resumen en texto -- util para diagnosticar si el relay
        # SMTP esta descartando el correo por tamano/adjuntos, no por el contenido en si.
        $attachmentPaths = New-Object System.Collections.Generic.List[string]

        if ($WithoutAttachments) {
            $bodyLines.Add('')
            $bodyLines.Add('[NOTE] Corrida con -WithoutAttachments: sin adjuntos a proposito. Artifacts disponibles en la carpeta de resultados de arriba.')
            $body = ($bodyLines -join "`r`n")
        }
        else {
            $logFilePath = Join-Path $ReportDir 'execution.log'
            $unitTrxPath = Join-Path $ReportDir 'results-unit.trx'
            $integrationTrxPath = Join-Path $ReportDir 'results-integration.trx'
            $vulnerablePackagesPath = Join-Path $ReportDir 'vulnerable-packages.log'

            foreach ($candidate in @($logFilePath, $unitTrxPath, $integrationTrxPath, $vulnerablePackagesPath)) {
                if (Test-Path $candidate) {
                    $attachmentPaths.Add($candidate)
                }
            }

            # $HtmlIndexPath (y su carpeta) puede existir vacio incluso sin cobertura (se crea en
            # -NotBeNull antes de correr los tests) -- solo comprimir/adjuntar si el reporte real existe.
            if ($HtmlIndexPath -and (Test-Path $HtmlIndexPath)) {
                $htmlReportDir = Split-Path -Path $HtmlIndexPath -Parent
                $htmlZipPath = Join-Path $ReportDir 'coverage-html-report.zip'
                Compress-Archive -Path "$htmlReportDir\*" -DestinationPath $htmlZipPath -Force
                $attachmentPaths.Add($htmlZipPath)
            }
        }

        $totalAttachmentsBytes = if ($attachmentPaths.Count -gt 0) { ($attachmentPaths | ForEach-Object { (Get-Item $_).Length } | Measure-Object -Sum).Sum } else { 0 }
        if ($totalAttachmentsBytes -gt $maxAttachmentsBytes) {
            Write-EmailNotificationLog "Adjuntos omitidos: $([Math]::Round($totalAttachmentsBytes / 1MB, 2)) MB supera el tope de $([Math]::Round($maxAttachmentsBytes / 1MB, 2)) MB."
            $bodyLines.Add('')
            $bodyLines.Add("[NOTE] Adjuntos omitidos: $([Math]::Round($totalAttachmentsBytes / 1MB, 2)) MB supera el tope de $([Math]::Round($maxAttachmentsBytes / 1MB, 2)) MB configurado. Revisa los artifacts directamente en: $ReportDir")
            $body = ($bodyLines -join "`r`n")
            $attachmentPaths.Clear()
        }

        $smtpClient = New-Object System.Net.Mail.SmtpClient($mail.Smtp.Host, [int]$mail.Smtp.Port)
        $smtpClient.EnableSsl = [bool]$mail.Smtp.UseSsl
        $smtpClient.Credentials = New-Object System.Net.NetworkCredential($mail.Smtp.User, $mail.Smtp.Password)

        $mailMessage = New-Object System.Net.Mail.MailMessage
        try {
            $mailMessage.From = New-Object System.Net.Mail.MailAddress($mail.From.Email, $mail.From.Name)
            $mailMessage.To.Add((New-Object System.Net.Mail.MailAddress($mail.ToDev.Email, $mail.ToDev.Name)))
            $mailMessage.Subject = $subject
            $mailMessage.Body = $body
            $mailMessage.IsBodyHtml = $false

            foreach ($attachmentPath in $attachmentPaths) {
                $mailMessage.Attachments.Add((New-Object System.Net.Mail.Attachment($attachmentPath)))
            }

            $smtpClient.Send($mailMessage)
            Write-Host "[NOTE] Aviso por correo enviado a $($mail.ToDev.Email) con $($attachmentPaths.Count) adjunto(s)." -ForegroundColor Green
            Write-EmailNotificationLog "OK: enviado a $($mail.ToDev.Email) con $($attachmentPaths.Count) adjunto(s), $([Math]::Round($totalAttachmentsBytes / 1MB, 2)) MB. Subject: $subject"
        }
        finally {
            $mailMessage.Dispose()
        }
    }
    catch {
        Write-Host "[NOTE] No se pudo enviar el aviso por correo: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-EmailNotificationLog "ERROR: $($_.Exception.ToString())"
    }
}

# Display header
Write-Host ('=' * 90) -ForegroundColor Cyan
Write-Host "TEST RUN: $RunsettingsType" -ForegroundColor Cyan
Write-Host "Time: $timestamp" -ForegroundColor Cyan
Write-Host "Output: $reportDir" -ForegroundColor Cyan
Write-Host ('=' * 90) -ForegroundColor Cyan
Write-Host ""

# Build shared dotnet test arguments (project path added per-run below)
$baseArgs = @(
    "test",
    "--settings", $runsettingsPath,
    "--results-directory", $reportDir,
    "--verbosity", $Verbosity,
    "--logger", "console;verbosity=$Verbosity"
)

$unitTestsPath = "$rootPath\tests\AutoDiscoveryIAM.UnitTests"
$integrationTestsPath = "$rootPath\tests\AutoDiscoveryIAM.IntegrationTests"

if ($NoBuild) {
    $baseArgs += "--no-build"
}
else {
    # Coverlet parchea los DLLs in-place antes de cada test run y los restaura al terminar.
    # Si el proceso es interrumpido, el DLL parcheado queda en disco.
    # Un build incremental (el default de dotnet test) lo reutiza sin reparchar => cobertura vacía.
    # Solución: siempre forzar --no-incremental para garantizar binarios limpios, y pasar --no-build a dotnet test.
    Write-Host "[BUILD] Compilando proyectos con --no-incremental para garantizar binarios limpios (coverlet)..." -ForegroundColor Cyan
    & dotnet build $unitTestsPath "--no-incremental" "-v" "minimal" 2>&1 | ForEach-Object { Write-Host $_ }
    & dotnet build $integrationTestsPath "--no-incremental" "-v" "minimal" 2>&1 | ForEach-Object { Write-Host $_ }
    $baseArgs += "--no-build"
    Write-Host ""
}

# Force UTF-8 so dotnet.exe output is decoded correctly (fixes accents in Spanish Windows)
$prevOutputEncoding = [Console]::OutputEncoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$prevPSEncoding = $OutputEncoding
$OutputEncoding = [System.Text.Encoding]::UTF8

$startTime = Get-Date
$script:PhaseEvents = [System.Collections.Generic.List[object]]::new()
$logWriter = [System.IO.StreamWriter]::new($logFile, $false, [System.Text.UTF8Encoding]::new($false))
$logWriter.AutoFlush = $true

# --- Unit tests first ---
$unitCommandStart = Get-Date
Write-PhaseMarker -Name 'INICIO UNITARIAS' -Timestamp $unitCommandStart
Write-Host ""
$unitArgs = $baseArgs + @($unitTestsPath, "--logger", "trx;LogFileName=$unitTrxFile")
& dotnet @unitArgs 2>&1 | ForEach-Object {
    $line = [string]$_
    Write-Host $line
    $logWriter.WriteLine($line)
}
$unitExitCode = $LASTEXITCODE
$unitCommandEnd = Get-Date
Write-PhaseMarker -Name 'FIN UNITARIAS' -Timestamp $unitCommandEnd

# --- Integration tests second ---
$integrationCommandStart = Get-Date
Write-Host ""
Write-PhaseMarker -Name 'INICIO INTEGRACION' -Timestamp $integrationCommandStart
Write-Host ""
$integrationArgs = $baseArgs + @($integrationTestsPath, "--logger", "trx;LogFileName=$integrationTrxFile")
& dotnet @integrationArgs 2>&1 | ForEach-Object {
    $line = [string]$_
    Write-Host $line
    $logWriter.WriteLine($line)
}
$integrationExitCode = $LASTEXITCODE
$integrationCommandEnd = Get-Date
Write-PhaseMarker -Name 'FIN INTEGRACION' -Timestamp $integrationCommandEnd

# --- Escaneo de dependencias vulnerables (solo perfil "security") ---
if ($RunsettingsType -eq "security") {
    Write-Host ""
    Write-PhaseMarker -Name 'ESCANEO DE DEPENDENCIAS VULNERABLES' -Timestamp (Get-Date)
    Write-Host ""

    $vulnerablePackagesLog = "$reportDir\vulnerable-packages.log"
    & dotnet list "$rootPath\AutoDiscoveryIAM.sln" package --vulnerable --include-transitive 2>&1 | ForEach-Object {
        $line = [string]$_
        Write-Host $line
        $logWriter.WriteLine($line)
        Add-Content -Path $vulnerablePackagesLog -Value $line
    }
    Write-Host ""
    Write-Host "  Resultado del escaneo: $vulnerablePackagesLog" -ForegroundColor White
}

$logFinalizeStart = Get-Date
$logWriter.Flush()
$logWriter.Dispose()
$logFinalizeEnd = Get-Date

$testExitCode = if ($unitExitCode -ne 0 -or $integrationExitCode -ne 0) { 1 } else { 0 }

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host ('=' * 90) -ForegroundColor Cyan
Write-Host 'TEST RUN COMPLETED' -ForegroundColor Cyan
$exitCodeColor = if ($testExitCode -eq 0) { 'Green' } else { 'Red' }
Write-Host "Exit Code: $testExitCode" -ForegroundColor $exitCodeColor
Write-Host "Duration: $(Format-TestDurationSeconds -Duration $duration) | $(Format-TestDuration -Duration $duration)" -ForegroundColor Cyan
Write-Host '[NOTE] Diferencias de pocos ms frente a TRX son normales: TRX mide la ventana interna del runner de pruebas, mientras Wrapper mide el flujo completo (inicio/fin del comando, IO y cierre).' -ForegroundColor DarkYellow
Write-Host ('=' * 90) -ForegroundColor Cyan
Write-Host ""

# Parse and display colored summary from log
$unitSummary = Get-TrxSummary -TrxPath $unitTrxFile -Label 'Unit' -ExitCode $unitExitCode -FallbackStart $unitCommandStart -FallbackFinish $unitCommandEnd
$integrationSummary = Get-TrxSummary -TrxPath $integrationTrxFile -Label 'Integration' -ExitCode $integrationExitCode -FallbackStart $integrationCommandStart -FallbackFinish $integrationCommandEnd

$generalCheckpoints = [System.Collections.Generic.List[object]]::new()

$generalCheckpoints.Add((New-Checkpoint -Name 'PREPARACION' -Start $startTime -End $unitCommandStart -Source 'Wrapper'))
$generalCheckpoints.Add((New-Checkpoint -Name 'UNITARIAS' -Start $unitCommandStart -End $unitCommandEnd -Source 'Wrapper'))
$generalCheckpoints.Add((New-Checkpoint -Name 'TRANSICION' -Start $unitCommandEnd -End $integrationCommandStart -Source 'Wrapper'))
$generalCheckpoints.Add((New-Checkpoint -Name 'INTEGRACION' -Start $integrationCommandStart -End $integrationCommandEnd -Source 'Wrapper'))
$generalCheckpoints.Add((New-Checkpoint -Name 'POST INTEGRACION' -Start $integrationCommandEnd -End $logFinalizeStart -Source 'Wrapper'))
$generalCheckpoints.Add((New-Checkpoint -Name 'CIERRE LOG' -Start $logFinalizeStart -End $logFinalizeEnd -Source 'Wrapper'))
$generalCheckpoints.Add((New-Checkpoint -Name 'CIERRE FINAL' -Start $logFinalizeEnd -End $endTime -Source 'Wrapper'))
$generalCheckpoints.Add((New-Checkpoint -Name 'TOTAL' -Start $startTime -End $endTime -Source 'Wrapper'))

# Find and list coverage files
$coverageFiles = Get-ChildItem -Path $reportDir -Include "*.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
if (-not $coverageFiles) {
    $coverageFiles = @()
}

# Generate HTML report if coverage files exist and reportgenerator is available
$reportGenerator = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
if ($coverageFiles -and $reportGenerator) {
    Write-Host '[GENERATING HTML COVERAGE REPORT]' -ForegroundColor Cyan
    
    # Remove Data.Seeds entries from coverage XML before generating report
    Write-Host '  Cleaning coverage data (removing Data.Seeds entries)...' -ForegroundColor Cyan
    Remove-DataSeedsFromCoverage -CoverageFiles ($coverageFiles | ForEach-Object { $_.FullName })
    Write-Host '[NOTE] Se excluyen Data.Seeds del reporte para mantener la cobertura enfocada en logica de negocio; esta limpieza es extensible para agregar futuras exclusiones tecnicas conforme crezca la aplicacion.' -ForegroundColor DarkYellow
    
    $coverageReportsArg = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
    $fileFiltersArg = "-**/Migrations/*.cs;-**/Migrations/**/*.cs;-**/*ModelSnapshot.cs;-**/Docs/**/*.cs;-**/Data/Seeds/**/*.cs;-**/*ExampleSwagger*.cs;-**/*OpenApi*.cs"
    
    & $reportGenerator.Source `
        "-reports:$coverageReportsArg" `
        "-targetdir:$htmlReportDir" `
        "-reporttypes:Html;HtmlSummary;TextSummary" `
        "-sourcedirs:$rootPath" `
        "-filefilters:$fileFiltersArg" | Out-Null
    
    if (Test-Path $htmlIndexPath) {
        Write-Host "  HTML Report generated: $htmlIndexPath" -ForegroundColor Green
    }
}
elseif ($coverageFiles) {
    Write-Host "[NOTE] reportgenerator not found in PATH. Skipping HTML report generation." -ForegroundColor Yellow
}

$coverageSummary = $null
$summaryTxtPath = "$htmlReportDir\Summary.txt"
if (Test-Path $summaryTxtPath) {
    $coverageSummary = Get-CoverageSummaryFromReportGenerator -SummaryPath $summaryTxtPath
}

if (-not $coverageSummary -and $coverageFiles.Count -eq 1) {
    $coverageSummary = Get-CoverageSummaryFromCobertura -CoverageFiles $coverageFiles
}

Write-Host ('=' * 90) -ForegroundColor DarkGray
Write-Host '[SUMMARY]' -ForegroundColor Yellow
Write-Host ('=' * 90) -ForegroundColor DarkGray
Write-Host 'TEST' -ForegroundColor Yellow
Write-TestSummaryLine -Summary $unitSummary
Write-TestSummaryLine -Summary $integrationSummary

if ($coverageSummary) {
    Write-Host 'COVERAGE' -ForegroundColor Yellow
    Write-Host "Source: $($coverageSummary.Source)" -ForegroundColor DarkGray
    Write-CoverageSummary -CoverageSummary $coverageSummary
}
elseif ($coverageFiles.Count -gt 1 -and -not $reportGenerator) {
    Write-Host 'COVERAGE' -ForegroundColor Yellow
    Write-Host '[NOTE] No se pudo calcular coverage consolidado sin ReportGenerator (hay multiples archivos de cobertura).' -ForegroundColor Yellow
}

Write-Host ""
Write-GeneralSummary -Checkpoints $generalCheckpoints -PhaseEvents $script:PhaseEvents -UnitSummary $unitSummary -IntegrationSummary $integrationSummary

Write-Host ""
Write-Host '[ARTIFACTS]' -ForegroundColor Yellow
Write-Host "  Execution Log:    $logFile" -ForegroundColor White
Write-Host "  Unit TRX:         $unitTrxFile" -ForegroundColor White
Write-Host "  Integration TRX:  $integrationTrxFile" -ForegroundColor White
Write-Host "  Results Dir:      $reportDir" -ForegroundColor White
if ($coverageFiles.Count -gt 0) {
    Write-Host "  Coverage Files: $($coverageFiles.Count) found" -ForegroundColor Green
}

Write-Host ""

# Open HTML report in browser if requested
if ($OpenReportInBrowser) {
    if (Test-Path $htmlIndexPath) {
        Write-Host 'Opening HTML report in browser...' -ForegroundColor Cyan
        
        if ([string]::IsNullOrWhiteSpace($Browser)) {
            Start-Process -FilePath $htmlIndexPath
        }
        else {
            Start-Process -FilePath $Browser -ArgumentList "`"$htmlIndexPath`""
        }
    }
    elseif ($coverageFiles) {
        Write-Host '[NOTE] HTML report not generated yet. Either reportgenerator was not available or report generation failed.' -ForegroundColor Yellow
    }
    else {
        Write-Host '[NOTE] No coverage files found. Cannot generate HTML report.' -ForegroundColor Yellow
    }
}

Write-Host ""
$exitColor = if ($testExitCode -eq 0) { 'Green' } else { 'Red' }
Write-Host "Exit code: $testExitCode" -ForegroundColor $exitColor

# El envio NO se condiciona al resultado de los tests -- se manda igual en exito o en fallo
# (el asunto/cuerpo ya indican EXITO/FALLO); -WithEmailReport es lo unico que decide si se manda.
if ($WithEmailReport) {
    Send-TestNotificationEmail `
        -RunsettingsType $RunsettingsType `
        -ExitCode $testExitCode `
        -Duration $duration `
        -UnitSummary $unitSummary `
        -IntegrationSummary $integrationSummary `
        -CoverageSummary $coverageSummary `
        -Checkpoints $generalCheckpoints `
        -ReportDir $reportDir `
        -HtmlIndexPath $htmlIndexPath `
        -WithoutAttachments:$WithoutAttachments
}

# Restore previous encoding
[Console]::OutputEncoding = $prevOutputEncoding
$OutputEncoding = $prevPSEncoding

# Return exit code
exit $testExitCode
