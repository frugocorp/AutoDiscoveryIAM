# 🔢 Prioridad del issue `[issue-priority]`

📍 **Dónde vive:** organización `frugocorp` → *Issue fields* → **Priority**. Se asigna en el issue, en la barra lateral, para cualquier tipo de issue.

No confundir con la prioridad del Project (P0, P1, P2), que es otro campo, en otro lugar: ver [`project-priority.md`](project-priority.md). Las dos se llenan, y siempre con valores equivalentes (tabla de abajo).

---

## Catálogo

| Valor | Significado | Cuándo usarlo | Ejemplo |
|-------|-------------|---------------|---------|
| `Urgent` | Bloquea todo; se atiende de inmediato. | Rompe producción, impide publicar o desplegar, pérdida de datos o vulnerabilidad explotable. | "El login responde 500 y nadie puede entrar". |
| `High` | Importante; va en la siguiente tanda de trabajo. | Afecta la seguridad, la integridad de datos o a quien integra la librería, sin bloquear todo. | "Un endpoint no valida el permiso". |
| `Medium` | Útil, se programa según disponibilidad. | Mejoras, refactors, bugs menores, deuda técnica. | "Refactorizar el repositorio de usuarios". |
| `Low` | Puede esperar sin consecuencias. | Detalles cosméticos, ideas a futuro, documentación menor. | "Corregir una errata en un comentario". |

---

## Equivalencia con la prioridad del Project

| `[issue-priority]` | `[project-priority]` |
|--------------------|----------------------|
| `Urgent` | `P0` |
| `High` | `P1` |
| `Medium` | `P2` |
| `Low` | `P2` |

El Project solo tiene tres niveles, así que `Medium` y `Low` comparten `P2`: la diferencia entre ambos queda en el campo del issue.
