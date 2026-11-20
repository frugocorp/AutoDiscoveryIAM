# 📌 Labels `[repo-label]`

📍 **Dónde viven:** el repositorio → *Issues* → *Labels*. Se asignan en el issue (y en el PR), en el campo **Labels** de la barra lateral.

Por ahora el repositorio usa solo los 9 labels que GitHub crea por defecto. Ningún otro label existe: `security`, `tech-debt`, `architecture` o cualquier otro no se pueden asignar mientras no se creen en el repositorio y se agreguen a este catálogo.

Los labels complementan al tipo de issue ([`issue-type.md`](issue-type.md)), no lo repiten: un `Task` de refactor no necesita label.

---

## Label por plantilla

Cada plantilla trae en su encabezado (`labels:`) el label que le corresponde de origen:

| Plantilla | Label de origen |
|-----------|-----------------|
| `feature_request.md` | `enhancement` |
| `perf.md` | `enhancement` |
| `fix_bug.md` | `bug` |
| `hotfix.md` | `bug` |
| `docs.md` | `documentation` |
| `chore.md`, `refactor.md`, `test.md`, `ci.md`, `style.md` | ninguno |

Además del de origen, un issue puede llevar los labels de seguimiento que apliquen (`help wanted`, `good first issue`, `question`). `duplicate`, `invalid` y `wontfix` se ponen al cerrar un issue, nunca al crearlo.

---

## Tabla Resumen de Labels

| 📌 Label              | Propósito principal                 | Cuándo usarlo                                   |
|----------------------|-------------------------------------|-------------------------------------------------|
| 🐞 `bug`              | Errores o fallos del sistema        | Algo no funciona como debería                   |
| 📚 `documentation`    | Cambios o mejoras en documentación  | README, wiki, comentarios, docs externas        |
| 🔁 `duplicate`        | Marca issues repetidos              | Ya existe otro issue igual o muy similar        |
| 🚀 `enhancement`      | Mejoras o nuevas funcionalidades    | Solicitud de feature, optimización, mejora UX   |
| 🌱 `good first issue` | Ideal para nuevos colaboradores     | Tareas simples, bien definidas, para onboarding |
| 🙋 `help wanted`      | Se necesita apoyo externo           | Pedir ayuda con un issue o tarea pendiente      |
| 🚫 `invalid`          | Issue no aplica o está mal definido | No tiene sentido, es spam, está incompleto      |
| ❓ `question`         | Dudas o solicitudes de aclaración   | Se necesita explicación, guía o contexto        |
| ❌ `wontfix`          | No se resolverá el issue            | El equipo decide no tratar el problema          |

---

## Lista Detallada de Labels y Ejemplos

### 🐞 `bug`
- **Propósito:** Identificar errores, fallos o comportamientos inesperados en el sistema.
- **Cuándo usarlo:** Cuando una funcionalidad no opera como se espera, hay crashes, excepciones, o resultados incorrectos.
- **Ejemplos:**
  - El endpoint `/api/users` devuelve error 500.
  - El botón "Guardar" no responde en el formulario de registro.
  - El sistema permite crear usuarios con emails duplicados.
  - La paginación no muestra todos los elementos esperados.
  - El mensaje de error no se muestra cuando la validación falla.

---

### 📚 `documentation`
- **Propósito:** Marcar tareas relacionadas con la creación, corrección o mejora de documentación.
- **Cuándo usarlo:** Cambios en README, archivos Markdown, wiki, comentarios de código o documentación externa.
- **Ejemplos:**
  - Actualizar el README con instrucciones de despliegue.
  - Corregir ejemplos en la documentación de la API.
  - Añadir diagrama de arquitectura al wiki.
  - Mejorar la explicación de los parámetros en la documentación de endpoints.
  - Documentar el proceso de configuración de variables de entorno.

---

### 🔁 `duplicate`
- **Propósito:** Señalar issues que ya existen o han sido reportados previamente.
- **Cuándo usarlo:** Cuando un issue es igual o muy similar a otro ya abierto.
- **Ejemplos:**
  - Se reporta el mismo bug en dos issues distintos.
  - Una solicitud de feature ya está registrada.
  - Un error de validación ya fue documentado en otro issue.
  - Dos issues abiertos para el mismo problema de autenticación.
  - Propuestas de refactorización repetidas.

---

### 🚀 `enhancement`
- **Propósito:** Identificar solicitudes de mejora, nuevas funcionalidades o refactorizaciones.
- **Cuándo usarlo:** Cuando se propone optimizar, agregar o modificar una funcionalidad existente.
- **Ejemplos:**
  - Mejorar el rendimiento de la consulta de usuarios.
  - Agregar paginación a la API de posts.
  - Permitir login con redes sociales.
  - Optimizar el uso de memoria en el backend.
  - Añadir filtros avanzados en la búsqueda de productos.

---

### 🌱 `good first issue`
- **Propósito:** Indicar tareas ideales para nuevos colaboradores o quienes se inician en el proyecto.
- **Cuándo usarlo:** Issues simples, bien definidos y de bajo riesgo, útiles para onboarding.
- **Ejemplos:**
  - Corregir un error tipográfico en la interfaz.
  - Agregar un test unitario básico.
  - Actualizar el logo en la página de inicio.
  - Mejorar la descripción de un endpoint en la documentación.
  - Cambiar el color de un botón para mayor accesibilidad.
  - Añadir una validación simple a un formulario.

---

### 🙋 `help wanted`
- **Propósito:** Solicitar colaboración o ayuda externa en un issue específico.
- **Cuándo usarlo:** Cuando el equipo necesita ideas, revisión o implementación de una tarea.
- **Ejemplos:**
  - Se requiere ayuda para migrar a una nueva versión de framework.
  - Falta experiencia en una tecnología específica.
  - Se necesita colaboración para traducir la documentación.
  - Revisión de código para una nueva funcionalidad.
  - Resolver conflictos en la integración continua.
  - Solicitar apoyo para diseñar la interfaz de usuario.
  - Pedir ayuda para mejorar la cobertura de pruebas.
  - Se necesita ayuda para configurar un entorno de desarrollo específico.
  - Se requiere asistencia para depurar un problema complejo.

---

### 🚫 `invalid`
- **Propósito:** Marcar issues que no son válidos, están mal definidos o fuera de alcance.
- **Cuándo usarlo:** Issues incompletos, irrelevantes, spam o que no aplican al proyecto.
- **Ejemplos:**
  - Issue sin descripción ni contexto.
  - Solicitud fuera del alcance del repositorio.
  - Reporte de un problema que no es reproducible.
  - Solicitud de soporte para una versión no compatible.
  - Issue que no proporciona suficiente información para ser resuelto.
  - Solicitud de una funcionalidad que ya está implementada.
  - Reporte de un problema que es un error del usuario.

---

### ❓ `question`
- **Propósito:** Etiquetar solicitudes de aclaración, dudas o preguntas sobre el proyecto.
- **Cuándo usarlo:** Cuando se requiere explicación sobre una funcionalidad, proceso o decisión.
- **Ejemplos:**
  - ¿Cómo se configura el entorno de desarrollo?
  - ¿Qué convención de nombres se utiliza para los endpoints?
  - ¿Cuál es la diferencia entre los roles de usuario?
  - ¿Cómo se ejecutan los tests de integración?
  - ¿Qué librería se recomienda para validación de datos?
  - ¿Cómo se realiza el despliegue en producción?
  - ¿Dónde se encuentra la documentación de arquitectura?
  - ¿Cómo se manejan las excepciones en el código?
  - ¿Qué es el propósito de una función específica en el código?

---

### ❌ `wontfix`
- **Propósito:** Indicar que el issue no será resuelto, por decisión de equipo o contexto.
- **Cuándo usarlo:** Problemas fuera de alcance, baja prioridad, o que no afectan al producto.
- **Ejemplos:**
  - Solicitud de soporte para una versión obsoleta.
  - Feature descartada por decisión de producto.
  - Problema que no es reproducible en el entorno de desarrollo.
  - Solicitud de una funcionalidad que no es compatible con la arquitectura actual.
  - Issue que no tiene impacto significativo en el producto.
  - Solicitud de una mejora que no es factible en el corto plazo.
  - Problema que es un error del usuario y no del sistema.

---

## Recomendaciones Generales

- Usa los labels de manera consistente para facilitar la gestión y búsqueda.
- Puedes combinar varios labels en un mismo issue (ejemplo: `bug` + `help wanted`).
- Un label nuevo se crea primero en el repositorio y se agrega a este catálogo; hasta entonces no se usa.
