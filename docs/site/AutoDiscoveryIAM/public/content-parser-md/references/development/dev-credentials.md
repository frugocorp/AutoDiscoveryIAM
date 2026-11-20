---
[slug]: dev-credentials
[title]: Credenciales de desarrollo por defecto
[description]: Usuarios y contraseñas que se crean por defecto en un entorno de desarrollo.
[keywords]: credenciales de desarrollo, usuarios por defecto, seed
[readme_flavors]: github, npmjs
---

# Credenciales de desarrollo por defecto

El seeder (`UserSeeder.cs`) crea este usuario admin con el rol `AdminAutoDiscoveryIAM` **solo si la app lo pide con `SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true)`, como hace `samples/AutoDiscoveryIAM.SampleHost`, y `ASPNETCORE_ENVIRONMENT` es `Development` o `Testing`** — en cualquier otro ambiente (incluida Producción) no se crea, ver [Primer usuario con privilegios](/docs/security-rbac/authorization#primer-usuario-con-privilegios).

- **Usuario:** `admin` · **Email:** `admin@example.com` · **Contraseña:** `AdminPass123!`

> ⚠️ Solo para desarrollo/testing local.
