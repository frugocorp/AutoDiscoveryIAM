---
name: 📚 Actualización o mejora de documentación
about: Propón la creación, actualización o mejora de documentación interna o externa
title: '[DOCS] '
type: Task
labels:
  - documentation
assignees: []
---

<!-- title:[DOCS] : aquí DOCS corresponde al tipo de rama [branch-type] en mayúsculas -->
<!-- type: tipo de issue [issue-type] fijo de esta plantilla; GitHub lo asigna solo al crear el issue -->
<!-- labels: esta plantilla trae su label de origen del catálogo [repo-label]; agrega solo otros labels de ese catálogo si aplican -->

## 📝 Resumen <!-- Obligatorio -->

<!--
Breve descripción del objetivo principal del issue, en una sola frase clara y concisa (no debe exceder los 150 caracteres).

Este resumen es indispensable porque:
- Permite extraer automáticamente una descripción breve y uniforme para reportes, dashboards, tablas de relaciones y documentación.
- Facilita la comprensión rápida del propósito del issue sin leer todo el cuerpo.
- Mejora la trazabilidad y la integración con herramientas externas (scripts, APIs, tableros).
-->
<!-- Describe a partir de aquí: -->

## ✨ Descripción <!-- Obligatorio -->

<!-- Explica brevemente el issue, señala detalles como entorno (producción, QA, etc.), contexto, impacto, urgencia, necesidad, otros. -->
<!-- Describe a partir de aquí: -->

## 🔗 Relaciones <!-- Obligatorio -->

<!-- La opción mínima es - [x] Sin relaciones. Si hay relaciones, enlista una por línea con el formato de [relationship] (issue-relationships.md) y, al crear el issue, aplica cada una en la barra lateral → Relationships. Ejemplo:
- [ ] Parent issue #123 : [short-title](https://github.com/{owner}/{repo}/issues/123)
- [ ] Blocked by #123 : [short-title](https://github.com/{owner}/{repo}/issues/123)
- [ ] Blocking #123 : [short-title](https://github.com/{owner}/{repo}/issues/123)
- [ ] Related to #123 : [short-title](https://github.com/{owner}/{repo}/issues/123)
-->
<!-- Describe a partir de aquí: -->

- [x] Sin relaciones

## 🧩 Objetivo o Comportamiento esperado <!-- Obligatorio -->

<!-- Describe detalladamente lo que se pretende lograr. -->
<!-- Describe a partir de aquí: -->

## ✅ Checklist para el pull request <!-- Obligatorio -->

<!-- La siguiente se copia al pull request y ahí validar que se cumplen los requisitos del issue, no es necesario que se marque como cumplido en el issue. Es indispensable enlistar los puntos que se deben cumplir para que el pull request pueda ser aceptado -->

- [ ] Todos los commit del issue cumplen con las [Conventional Commits](../blob/main/.github/docs/conventions/conventional-commits.md)
- [ ] Se cerraron todos los issues de los que este depende (`Blocked by`)
- [ ] Se completó el apartado Clasificación
- [ ] X
- [ ] Y
- [ ] Z

## 💬 Notas adicionales <!-- Opcional -->

<!-- Referencias, enlaces, logs, evidencia o cualquier información relevante. -->
<!-- Describe a partir de aquí: -->

---

## 🏷️ Clasificación <!-- Obligatorio -->

<!-- Este apartado lo completa el desarrollador. Cada valor se copia a su campo en GitHub, en el lugar que indica cada grupo; los valores válidos están en el catálogo de cada marcador (ver Guías de referencia). -->
<!-- Describe a partir de aquí: -->

### 📍 Issue (barra lateral del issue)

🚧 Tipo de issue `[issue-type]`: Task
📌 Labels `[repo-label]`:
📅 Milestone `[repo-milestone]`:
🔢 Prioridad `[issue-priority]`:
🏋️ Esfuerzo `[issue-effort]`:

### 📊 Project (tarjeta del issue en el Project)

🚦 Prioridad `[project-priority]`:
📏 Tamaño `[project-size]`:
🧮 Estimación `[project-estimate]`:

### 🌿 Rama

🛠 Tipo de rama `[branch-type]`: docs
📝 Titulo corto (kebab-case) `[short-title]`:
🧾 Nombre de la rama _(base: docs/#[issue-id]-[short-title])_ `[branch-name]`: docs/#-

<!-- [issue-id] tiene que ser reemplazado una vez que se crea el issue ya que antes es imposible obtenerlo -->

## 🧭 Guías de referencia

Para asignar correctamente cada valor de la Clasificación, consulta su catálogo:

- 🗺️ [Glosario: dónde se asigna cada campo](../blob/main/.github/glossary.md)
- 🚧 `[issue-type]` [Tipos de issue](../blob/main/.github/docs/conventions/issue-type.md)
- 📌 `[repo-label]` [Labels](../blob/main/.github/docs/conventions/repo-label.md)
- 📅 `[repo-milestone]` [Milestones](../blob/main/.github/docs/conventions/repo-milestone.md)
- 🔢 `[issue-priority]` [Prioridad del issue](../blob/main/.github/docs/conventions/issue-priority.md)
- 🏋️ `[issue-effort]` [Esfuerzo del issue](../blob/main/.github/docs/conventions/issue-effort.md)
- 🚦 `[project-priority]` [Prioridad en el Project](../blob/main/.github/docs/conventions/project-priority.md)
- 📏 `[project-size]` [Tamaño en el Project](../blob/main/.github/docs/conventions/project-size.md)
- 🧮 `[project-estimate]` [Estimación en el Project](../blob/main/.github/docs/conventions/project-estimate.md)
- 🔗 `[relationship]` [Relaciones entre issues](../blob/main/.github/docs/conventions/issue-relationships.md)
- 🔀 `[branch-type]` `[branch-name]` [Tipos y nombres de rama](../blob/main/.github/docs/conventions/branch-type.md)

---

## ✅ Checklist antes de enviar

Marca cada punto para asegurar que tu propuesta está completa y alineada a las convenciones del proyecto:

- [ ] La descripción del issue es clara y específica.
- [ ] El objetivo del issue está bien justificado.
- [ ] El alcance de los cambios está correctamente especificado.
- [ ] Se han agregado notas, referencias o información adicional si aplica.
- [ ] Cada relación se aplicó en la barra lateral del issue → Relationships.
- [x] Se seleccionó el tipo de rama correcto (`[branch-type]`).
- [ ] Los campos del issue (`[repo-label]`, `[repo-milestone]`, `[issue-priority]`, `[issue-effort]`) se asignaron en la barra lateral, con valores de sus catálogos.
- [ ] Los campos del Project (`[project-priority]`, `[project-size]`, `[project-estimate]`) se asignaron en la tarjeta del Project, con valores de sus catálogos.
- [ ] `[issue-effort]`, `[project-priority]` y `[project-estimate]` corresponden a las equivalencias de sus catálogos.
- [ ] El título corto (`[short-title]`) sigue el formato sugerido (kebab-case).
- [ ] Se actualizó el Nombre de la rama (`[branch-name]`) una vez creado el issue.
