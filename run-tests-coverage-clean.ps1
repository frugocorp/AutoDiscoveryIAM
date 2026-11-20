# Script de limpieza de directorios de coverage
Write-Host "= LIMPIEZA DE COVERAGE =" -ForegroundColor Green

$basePath = "runsettings/coverage"
$legacyPath = "runsettings/runsettings/coverage"

# Limpiar directorios GUID antiguos
if (Test-Path "$basePath/raw") {
    $guidDirs = Get-ChildItem -Path "$basePath/raw" -Directory
    Write-Host "Directorios GUID encontrados: $($guidDirs.Count)" -ForegroundColor Yellow
    
    foreach ($dir in $guidDirs) {
        Write-Host "Eliminando: $($dir.Name)" -ForegroundColor Gray
        Remove-Item -Path $dir.FullName -Recurse -Force
    }
    
    Write-Host "Directorios GUID eliminados." -ForegroundColor Green
}

# Limpiar reportes HTML anteriores
if (Test-Path "$basePath/html") {
    Write-Host "Eliminando reportes HTML anteriores..." -ForegroundColor Yellow
    Remove-Item -Path "$basePath/html" -Recurse -Force
    Write-Host "Reportes HTML eliminados." -ForegroundColor Green
}

Write-Host "Limpieza completada." -ForegroundColor Green

# Limpiar ruta legacy si existe
if (Test-Path $legacyPath) {
    Write-Host "Eliminando ruta legacy..." -ForegroundColor Yellow
    Remove-Item -Path $legacyPath -Recurse -Force
    Write-Host "Ruta legacy eliminada." -ForegroundColor Green
}
