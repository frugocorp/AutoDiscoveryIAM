# Script de automatización de coverage
Write-Host "= COBERTURA DE CODIGO =" -ForegroundColor Green

$canonicalCoveragePath = "runsettings/coverage"
$legacyCoveragePath = "runsettings/runsettings/coverage"

# Limpiar coverage anterior si existe
if (Test-Path $canonicalCoveragePath) {
    Write-Host "Limpiando coverage anterior..." -ForegroundColor Yellow
    Remove-Item -Path $canonicalCoveragePath -Recurse -Force
}

# Limpiar ruta legacy si existe
if (Test-Path $legacyCoveragePath) {
    Write-Host "Limpiando ruta legacy de coverage..." -ForegroundColor Yellow
    Remove-Item -Path $legacyCoveragePath -Recurse -Force
}

# Función para eliminar Data.Seeds del coverage XML
function Remove-DataSeedsFromCoverageXml {
    param(
        [string[]]$Files
    )
    
    foreach ($file in $Files) {
        if (-not (Test-Path $file)) {
            continue
        }
        
        try {
            [xml]$xml = Get-Content -Path $file
            $classesToRemove = $xml.SelectNodes("//class[contains(@name, 'Data.Seeds')]")
            
            foreach ($class in $classesToRemove) {
                $class.ParentNode.RemoveChild($class) | Out-Null
            }
            
            $xml.Save($file)
            Write-Host "  Removed $($classesToRemove.Count) Data.Seeds entries from: $(Split-Path -Leaf $file)" -ForegroundColor Cyan
        }
        catch {
            Write-Host "  WARNING: Could not process $file" -ForegroundColor Yellow
        }
    }
}

# Ejecutar tests con coverage
Write-Host "Ejecutando tests con coverage..." -ForegroundColor Cyan
dotnet test --settings runsettings/coverage.runsettings --verbosity minimal

# Verificar si se generaron los archivos
$rawPath = "runsettings/coverage/raw"
if (Test-Path $rawPath) {
    $coverageFiles = Get-ChildItem -Path $rawPath -Name "*.cobertura.xml" -Recurse
    Write-Host "Archivos de coverage encontrados: $($coverageFiles.Count)" -ForegroundColor Green
    
    # Clean coverage files before report generation
    Write-Host "Limpiando entradas de Data.Seeds del coverage..." -ForegroundColor Cyan
    $coveragePaths = Get-ChildItem -Path $rawPath -Filter "*.cobertura.xml" -Recurse | ForEach-Object { $_.FullName }
    Remove-DataSeedsFromCoverageXml -Files $coveragePaths
    
    # Generar reporte HTML
    Write-Host "Generando reporte HTML..." -ForegroundColor Cyan
    $targetDir = "runsettings/coverage/html"
    $reports = "$rawPath\*\coverage.cobertura.xml"
    $fileFilters = "-**/Migrations/*.cs;-**/Migrations/**/*.cs;-**/*ModelSnapshot.cs;-**/Docs/**/*.cs;-**/Data/Seeds/**/*.cs;-**/*ExampleSwagger*.cs;-**/*Swagger*.cs"
    
    reportgenerator -reports:$reports -targetdir:$targetDir -reporttypes:"Html;HtmlSummary;Badges" -sourcedirs:"." -filefilters:$fileFilters -verbosity:Info
    
    # Abrir reporte
    $indexPath = "$targetDir\index.html"
    if (Test-Path $indexPath) {
        Write-Host "Reporte generado exitosamente!" -ForegroundColor Green
        Write-Host "Abriendo reporte en el navegador..." -ForegroundColor Cyan
        Start-Process $indexPath
        Write-Host "Reporte disponible en: $(Resolve-Path $indexPath)" -ForegroundColor Yellow
    }
} else {
    Write-Host "ERROR: No se encontraron archivos de coverage" -ForegroundColor Red
}

Write-Host "Proceso completado." -ForegroundColor Green
