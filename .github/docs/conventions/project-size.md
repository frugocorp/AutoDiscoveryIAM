# 📏 Tamaño en el Project `[project-size]`

📍 **Dónde vive:** el Project de GitHub del repositorio → campo **Size**. Se asigna en la tarjeta del issue dentro del Project, no en el issue.

Es el primer campo de esfuerzo que se decide; de él salen la estimación del Project ([`project-estimate.md`](project-estimate.md)) y el esfuerzo del issue ([`issue-effort.md`](issue-effort.md)).

---

## Catálogo

| `[project-size]` | Duración | `[project-estimate]` | `[issue-effort]` | Ejemplos |
|------------------|----------|----------------------|------------------|----------|
| 🟢 `XS` | Menos de 30 minutos | `1` | `Low` | Erratas, ajustes de mensajes de error |
| 🔵 `S` | Menos de 2 horas | `2` o `3` | `Low` | Validaciones simples, cambios de configuración |
| 🟡 `M` | 2 a 6 horas | `5` | `Medium` | Endpoint nuevo, logging, integración menor |
| 🟠 `L` | 1 a 3 días | `8` | `High` | Refactor de un módulo, migración pequeña |
| 🔴 `XL` | 1 semana o más | `13` | `High` | Rediseño de arquitectura, cambios estructurales |

No existe ningún otro valor. Si una tarea parece mayor que `XL`, se divide en varios issues (con `Parent issue`, ver [`issue-relationships.md`](issue-relationships.md)).

---

🟢 **Size: XS (Extra pequeño)**
- 🌀 1 punto
- 🕜 Menos de 30 minutos.
- 🧩 Características:
    - Cambios muy simples y de bajo riesgo.
    - No requiere lógica de negocio ni dependencias externas.
    - No afecta el flujo principal de la aplicación.
    - No requiere pruebas profundas ni revisión de código extensa.
    - Se puede completar en menos de 30 minutos.
- ✅ Ejemplos:
    - Corregir un error tipográfico en un mensaje de respuesta.
    - Ajustar el nombre de un campo en un DTO o modelo.
    - Actualizar la documentación de un endpoint existente.
    - Modificar un mensaje de error para mayor claridad.
    - Cambiar la configuración de CORS para permitir un origen adicional.

---

🔵 **Size: S (Pequeño)**
- 🌀 2-3 puntos
- ⏱️ Toma menos de 2 horas.
- 🧩 Características:
    - Tarea sencilla con lógica mínima.
    - Afecta solo un archivo o módulo.
    - Puede requerir una prueba rápida o revisión básica.
    - No introduce dependencias nuevas ni afecta la arquitectura.
    - Se completa generalmente en menos de 2 horas.
- ✅ Ejemplos:
    - Agregar una validación simple a un campo de entrada.
    - Exponer un nuevo campo en la respuesta de un endpoint existente.
    - Añadir un filtro básico a una consulta de datos.
    - Actualizar la configuración de logging para un entorno.
- Implementar una respuesta estándar para errores comunes.

---

🟡 **Size: M (Mediano)**
- 🌀 5 puntos
- 🕜 2–6 horas (hasta un día).
- 🧩 Características:
    - Implica lógica de negocio moderada y validaciones.
    - Puede involucrar varios archivos o módulos.
    - Requiere pruebas funcionales y validación cruzada.
    - Puede necesitar pequeños refactors.
    - Se completa en una jornada de trabajo.
- ✅ Ejemplos:
    - Implementar un endpoint POST con validaciones y lógica de negocio.
    - Añadir paginación y ordenamiento a un listado de recursos.
    - Integrar una notificación por email ante ciertos eventos.
    - Refactorizar la gestión de errores para un módulo.
    - Agregar logging estructurado a operaciones críticas.

---

🟠 **Size: L (Grande)**
- 🌀 8 puntos
- 🕒 1–3 días.
- 🧩 Características:
    - Tarea compleja, con múltiples pasos o integración externa.
    - Afecta varios módulos o componentes del sistema.
    - Requiere pruebas integrales y validación en diferentes capas.
    - Puede involucrar migraciones de datos o cambios en la arquitectura.
    - Se completa en varios días.
- ✅ Ejemplos:
    - Implementar el patrón Unit of Work para toda la capa de datos.
    - Integrar autenticación externa (OAuth, JWT, etc.).
    - Migrar la configuración de la aplicación a un sistema centralizado.
    - Refactorizar la lógica de autorización en todos los endpoints.
- Adaptar el sistema para soportar soft deletes en múltiples entidades.

---

🔴 **Size: XL (Épica o muy grande)**
- 🌀 13 puntos
- 🕜 Más de 1 semana, requiere planificación, coordinación y puede dividirse en subtareas.
- 🧩 Características:
    - Alta complejidad, impacto o incertidumbre.
- Puede afectar varias áreas o microservicios.
- Requiere coordinación entre varios desarrolladores o equipos.
- Necesita planificación, pruebas exhaustivas y revisión profunda.
- Generalmente se divide en subtareas o épicas.
- ✅ Ejemplos:
    - Rediseñar la arquitectura de la API para soportar multitenencia.
- Implementar un sistema completo de permisos y roles granulares.
- Migrar toda la base de datos a un nuevo motor o proveedor.
- Desarrollar un módulo de auditoría y trazabilidad para todas las entidades.
- Refactorizar el sistema de autenticación y autorización a nivel global.

---

**Tips de uso:**

- Combina tamaño y prioridad para planificar: un bug `High`/`P1` de tamaño `S` se resuelve rápido; un refactor `Medium`/`P2` de tamaño `L` se agenda para después.
- Revisa el alcance real antes de asignar tamaño, especialmente en tareas de infraestructura o arquitectura.
