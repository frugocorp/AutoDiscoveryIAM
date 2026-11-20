---
[slug]: troubleshooting
[title]: Troubleshooting
[description]: Problemas comunes al ejecutar el proyecto y cómo resolverlos.
[keywords]: troubleshooting, errores comunes, solución de problemas
[readme_flavors]: github, npmjs
---

# Troubleshooting

**`dotnet ef` responde "no se pudo ejecutar porque no se especificó el comando o archivo":**

Falta la herramienta global `dotnet-ef` (no viene con el SDK de .NET, se instala aparte una sola vez por máquina):

````code-block
```bash
dotnet tool install --global dotnet-ef
```
````

**El usuario admin no existe tras `dotnet ef database update`:**

1. Confirmar el ambiente activo:

   ````code-block
   ```bash
   echo $ASPNETCORE_ENVIRONMENT
   ```
   ````

   Debe ser `Development` o `Testing` — en cualquier otro valor el seeder omite este usuario a propósito.

2. Confirmar que el seed corrió al arrancar la app (se ejecuta en cada arranque, no en la migración):

   ````code-block
   ```bash
   dotnet run --project samples/AutoDiscoveryIAM.SampleHost
   ```
   ````

3. Verificar en la base de datos:

   ````code-block
   ```sql
   SELECT * FROM Users WHERE Username = 'admin';
   SELECT * FROM Roles WHERE Name IN ('AdminAutoDiscoveryIAM', 'UserDefault');
   ```
   ````

---
