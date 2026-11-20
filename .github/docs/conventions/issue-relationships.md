# 🔗 Relaciones entre issues y PR `[relationship]`

📍 **Dónde viven:** en el issue, apartado **Relationships** de la barra lateral (nativo de GitHub), salvo `Related to`, que es solo texto.

En el archivo del issue se escriben como lista en el apartado **🔗 Relaciones**, y al crear el issue en GitHub cada línea se copia a su lugar nativo. GitHub las muestra solas en el issue y en el Project: no hace falta ningún script que las lea.

---

## Formato de cada línea

```markdown
- [ ] {relationship} #{issue-id} : [{short-title}]({issue-link})
```

- `{relationship}`: uno de los valores del catálogo de abajo, escrito exactamente igual.
- `{issue-id}`: número del issue relacionado.
- `{short-title}`: el `[short-title]` del issue relacionado.
- `{issue-link}`: URL completa del issue relacionado.

Ejemplo:

```markdown
- [ ] Blocked by #12 : [unit-of-work-infraestructura](https://github.com/frugocorp/AutoDiscoveryIAM/issues/12)
```

Si no hay ninguna relación, se deja la única línea `- [x] Sin relaciones`.

---

## Catálogo para issues

| `[relationship]` | Significado | Cómo se aplica en GitHub |
|------------------|-------------|--------------------------|
| `Parent issue` | Este issue es una parte (sub-issue) de otro más grande. | Barra lateral → **Relationships** → *Add parent*. |
| `Blocked by` | Este issue no puede avanzar hasta que el otro se cierre. | Barra lateral → **Relationships** → *Mark as blocked by*. |
| `Blocking` | Este issue impide que el otro avance. | Barra lateral → **Relationships** → *Mark as blocking*. |
| `Related to` | Relación informativa, sin dependencia. | Solo texto: la mención `#N` crea la referencia cruzada. |
| `Duplicate of` | Este issue repite a otro. | Se cierra con *Close as duplicate* y se elige el issue original. |

Una relación se declara en un solo lado: si el issue 12 está `Blocked by` el 10, no hace falta declarar en el 10 que es `Blocking` del 12; GitHub muestra las dos caras.

---

## Catálogo para PR

En la descripción del PR, una palabra de cierre seguida del issue hace que GitHub lo cierre al hacer merge en la rama principal:

| Palabra | Uso |
|---------|-----|
| `Closes #N` | La que usa este proyecto, siempre. |

GitHub también acepta `Fixes` y `Resolves`, pero por consistencia los PR de este proyecto usan solo `Closes`.
