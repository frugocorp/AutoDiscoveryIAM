---
[slug]: performance-timeouts
[title]: Timeouts de las pruebas de performance
[description]: Por qué los umbrales de tiempo de las pruebas de performance son generosos y cuáles son.
[keywords]: pruebas de performance, timeouts, Argon2id, ci-full
[readme_flavors]: github, npmjs
---

# ⏱️ Timeouts de las pruebas de Performance

## 🎯 Filosofía de los timeouts

1. **Realismo de hardware**: pensados para equipos de desarrollo con capacidades medias, no servidores dedicados.
2. **Margen de seguridad**: absorben variaciones normales del sistema operativo y, sobre todo, la contención de CPU/memoria que agrega `ci-full` (cobertura instrumentada + toda la suite corriendo en paralelo).
3. **Estabilidad**: evitar fallos esporádicos por timing en vez de por un problema real de performance.
4. **Utilidad real**: los márgenes son generosos, pero siguen detectando una regresión grave (ej. si el hashing se volviera 10x más lento por un bug real).

## 🔬 Parámetros de Argon2id (`Common/Security/PasswordHasher.cs`)

`Argon2.Hash(password)` de `Isopoh.Cryptography.Argon2` usa, por defecto:

- `m=65536` (64 MiB de memoria)
- `t=3` (3 iteraciones)
- `p=1` (1 hilo de paralelismo)

Argon2id es *memory-hard* a propósito (reserva memoria además de CPU, para dificultar ataques por GPU) — cuesta más por operación que un hash puramente basado en CPU, y ese costo es mayor todavía bajo cobertura de código instrumentada + ejecución paralela de toda la suite (perfil `ci-full`). `VerifyPassword` tiene un costo equivalente a `HashPassword`, ya que Argon2 recalcula el hash con los parámetros codificados en el string para comparar.

## 📊 Umbrales actuales

| Prueba | Umbral | Qué mide |
| --- | --- | --- |
| `PasswordHasherTests.HashPassword_Performance_Should_Be_Reasonable` | 45,000ms por operación | Un solo `HashPassword`. |
| `LoginPerformanceTests.PasswordHasher_Argon2idPerformance_MaintainsAcceptableSpeed` | 20,000ms por operación | 20 iteraciones de `HashPassword` + `VerifyPassword`. |
| `LoginPerformanceTests` (caso individual de login) | 15,000ms | Un login completo (hash/verify + lógica de negocio). |
| `LoginPerformanceTests` (promedio por caso) | 1,200ms | Promedio sobre el conjunto de casos de login. |
| `LoginPerformanceTests` (concurrencia) | 120,000ms (2 min) | N requests de login concurrentes. |
| `LoginMassiveDataTests` (lote masivo) | 1,200,000ms (20 min) | 1000+ operaciones de login secuenciales. |
| `LoginMassiveDataTests` (promedio por caso) | 1,200ms | Promedio sobre el lote masivo. |
| `LoginEdgeCasesTests` (prevención de timing attacks) | máx(1,500ms, promedio × 1.5) | Desviación máxima aceptable entre intentos válidos/inválidos. |

Todas estas pruebas están etiquetadas `[Trait("Category", "Performance")]` — quedan excluidas del perfil `normal` (el de uso diario) y solo corren bajo `performance`/`ci-full`.

## 🔬 Factores que afectan el timing en desarrollo

- **JIT warming**: la primera ejecución de un proceso es más lenta que las siguientes.
- **Garbage Collection**: pausas de Gen2 no siempre predecibles.
- **Antivirus/background tasks**: escaneo en tiempo real, indexado, actualizaciones.
- **Throttling térmico**: común en laptops bajo carga sostenida.
- **Presión de memoria**: Argon2id reserva ~64 MiB por operación; otros procesos compitiendo por memoria afectan el tiempo medido.
- **`ci-full` específicamente**: cobertura de código instrumentada + toda la suite (funcional + performance) corriendo en paralelo es, por diseño, el escenario de mayor contención — de ahí que los márgenes de estas pruebas sean sensiblemente mayores que el costo real de una sola operación en aislamiento.

## 🎯 Desarrollo vs. producción

| Escenario | Umbral en desarrollo | Esperado en producción | Ratio |
| --- | --- | --- | --- |
| Login (1 operación Argon2id) | 15,000ms | 800-1,200ms | ~12-18x |
| Lote de 1000 logins | 1,200,000ms | 300,000ms | 4x |
| Requests concurrentes | 120,000ms | 30,000-40,000ms | ~3-4x |

Un servidor de producción típico (CPU/RAM dedicados, sin overhead de desarrollo, sin cobertura instrumentada) opera muy por debajo de estos márgenes — son deliberadamente generosos para absorber el peor caso de `ci-full` en hardware de desarrollo modesto, no una expectativa realista de producción.

## 🔧 Monitoreo recomendado en producción

- APM con percentiles P95/P99 en el endpoint de login.
- Alerta si P95 > 1,500ms (Argon2id agrega una latencia base mayor que un hash puramente por CPU).
- Vigilar consumo de memoria bajo carga concurrente alta de login (Argon2id es memory-hard).

## 📚 Referencias

- [Argon2 RFC 9106 — recomendaciones de parámetros](https://www.rfc-editor.org/rfc/rfc9106.html)
- [Isopoh.Cryptography.Argon2 (paquete usado en este proyecto)](https://www.nuget.org/packages/Isopoh.Cryptography.Argon2)
