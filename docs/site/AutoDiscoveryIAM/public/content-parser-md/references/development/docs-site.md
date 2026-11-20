---
[slug]: docs-site
[title]: Sitio de documentación (Angular)
[description]: Cómo está organizado el sitio de documentación de AutoDiscoveryIAM, una app Angular con SSR.
[keywords]: Angular, SSR, sitio de documentación, Feature-Driven Development, bounded contexts
[readme_flavors]: github, npmjs
---

# Sitio de documentación (Angular)

El sitio de documentación (`docs/site/AutoDiscoveryIAM`) es una app Angular con SSR, servida en desarrollo por [`docker/app/web/`](/tutorials/docker/docker-app-web). Su estructura interna sigue Feature-Driven Development (FDD) combinado con Bounded Contexts (DDD) — pensada para escalar a cualquier tamaño de sitio sin que `src/app/` se vuelva una carpeta plana ingobernable:

```
src/app/
├── core/                            # Singletons globales de toda la app (Auth, interceptores HTTP,
│                                     # guards de sesión, config global) — una sola instancia, se
│                                     # importa una vez en el arranque, nunca por feature.
├── shared/                          # UI Kit global (botones, modales base, inputs, pipes/directivas
│                                     # genéricas) — reutilizable por cualquier bounded context, sin
│                                     # lógica de negocio de ninguno en particular.
│
└── bounded-contexts/                # Término de Eric Evans (DDD) — cada subcarpeta es un límite de
    │                                 # dominio con su propio lenguaje/modelo, independiente de los demás.
    │
    ├── <bounded-context-a>/         # ─── BOUNDED CONTEXT: <nombre del dominio> ───
    │   ├── core/                    # Interceptores/servicios/guards exclusivos de este contexto
    │   ├── shared/                  # Componentes visuales exclusivos de este contexto (no globales)
    │   └── features/                # Slices funcionales (Feature-Driven Development) de este contexto
    │       ├── <feature-slice-1>/   # Slice: una pieza de funcionalidad cohesiva y autocontenida
    │       └── <feature-slice-2>/   # Slice: otra pieza de funcionalidad, independiente de la anterior
    │
    ├── <bounded-context-b>/         # ─── BOUNDED CONTEXT: <otro dominio> ───
    │   ├── core/
    │   ├── shared/
    │   └── features/
    │       └── <feature-slice-1>/
    │
    └── <bounded-context-n>/         # ... tantos bounded contexts como dominios reales tenga el sitio
```

**Regla de anidamiento** (el mismo patrón se repite en cada nivel, a propósito): `core/` es lo transversal/singleton **de ese nivel** — global si está en la raíz de `app/`, exclusivo del bounded context si está dentro de uno; `shared/` es UI reutilizable **de ese nivel**, sin lógica de negocio de ningún slice en particular; `features/` es donde vive el trabajo real, un slice por caso de uso — mismo espíritu que `Features/<Entity>/Commands|Queries/<Slice>/` en el backend (Vertical Slice, ver [Ejemplo: arquitectura Vertical Slice en tu propia app](/tutorials/examples/vertical-slice-example)), aplicado del lado de Angular. Vale la pena mantener los ejemplos de la estructura (`<bounded-context-a>`, `<feature-slice-1>`, etc.) aunque sean genéricos — sin un ejemplo concreto de cuántos niveles anidar y dónde, la regla es fácil de mal-aplicar (ej. meter un componente de un solo feature dentro de `shared/` del bounded context, o un servicio de negocio dentro del `shared/` global).

**Cuándo crear un bounded context nuevo vs. un feature slice nuevo dentro de uno existente**: un bounded context nuevo es cuando el dominio tiene su propio lenguaje/modelo que no comparte con los demás (ej. "Facturación" y "Catálogo de productos" casi nunca coinciden en qué significa "item" — uno lo ve como una línea de cobro, el otro como un producto con inventario); un feature slice nuevo es cuando ya estás dentro del mismo dominio, solo agregando otro caso de uso al mismo lenguaje/modelo. La pregunta práctica: si dos partes del sitio necesitaran modelos de datos incompatibles para lo que a simple vista "suena igual", son bounded contexts distintos, no dos features del mismo.

