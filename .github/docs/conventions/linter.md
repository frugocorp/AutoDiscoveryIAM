# Guía Integral de Linters y Formateadores para .NET y Archivos Asociados

Esta guía documenta de forma amplia y práctica el uso, configuración, prioridades y buenas prácticas de linters y formateadores en proyectos backend .NET (C#) y archivos asociados (configuración, documentación, scripts, etc.).

---

## ¿Qué es un Linter? ¿Qué es un Formateador?

-   **Linter:** Herramienta que analiza el código fuente para detectar errores, malas prácticas, problemas de estilo, semántica y convenciones. Puede sugerir o aplicar correcciones.
-   **Formateador:** Herramienta que ajusta automáticamente el formato del código (espaciado, indentación, saltos de línea, etc.) para cumplir con reglas predefinidas.

Ambos son fundamentales para el trabajo colaborativo, la mantenibilidad y la calidad del código.

---

## Convención del Proyecto: Integración de Linters y Formateadores

> Esta sección describe la convención adoptada para la integración y uso conjunto de linters y formateadores en el proyecto. Aquí se documentarán los fundamentos, ventajas, criterios y expectativas para el equipo.

### ¿Por qué se decidió usar más de una herramienta (Roslyn Analyzers, StyleCop, EditorConfig, dotnet format, Prettier)?

La decisión de integrar varias herramientas de linteo y formateo en el proyecto responde a la necesidad de lograr una cobertura completa y robusta sobre todos los aspectos de calidad y estilo del código. Ninguna herramienta por sí sola cubre todos los niveles de análisis y formato requeridos en un entorno profesional y colaborativo. Cada una aporta fortalezas específicas:

- **Roslyn Analyzers:** Detectan errores semánticos, malas prácticas, convenciones y posibles bugs a nivel de compilador y durante la edición. Permiten análisis avanzados y personalizables.
- **StyleCop:** Refuerza reglas de estilo y convenciones específicas de C#, complementando a Roslyn con validaciones estrictas sobre documentación, nombres, estructura, etc.
- **EditorConfig:** Define reglas universales de formato (indentación, saltos de línea, codificación) que son interpretadas por múltiples editores y herramientas, garantizando coherencia visual.
- **dotnet format:** Aplica y corrige automáticamente el formato y estilo definido en `.editorconfig` y los analizadores, asegurando que todos los archivos cumplan las reglas antes de integrarse al repositorio.
- **Prettier:** Se encarga del formato homogéneo en archivos de configuración, documentación y scripts no C#, donde las herramientas anteriores no tienen alcance.

**¿Cómo se complementan? Cobertura, sinergia y prevención de conflictos**

Cada herramienta cumple un rol específico y se integra en una cadena de calidad, evitando duplicidad de funciones y reforzando mutuamente sus aportes:

- **Roslyn Analyzers** y **StyleCop** trabajan sobre el código C# fuente, pero con enfoques diferentes:
    - *Roslyn* se centra en semántica, convenciones y errores lógicos.
    - *StyleCop* refuerza reglas de estilo, estructura, documentación y nombres.
    - Ambas pueden coexistir porque StyleCop se instala como paquete NuGet y Roslyn es parte del compilador/análisis, permitiendo reglas complementarias y configurables.

- **EditorConfig** actúa como base universal de formato para todos los archivos del proyecto, definiendo reglas de indentación, saltos de línea y codificación. Es interpretado tanto por editores como por herramientas CLI (incluyendo dotnet format y Prettier).
    - *Prevención de conflicto:* Si hay discrepancias, `.editorconfig` tiene prioridad sobre configuraciones internas de editores y formateadores.

- **dotnet format** es el ejecutor automático de las reglas de formato y estilo definidas en `.editorconfig` y los analizadores. Se utiliza como paso final para garantizar que todo el código cumple las reglas antes de integrarse al repositorio.
    - *Sinergia:* Refuerza la coherencia al aplicar de forma masiva las reglas, corrigiendo inconsistencias que puedan haber pasado inadvertidas durante la edición manual.

- **Prettier** se utiliza para archivos no C# (Markdown, JSON, YAML, scripts, etc.), donde las herramientas anteriores no tienen alcance. Así, asegura formato homogéneo en documentación y configuración.
    - *Prevención de duplicidad:* Prettier no interfiere con archivos `.cs`, y `.editorconfig` puede definir reglas base para todos los tipos de archivo.

**Ventajas prácticas de la integración:**

- **Calidad de código:** Se detectan y corrigen errores, inconsistencias y malas prácticas antes de que lleguen a producción.
- **Mantenibilidad:** El código es más fácil de entender, modificar y ampliar gracias a reglas y formato homogéneos.
- **Onboarding acelerado:** Nuevos integrantes del equipo pueden adaptarse rápidamente, ya que las reglas y estilos están automatizados y documentados.
- **Ahorro de tiempo:** Se reduce el tiempo invertido en revisiones manuales, discusiones de estilo y correcciones repetitivas.
- **Consistencia en equipos grandes:** Todos los miembros siguen las mismas reglas, evitando “estilos personales” y asegurando uniformidad a lo largo del tiempo.
- **Integración con CI/CD:** Los linters y formateadores pueden ejecutarse automáticamente en pipelines, bloqueando cambios que no cumplan con los estándares y facilitando la entrega continua.
- **Prevención de deuda técnica:** Al automatizar la calidad y el formato, se reduce la acumulación de problemas a largo plazo.
- **Expectativas para el equipo: adopción y cumplimiento de la convención**

Para que la integración de linters y formateadores aporte todos sus beneficios, se espera que cada integrante del equipo:

- **Adopte las herramientas recomendadas:** Instale y configure las extensiones, scripts y archivos de configuración propuestos en esta guía.
- **Revise y corrija advertencias/sugerencias:** Atienda los mensajes de linters y formateadores antes de realizar commits o pull requests, evitando subir código con errores o advertencias no justificadas.
- **Utilice los scripts y comandos estándar:** Aproveche los scripts y comandos documentados (por ejemplo, `dotnet format`, validaciones en CI/CD, comandos de Prettier) para asegurar la uniformidad antes de integrar cambios.
- **Colabore en la mejora continua:** Proponga ajustes y mejoras a las reglas y configuraciones cuando detecte nuevas necesidades o mejores prácticas.
- **Documente excepciones justificadas:** Si alguna regla debe ser ignorada por motivos válidos, se debe documentar la excepción en el código o en la guía.

> El cumplimiento de estas expectativas es esencial para mantener la calidad, consistencia y sostenibilidad del proyecto a largo plazo.

---

## Herramientas Fundamentales para .NET (C#)

### 1. **Roslyn Analyzers: Analizadores Estáticos de Código en .NET**

**¿Qué es Roslyn?**

Roslyn es el nombre del compilador moderno de C# y Visual Basic para la plataforma .NET. No solo traduce código fuente a binarios, sino que expone una API poderosa para analizar, transformar y generar código. Esta API permite crear herramientas de análisis estático, refactorización y linters personalizados.

**¿Qué son los Roslyn Analyzers?**

Los Roslyn Analyzers son "linters" o analizadores estáticos que funcionan sobre la plataforma Roslyn. Su objetivo es detectar automáticamente problemas de:

-   Estilo de código (nombres, indentación, convenciones)
-   Semántica (mal uso de tipos, patrones peligrosos, errores lógicos)
-   Convenciones y mejores prácticas
-   Potenciales bugs y vulnerabilidades

**¿Cómo funcionan?**

-   Se integran directamente en el flujo de compilación de .NET.
-   Analizan el código fuente mientras editas (en VS, VSCode, Rider) y durante la compilación (`dotnet build`).
-   Reportan advertencias, sugerencias o errores en tiempo real, ayudando a mantener la calidad y coherencia del código.
-   No requieren ser ejecutados exclusivamente mediante `dotnet format`.
    -   `dotnet format` es una herramienta CLI que puede aplicar reglas de formato, pero los analizadores funcionan automáticamente en editores compatibles y durante la compilación.
-   Se pueden extender o personalizar creando tus propios analizadores Roslyn.

**Ejemplo de funcionamiento:**

-   Si escribes una variable con un nombre que no sigue la convención, el analizador lo marcará.
-   Si usas una API obsoleta o peligrosa, puede sugerir alternativas o mostrar advertencias.
-   Si olvidas documentar un método público, puede marcarlo como advertencia o error (según configuración).

**¿Dónde se usan?**

-   Visual Studio (integrado por defecto)
-   Visual Studio Code (con C# Dev Kit / OmniSharp, no disponibles en forks como Coursor o Windsurf)
-   Rider
-   CLI de .NET (`dotnet build`, `dotnet format`, `dotnet analyzers`)
-   Pipelines de CI/CD (Azure DevOps, GitHub Actions, etc.)

**Referencias oficiales:**

-   [Documentación de Roslyn Analyzers (Microsoft)](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
-   [GitHub - dotnet/roslyn-analyzers](https://github.com/dotnet/roslyn-analyzers)
-   [Cómo crear tus propios analizadores](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/roslyn-analyzers-overview)

> **Resumen:** Los Roslyn Analyzers son linters avanzados para .NET, capaces de analizar semántica, estilo y convenciones. Funcionan automáticamente en editores y compilación, y no dependen exclusivamente de `dotnet format` para operar.

### 2. **EditorConfig: Archivo Universal de Configuración de Formato**

**¿Qué es `.editorconfig`?**

`.editorconfig` es un archivo de texto plano, estándar abierto, que permite definir reglas de formato y estilo para los archivos de un proyecto. Su objetivo es mantener la coherencia en el formato del código cuando varios desarrolladores usan diferentes editores o IDEs.

**¿Qué puede definir?**

-   Tamaño de indentación (espacios o tabs)
-   Cantidad de espacios por tabulación
-   Tipo de salto de línea (LF, CRLF)
-   Codificación de archivos (utf-8, utf-16, etc.)
-   Eliminación de espacios finales
-   Inserción de nueva línea al final del archivo

**¿Qué NO puede hacer?**

-   No es un linter ni un formateador: no revisa errores ni aplica formato automáticamente.
-   No define reglas semánticas: no puede exigir nombres de variables, convenciones de documentación, ni reglas de negocio.
-   No es una extensión: es un archivo de configuración. Algunas extensiones pueden mejorar su soporte, pero la mayoría de editores modernos (incluido VS Code) lo soportan de forma nativa.

**¿Cómo funciona?**

-   Los editores compatibles leen el archivo `.editorconfig` al abrir o guardar archivos y aplican las reglas de formato especificadas.
-   Si el editor no lo soporta de base, existen extensiones como "EditorConfig for VS Code" para añadir soporte o mejorar la experiencia.

**Ejemplo básico de `.editorconfig`:**

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true
```

**¿Dónde se coloca?**

-   En la raíz del repositorio, para que todos los archivos y subcarpetas hereden la configuración.
-   Se pueden tener varios `.editorconfig` en subcarpetas para reglas más específicas.

**Referencias oficiales:**

-   [Sitio oficial EditorConfig](https://editorconfig.org/)
-   [Documentación VS Code sobre EditorConfig](https://code.visualstudio.com/docs/editor/codebasics#_editorconfig)

> **Resumen:** `.editorconfig` es un archivo universal para definir reglas de formato de código. No es un linter, ni un formateador, ni una extensión. Es esencial para mantener coherencia en equipos y proyectos colaborativos.

### 3. **C# Dev Kit y OmniSharp: Análisis y Autoformateo en VSCode (no disponibles en forks como Cursor o Windsurf)**

**¿Qué es OmniSharp?**

OmniSharp es un servidor de lenguaje (Language Server Protocol, LSP) para C#, utilizado por editores como Visual Studio Code, Atom, Sublime Text y otros. Proporciona funcionalidades avanzadas como:

-   Análisis de código en tiempo real
-   Navegación y autocompletado
-   Refactorización
-   Detección de errores y advertencias
-   Formateo de código

**¿Qué es C# Dev Kit?**

C# Dev Kit es la extensión oficial moderna de Microsoft para desarrollo en C# en Visual Studio Code. Integra OmniSharp como motor de análisis y añade herramientas adicionales para mejorar la experiencia de desarrollo profesional en .NET.

**¿Cómo funciona el autoformateo?**

-   OmniSharp puede aplicar formato automáticamente al guardar archivos `.cs` si está habilitado en la configuración del editor (`"editor.formatOnSave": true`).
-   Usa las reglas definidas en `.editorconfig` si están presentes en el proyecto, pero también puede aplicar reglas internas o de otras extensiones si no existe `.editorconfig`.
-   El formateo y análisis dependen de que OmniSharp esté correctamente instalado y habilitado en VSCode.

**Configuración recomendada en `settings.json` de VSCode:**

```json
{
    "editor.formatOnSave": true,
    "editor.formatOnType": true,
    "editor.formatOnPaste": true,
    "[csharp]": {
        "editor.defaultFormatter": "ms-dotnettools.csharp"
    },
    "dotnet.server.useOmnisharp": true
}
```

**¿Por qué se recomienda activar `editor.formatOnType` y `editor.formatOnPaste`?**

- **`"editor.formatOnType": true`**
  - **Función:** Aplica el formato automáticamente cada vez que escribes ciertos caracteres clave (como `;`, `{`, `}`) en el código.
  - **Beneficio:** Garantiza que el formato se mantenga correcto mientras programas, sin esperar a guardar el archivo. Es útil para mantener el código limpio y ordenado en tiempo real, ayudando a evitar acumulación de errores de formato.

- **`"editor.formatOnPaste": true`**
  - **Función:** Aplica el formato automáticamente al pegar código en el editor.
  - **Beneficio:** Evita que fragmentos pegados desde otras fuentes (con diferente indentación o estilo) rompan la consistencia del formato del archivo, asegurando que todo el código en el proyecto mantenga el mismo estándar visual.

**Resumen:**
Estas opciones mejoran la experiencia de desarrollo, aseguran que el código siempre esté bien formateado y reducen la necesidad de correcciones manuales posteriores. Son especialmente útiles en equipos o proyectos colaborativos, ya que ayudan a mantener la coherencia y calidad del código desde el primer momento.

**Notas importantes:**

-   El autoformateo solo ocurre si está activado en la configuración del editor.
-   Si hay conflictos entre reglas de `.editorconfig` y las internas del formateador, prevalecen las de `.editorconfig`.
-   La extensión "C# Dev Kit" es la opción moderna y recomendada para desarrollo profesional en C# con VSCode.

**Referencias oficiales:**

-   [Documentación de OmniSharp](https://github.com/OmniSharp/omnisharp-roslyn)
-   [C# Dev Kit en el Marketplace de VS Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
-   [Configuración de formato en VSCode](https://code.visualstudio.com/docs/getstarted/keybindings#_formatting)

> **Resumen:** OmniSharp es el motor de análisis y formateo para C# en VSCode, integrado actualmente a través de la extensión C# Dev Kit. El autoformateo depende de la configuración del editor y de la presencia de `.editorconfig` en el proyecto.

### 4. **StyleCop.Analyzers: Linter de Estilo y Convenciones para C# (no disponibles en forks como Cursor o Windsurf)**

**¿Qué es StyleCop.Analyzers?**

StyleCop.Analyzers es un paquete NuGet que implementa las reglas históricas de StyleCop sobre la plataforma Roslyn. Su propósito es garantizar que el código C# siga convenciones de estilo, formato y documentación coherentes en todo el proyecto.

**¿Qué hace?**

-   Analiza el código fuente en tiempo real (en el editor) y durante la compilación.
-   Marca errores, advertencias y sugerencias relacionados con:
    -   Nombres de variables, clases y métodos
    -   Espaciado, indentación y saltos de línea
    -   Orden y agrupación de miembros
    -   Documentación XML de clases y métodos
    -   Reglas de visibilidad y acceso
-   Permite autocorrección de muchas reglas usando el atajo `Ctrl + .` en VS/VSCode.
-   Proporciona enlaces directos a la documentación oficial de cada regla.

**¿Cómo se configura?**

-   Se instala como paquete NuGet en cada proyecto (`dotnet add package StyleCop.Analyzers`).
-   Se recomienda agregar un archivo de configuración `stylecop.json` en la raíz del repositorio para personalizar reglas (opcional, no obligatorio si se usan las reglas por defecto).
-   Se debe referenciar en el archivo `.csproj` para asegurar que las reglas se apliquen correctamente.

**Ejemplo de configuración en `.csproj`:**

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)\rules.ruleset</CodeAnalysisRuleSet>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="StyleCop.Analyzers" Version="1.1.118">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <AdditionalFiles Include="../stylecop.json" />
</ItemGroup>
```

**Personalización:**

-   El archivo `stylecop.json` permite habilitar, deshabilitar o ajustar reglas específicas según las necesidades del equipo.
-   Ejemplo básico de `stylecop.json`:

```json
{
    "settings": {
        "documentationRules": {
            "companyName": "FRUGO Corp.",
            "documentExposedElements": true,
            "documentInternalElements": false
        }
    }
}
```

**Notas importantes:**

-   No es obligatorio tener `stylecop.json` si se aceptan las reglas por defecto.
-   Tras instalar y configurar, si el análisis no funciona correctamente, ejecutar:
    -   `dotnet clean; dotnet build`

**Referencias oficiales:**

-   [Repositorio oficial StyleCop.Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
-   [Documentación de configuración avanzada](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/Configuration.md)
-   [Lista de reglas StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/AnalyzerConfiguration.md)

> **Resumen:** StyleCop.Analyzers es un linter especializado en estilo y convenciones para C#. Se integra en el flujo de trabajo moderno de .NET y es altamente configurable para adaptarse a las necesidades del equipo o proyecto.

### 5. **dotnet format: Formateo Automatizado de Código en .NET**

**¿Qué es `dotnet format`?**

`dotnet format` es la herramienta oficial de línea de comandos (CLI) de Microsoft para aplicar reglas de formato y estilo en proyectos .NET (principalmente archivos `.cs`). Permite mantener el código limpio, homogéneo y alineado con las convenciones del equipo, sin intervención manual.

**¿Qué hace?**

-   Aplica automáticamente las reglas de formato definidas en el archivo `.editorconfig` del repositorio.
-   Corrige indentación, espacios, saltos de línea, finales de archivo, y otros aspectos de formato.
-   Puede detectar y corregir problemas menores de estilo antes de que lleguen al control de versiones.
-   Utiliza los analizadores instalados en el proyecto para reforzar las reglas.

**¿Cómo se usa?**

-   Se ejecuta desde la terminal en la raíz del proyecto o solución:

```sh
dotnet format
```

-   Puede ejecutarse sobre proyectos individuales o soluciones completas:

```sh
dotnet format MyProject.csproj
```

```sh
dotnet format MySolution.sln
```

#### Uso de argumentos adicionales en `dotnet format`

`dotnet format` acepta varios argumentos para personalizar su comportamiento. Los más útiles son:

- `--include <archivos o carpetas>`: Formatea solo los archivos o carpetas indicados.
- `--exclude <archivos o carpetas>`: Excluye archivos o carpetas del formateo.
- `--verify-no-changes`: Solo verifica si el código cumple con el formato, sin modificar archivos (muy útil en pipelines de CI/CD).
- `--severity <nivel>`: Especifica el nivel de severidad (`error`, `warning`, `info`, `hidden`) para aplicar reglas.

**Ejemplo práctico:**

```sh
dotnet format --exclude bin,obj,migrations --verify-no-changes --severity warning
```

- Excluye las carpetas `bin`, `obj` y `migrations` del formateo.
- Solo verifica el formato (no modifica archivos).
- Solo aplica reglas con severidad `warning` o superior.

#### ¿dotnet format puede excluir directorios? ¿Es recomendable analizar todo?

Sí, `dotnet format` puede excluir directorios usando el argumento `--exclude`.
Esto es especialmente útil para evitar analizar carpetas generadas automáticamente (como `bin`, `obj`, `node_modules`, `migrations`, etc.) que no deben ser formateadas, ya que contienen archivos de salida, dependencias o artefactos temporales.

**Ejemplo:**

```sh
dotnet format --exclude bin,obj
```

#### ¿Existe un archivo tipo `.ignore` para dotnet format, como `.gitignore` o `.prettierignore`?

Actualmente, **`dotnet format` no soporta un archivo de exclusión propio** (tipo `.dotnetformatignore` o similar) al estilo de `.gitignore` o `.prettierignore`.
La única forma de excluir archivos o carpetas es usando el argumento `--exclude` directamente en la línea de comandos o en los scripts de automatización.

**¿Qué implica esto en la práctica?**
- No puedes mantener una lista persistente de exclusiones en un archivo dedicado dentro del repositorio para `dotnet format`.
- Si necesitas exclusiones constantes, deberás incluir el argumento `--exclude` con las rutas deseadas cada vez que ejecutes el comando o documentar las exclusiones en los scripts de CI/CD.

**Buenas prácticas recomendadas:**
- Documenta en tu `README.md` o en la guía de linters cuáles carpetas deben ser excluidas y proporciona el comando completo recomendado.
- Si automatizas el formateo en pipelines, mantén el argumento `--exclude` actualizado en el workflow.
- Si en el futuro `dotnet format` implementa soporte para archivos de exclusión, actualiza la guía y la configuración del repositorio.

**Referencia oficial y discusión:**
- [Opciones de dotnet format (Microsoft)](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-format#options)
- [Issue oficial sobre soporte de archivos ignore en dotnet format (GitHub)](https://github.com/dotnet/format/issues/926)
    > En este issue se discute la posibilidad de implementar un archivo tipo `.dotnetformatignore`, pero al momento de esta guía, no está soportado.

**Recomendación:**
No es recomendable analizar todo el repositorio. Siempre excluye carpetas de salida, dependencias o archivos generados automáticamente para ahorrar tiempo y evitar errores innecesarios. Esto mejora la velocidad del análisis y reduce el riesgo de formatear archivos irrelevantes o temporales.

**Referencias oficiales:**
- [Opciones de dotnet format (Microsoft)](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-format#options)

> Consulta siempre la ayuda oficial para ver todos los argumentos disponibles:
>
> ```sh
> dotnet format --help
> ```

**Integración en CI/CD:**

-   Es una práctica recomendada ejecutar `dotnet format` en pipelines de integración continua (CI) para asegurar que todo el código subido cumpla con las reglas de formato.
-   Puede configurarse como un paso obligatorio antes de aceptar pull requests.

**Ejemplo de integración en GitHub Actions:**

```yaml
- name: Formatear código con dotnet format
  run: dotnet format --verify-no-changes
```

**Referencias oficiales:**

-   [Documentación dotnet format (Microsoft)](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-format)
-   [Opciones avanzadas de dotnet format](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-format#options)

> **Resumen:** `dotnet format` es la herramienta CLI estándar para aplicar reglas de formato y estilo en proyectos .NET. Es fundamental para mantener la calidad y coherencia del código, tanto en desarrollo local como en automatización CI/CD.

### 6. **Directory.Build.props: Centralización de Configuración en Proyectos .NET Multiproyecto**

**¿Qué es `Directory.Build.props`?**

`Directory.Build.props` es un archivo especial de MSBuild (el sistema de construcción de .NET) que permite centralizar y compartir configuraciones comunes entre múltiples proyectos (`.csproj`) dentro de un mismo repositorio. Es especialmente útil en soluciones grandes o arquitecturas monorepo, donde varios proyectos deben seguir las mismas reglas y convenciones.

**¿Para qué sirve?**

-   Evita duplicidad de configuración en cada archivo `.csproj`.
-   Garantiza consistencia en reglas de compilación, estilo, formato, versiones y metadatos.
-   Facilita el mantenimiento: un solo cambio en `Directory.Build.props` se replica en todos los proyectos que lo heredan.

**¿Cómo funciona?**

-   MSBuild busca automáticamente un archivo llamado `Directory.Build.props` en el directorio actual y en todos los directorios superiores hasta la raíz del disco.
-   Todas las propiedades y elementos definidos en este archivo se aplican a todos los proyectos hijos, a menos que sean explícitamente sobrescritos en un `.csproj` individual.

**Ejemplo de uso:**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors>CS1591</WarningsAsErrors>
    <Company>FRUGO Corp.</Company>
    <Authors>FRUGO Team</Authors>
    <Product>API name</Product>
    <Copyright>Copyright © FRUGO Corp. 202X</Copyright>
    <RepositoryUrl>https://github.com/xxxxxx/xxxxxxxx</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)\rules.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" Version="1.1.118">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)stylecop.json" />
  </ItemGroup>
</Project>
```

**Ventajas:**

-   Consistencia total en equipos grandes y repositorios con múltiples proyectos.
-   Reducción significativa de errores por configuración divergente.
-   Facilita la adopción de nuevas reglas o convenciones en todo el ecosistema de la solución.

**Referencias oficiales:**

-   [Documentación Directory.Build.props (Microsoft)](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)
-   [Centralización de configuración en MSBuild](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props)

> **Resumen:** `Directory.Build.props` es la forma recomendada y moderna de centralizar la configuración de proyectos en soluciones .NET multiproyecto. Su uso asegura coherencia, mantenibilidad y escalabilidad en equipos y arquitecturas complejas.

---

## Herramientas para Otros Archivos (No .cs)

### 7. **Prettier: Formateador de Código Universal para Archivos de Configuración y Documentación**

**¿Qué es Prettier?**

Prettier es una herramienta de formateo automático de código fuente para múltiples lenguajes y tipos de archivos, especialmente útil para archivos de configuración, scripts y documentación: `.js`, `.ts`, `.json`, `.yaml`, `.xml`, `.md`, entre otros.

**¿Qué hace?**

-   Aplica reglas estrictas y predecibles de formato: indentación, saltos de línea, espaciado, comillas, etc.
-   Asegura que todo el código y la documentación tenga un formato homogéneo, sin importar el editor o el desarrollador.
-   Facilita la revisión de cambios en pull requests, ya que elimina debates sobre estilo y formato.

**¿Qué NO hace?**

-   No es un linter clásico: no detecta errores de estilo, malas prácticas, ni problemas semánticos.
-   No valida nombres de variables, reglas de negocio ni convenciones de código.
-   Su único objetivo es el formato visual del archivo.

**¿Cómo se usa?**

-   Se instala como extensión en VS Code: **Prettier - Code formatter** (recomendado).
-   Se puede ejecutar desde la línea de comandos para formatear archivos de forma masiva.
-   Se configura con un archivo `.prettierrc` o `prettierrc.json` en la raíz del repositorio.

**Ejemplo de configuración (`.prettierrc`):**

```json
{
    "printWidth": 100,
    "tabWidth": 4,
    "useTabs": false,
    "semi": true,
    "singleQuote": true,
    "trailingComma": "es5",
    "bracketSpacing": true,
    "arrowParens": "always",
    "endOfLine": "lf"
}
```

> **Nota:** El archivo real debe ser JSON válido, sin comentarios.

**Ventajas:**

-   Ahorra tiempo y elimina discusiones sobre formato en equipos.
-   Compatible con la mayoría de editores y sistemas operativos.
-   Muy útil para proyectos con archivos de configuración y documentación extensos.

**Limitaciones:**

-   No sustituye a linters como ESLint, TSLint, StyleCop o Roslyn Analyzers para validación de reglas, semántica o buenas prácticas.
-   Si necesitas reglas de negocio, nombres, convenciones o advertencias, deberás combinar Prettier con un linter tradicional.

**Referencias oficiales:**

-   [Sitio oficial Prettier](https://prettier.io/)
-   [Extensión Prettier para VS Code](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode)
-   [Opciones de configuración](https://prettier.io/docs/en/options.html)

> **Resumen:** Prettier es un formateador universal, no un linter clásico. Su función es garantizar un formato visual homogéneo en archivos de configuración y documentación, simplificando la colaboración y la revisión de código.

## Prioridades y Prevalencia entre Linters/Formateadores

-   **Archivos .cs:**
    -   El formato y estilo lo define `.editorconfig`, pero StyleCop y Roslyn Analizers prevalecen en reglas de semántica y convenciones.
    -   `dotnet format` es la herramienta final para aplicar todo de forma uniforme.
-   **Archivos de configuración y documentación:**
    -   Prettier tiene la última palabra en formato visual.
    -   Si hay conflicto, Prettier sobrescribe el formato, pero `.editorconfig` puede definir reglas base (saltos de línea, indentación general).

---

## Buenas Prácticas

-   Instala y configura todas las extensiones recomendadas.
-   Mantén los archivos `.editorconfig`, `.prettierrc`, `stylecop.json` y `Directory.Build.props` bajo control de versiones.
-   Ejecuta `dotnet clean; dotnet build` tras cambios de configuración.
-   Revisa y corrige advertencias/sugerencias de linters antes de hacer commit.
-   Configura tu `settings.json` de VS Code para máxima integración. Ejemplo:

```json
{
    "explorer.confirmDelete": false,
    "security.workspace.trust.untrustedFiles": "open",
    "editor.formatOnSave": true,
    "editor.defaultFormatter": "esbenp.prettier-vscode",
    "[csharp]": {
        "editor.defaultFormatter": "ms-dotnettools.csharp"
    },
    "editor.formatOnType": true,
    "editor.formatOnPaste": true,
    "extensions.ignoreRecommendations": true,
    "dotnet.server.useOmnisharp": true
}
```

---

## Referencias y Recursos

-   [Documentación oficial EditorConfig](https://editorconfig.org/)
-   [Documentación oficial Prettier](https://prettier.io/docs/en/index.html)
-   [StyleCop Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
-   [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-format)
-   [OmniSharp para VS Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

---

> Esta guía es de referencia viva. Si encuentras mejores prácticas, nuevas herramientas o mejoras, actualízala y compártela con el equipo.
