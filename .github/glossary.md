# Glosario de convenciones del repositorio

Este glosario es el mapa de las convenciones de `.github/`: qué significa cada marcador de las plantillas (`[issue-type]`, `[project-size]`...), en qué lugar de GitHub se asigna y qué archivo define sus valores válidos. Ante cualquier duda, consulta primero aquí y luego el archivo enlazado.

---

## Índice

1. [Dónde se asigna cada campo](#dónde-se-asigna-cada-campo)
2. [Campos del issue](#campos-del-issue)
3. [Campos del Project](#campos-del-project)
4. [Campos del repositorio](#campos-del-repositorio)
5. [Ramas y commits](#ramas-y-commits)
6. [Plantillas](#plantillas)
7. [Otras convenciones](#otras-convenciones)

---

## Dónde se asigna cada campo

GitHub reparte los datos de un issue en tres niveles. El nombre de cada catálogo empieza por el nivel donde vive el campo:

| Prefijo | Nivel | Dónde se edita | Ejemplo |
|---------|-------|----------------|---------|
| `issue-` | Organización `frugocorp` | Barra lateral del issue | Type, Priority, Effort |
| `project-` | Project del repositorio | Tarjeta del issue dentro del Project | Priority (P0–P2), Size, Estimate |
| `repo-` | Repositorio | Barra lateral del issue | Labels, Milestone |

**Priority** se llama igual en dos niveles distintos y no debe confundirse: existe en el issue (Urgent…Low) y en el Project (P0…P2). La del Project se deriva de la del issue, nunca se elige por separado.

---

## Campos del issue

| Marcador | Campo en GitHub | Valores | Catálogo |
|----------|-----------------|---------|----------|
| `[issue-type]` | Type | `Feature`, `Bug`, `Task` | [`issue-type.md`](docs/conventions/issue-type.md) |
| `[issue-priority]` | Priority | `Urgent`, `High`, `Medium`, `Low` | [`issue-priority.md`](docs/conventions/issue-priority.md) |
| `[issue-effort]` | Effort | `High`, `Medium`, `Low` (sale de `[project-size]`) | [`issue-effort.md`](docs/conventions/issue-effort.md) |
| `[relationship]` | Relationships | `Parent issue`, `Blocked by`, `Blocking`, `Related to`, `Duplicate of` | [`issue-relationships.md`](docs/conventions/issue-relationships.md) |

## Campos del Project

| Marcador | Campo en GitHub | Valores | Catálogo |
|----------|-----------------|---------|----------|
| `[project-priority]` | Priority | `P0`, `P1`, `P2` (sale de `[issue-priority]`) | [`project-priority.md`](docs/conventions/project-priority.md) |
| `[project-size]` | Size | `XS`, `S`, `M`, `L`, `XL` | [`project-size.md`](docs/conventions/project-size.md) |
| `[project-estimate]` | Estimate | `1`, `2`, `3`, `5`, `8`, `13` (sale de `[project-size]`) | [`project-estimate.md`](docs/conventions/project-estimate.md) |

Los campos de fechas (Start date, Target date y End date, en el issue y en el Project) y el **Status** del Project no se usan en los archivos de issue: un archivo puede quedarse en local días o meses antes de subirse, y sus fechas quedarían desfasadas. Si hacen falta, se asignan directamente en GitHub. El avance se lee de los archivos locales de cada issue (sin plan, no ha empezado; sin PR, no ha terminado).

## Campos del repositorio

| Marcador | Campo en GitHub | Valores | Catálogo |
|----------|-----------------|---------|----------|
| `[repo-label]` | Labels | Los 9 de GitHub por defecto | [`repo-label.md`](docs/conventions/repo-label.md) |
| `[repo-milestone]` | Milestone | Los milestones existentes del repositorio | [`repo-milestone.md`](docs/conventions/repo-milestone.md) |

---

## Ramas y commits

| Marcador | Qué es | Catálogo |
|----------|--------|----------|
| `[branch-type]` | Tipo de rama (`feature`, `fix`, `chore`...); decide la plantilla del issue | [`branch-type.md`](docs/conventions/branch-type.md) |
| `[short-title]` | Título corto del issue en kebab-case | [`branch-type.md`](docs/conventions/branch-type.md) |
| `[branch-name]` | `<branch-type>/#<issue-id>-<short-title>` | [`branch-type.md`](docs/conventions/branch-type.md) |
| `[commit-type]` | Prefijo del commit (`feat`, `fix`, `docs`...) | [`conventional-commits.md`](docs/conventions/conventional-commits.md) |

---

## Plantillas

- **Issues:** `.github/ISSUE_TEMPLATE/`, una por `[branch-type]`. Cada una fija su tipo de issue y su label de origen.
- **Pull requests:** `.github/PULL_REQUEST_TEMPLATE.md`.

---

## Otras convenciones

- [`linter.md`](docs/conventions/linter.md): analizadores y formateadores del código.
- [`glob-patterns.md`](docs/conventions/glob-patterns.md): patrones de rutas en configuraciones y workflows.
- [`github-actions-workflows-jobs-pipelines.md`](docs/conventions/github-actions-workflows-jobs-pipelines.md): términos de GitHub Actions.
- [`no-licensed-libraries.md`](docs/conventions/no-licensed-libraries.md): dependencias permitidas según su licencia.
- **Versionado:** SemVer manual. La versión se fija en el `.csproj` de la librería y cada cambio notable se registra en `CHANGELOG.md` ([Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)).
