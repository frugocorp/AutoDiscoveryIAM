# HashGenerator

Utilidad de desarrollo para generar y verificar hashes Argon2id con los mismos parámetros que usa la librería (`Isopoh.Cryptography.Argon2`). Sirve cuando cambia alguna contraseña de las cuentas sembradas (`src/AutoDiscoveryIAM/Data/Seeds/`) o de su documentación.

No forma parte de la solución ni del paquete.

## Uso

Desde esta carpeta:

```powershell
dotnet run                          # hashes de todas las contraseñas sembradas
dotnet run -- "MiPassword123!"      # hash de una contraseña
dotnet run -- "MiPassword123!" '$argon2id$v=19$...'   # verifica una contraseña contra un hash
```

Las contraseñas sembradas son solo de desarrollo: la librería solo siembra esas cuentas en los ambientes `Development` y `Testing`.
