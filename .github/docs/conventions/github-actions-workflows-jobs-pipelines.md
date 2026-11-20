# GitHub Actions, Workflows, Jobs y Pipelines

## 1. ¿Qué es GitHub Actions?

GitHub Actions es el motor de automatización integrado en GitHub que permite ejecutar tareas (actions) en respuesta a eventos como `push`, `pull request`, creación de ramas, publicación de releases, entre otros. Facilita la definición de flujos de Integración Continua (CI), Entrega Continua (CD) y otras automatizaciones directamente en el repositorio, sin depender de herramientas externas.

**Ventajas principales:**

-   Permite automatizar pruebas, builds, despliegues y tareas repetitivas.
-   Se integra de forma nativa con el ciclo de vida de desarrollo en GitHub.
-   Es altamente configurable y extensible mediante acciones oficiales, de terceros o personalizadas.

---

## 2. ¿Qué es una Action?

Una **Action** es una unidad reutilizable de automatización dentro del ecosistema de GitHub Actions. Puede ser:

-   **Oficial:** Mantenida por GitHub.
-   **De terceros:** Publicada en el marketplace de GitHub.
-   **Personalizada:** Creada por ti o tu equipo para necesidades específicas de tu repositorio.

### Estructura de una Action personalizada

-   **Carpeta:** `.github/actions/{nombre-de-la-action}/`
-   **Archivo principal:** `action.yml`
-   **Código fuente:** Puede estar embebido en el propio `action.yml` o referenciar scripts externos (`.sh`, `.py`, `.js`, etc.)

**Ejemplo visual:**

```
.github/
  actions/
    mark-depends-on-resolved/
      action.yml
      mark_depends_on.py
```

-   `action.yml` define los inputs, outputs y cómo se ejecuta la action.
-   `mark_depends_on.py` contiene la lógica programada en Python (o el lenguaje que elijas).

---

## 3. ¿Qué es un Workflow?

Un **Workflow** es un archivo YAML ubicado en `.github/workflows/` que define cuándo y cómo se ejecutan una o varias actions (oficiales, de terceros o personalizadas). Un workflow permite automatizar procesos como pruebas, builds, despliegues y otras tareas en respuesta a eventos del repositorio.

-   Un workflow puede tener uno o varios jobs.
-   Cada job puede ejecutar una o varias actions en secuencia o en paralelo, según la configuración.

### Ejemplo de estructura de workflow y actions

```
.github/
  workflows/
    main.yml
  actions/
    mark-depends-on-resolved/
      action.yml
      mark_depends_on.py
```

-   `main.yml` es el workflow que define el flujo de automatización y llama a la action personalizada.

---

## 4. ¿Qué es un Pipeline?

En GitHub Actions, el término “pipeline” no es oficial, pero se usa para describir la **secuencia de pasos automatizados** que se ejecutan dentro de un job de un workflow.

-   Un pipeline es la “línea de ensamblaje” de tareas: checkout de código, build, lint, test, deploy, etc.
-   En GitHub Actions, cada job puede considerarse un pipeline, y un workflow puede contener varios jobs/pipelines.

### Resumen visual de la estructura

```
Repositorio
│
├── .github/
│   ├── workflows/
│   │   └── main.yml           # Workflow: orquesta jobs y actions
│   └── actions/
│       └── mark-depends-on-resolved/
│           ├── action.yml    # Action personalizada: define inputs, outputs, comandos
│           └── mark_depends_on.py  # Script ejecutado por la action
```

-   **Workflow:** Define el flujo general y cuándo se ejecuta.
-   **Job (pipeline):** Secuencia de pasos dentro del workflow.
-   **Action:** Tarea reutilizable que puede ser llamada por uno o varios jobs.

---

## 5. ¿Dónde entra cada uno?

-   **Actions:** Son los bloques básicos de funcionalidad. Se pueden reutilizar en distintos workflows y jobs.
-   **Workflow:** Es el “plan maestro” que define cuándo y cómo ejecutar jobs y actions.
-   **Pipeline:** Es el término práctico para la secuencia de pasos (steps) dentro de un job. Cada job en un workflow puede verse como un pipeline.
-   **Jobs:** Son conjuntos de steps (pasos) que se ejecutan en un runner. Cada job puede ejecutar varias actions y comandos.

### Ejemplo de flujo completo

1. **Evento:** Se crea un pull request (PR).
2. **Workflow:** Se activa el archivo `.github/workflows/main.yml`.
3. **Job (pipeline):** El workflow define un job que ejecuta varios pasos (checkout, build, test, lint, etc.).
4. **Action personalizada:** Uno de los pasos llama a `mark-depends-on-resolved` para actualizar issues según el PR.
5. **Script:** La action ejecuta `mark_depends_on.py` para realizar la lógica deseada.

### Referencias útiles

-   [Documentación oficial de GitHub Actions](https://docs.github.com/en/actions)
-   [Marketplace de Actions](https://github.com/marketplace?type=actions)

`dotnet clean`
`dotnet build`
`dotnet restore`

`dotnet clean; dotnet restore; dotnet build;`
