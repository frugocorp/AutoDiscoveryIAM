# Convención: Prohibición de uso de librerías con licencia comercial o restrictiva

## Fuente: FOSSED

Este documento se basa en información recopilada de **FOSSED** (Free and Open Source Software Ecosystem Database), una iniciativa que monitorea y documenta los cambios de licenciamiento en librerías populares del ecosistema .NET y otras tecnologías. FOSSED ayuda a los desarrolladores a identificar dependencias que han cambiado a modelos de licencia comercial o restrictiva, y sugiere alternativas libres y abiertas.

Para referencias actualizadas y consultar la base de datos completa, visita: [https://dariusz-wozniak.github.io/fossed/](https://dariusz-wozniak.github.io/fossed/)

## Librerías afectadas

A continuación se muestra una tabla resumen de librerías afectadas por cambios de licencia, su última versión libre, alternativas recomendadas y enlaces oficiales:

| Framework        | Licencia anterior | Última versión libre | Alternativas principales                                                   | Sitio web oficial                                                        |
| ---------------- | ----------------- | -------------------- | -------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Moq              | BSD-3-Clause      | 4.18.4               | NSubstitute, FakeItEasy                                                    | [GitHub](https://github.com/devlooped/moq)                               |
| FluentAssertions | Apache 2.0        | 7.x                  | Shouldly, Awesome Assertions, métodos estándar de Assert                   | [fluentassertions.com](https://fluentassertions.com/)                    |
| AutoMapper       | MIT               | Actual               | Mapperly, Mapster, ForgeMap, mapeo manual                                  | [automapper.org](https://automapper.org/)                                |
| MediatR          | Apache 2.0        | Actual               | Mediator, Brighter                                                         | [GitHub](https://github.com/jbogard/MediatR)                             |
| MassTransit      | Apache 2.0        | 8.x                  | NServiceBus, Rebus, Brighter, Wolverine                                    | [masstransit.io](https://masstransit.io/)                                |
| NBomber          | Apache 2.0        | 4.x                  | Netling, Artillery, JMeter, k6, Azure Load Testing, etc.                   | [nbomber.com](https://nbomber.com/)                                      |
| IdentityServer   | Apache 2.0        | 4.x                  | OpenIddict, Keycloak, Azure Active Directory B2C, etc.                     | [duendesoftware.com](https://duendesoftware.com/products/identityserver) |
| ImageSharp       | Apache 2.0        | 2.x                  | SkiaSharp, Magick.NET, System.Drawing.Common                               | [sixlabors.com](https://sixlabors.com/products/imagesharp/)              |
| NPOI             | Apache 2.0        | 2.7.4                | ClosedXML, Open XML SDK, ExcelDataReader, EPPlus (comercial)               | [GitHub](https://github.com/nissl-lab/npoi)                              |
| WiX Toolset      | MS-RL             | 5.x                  | WiX v5 (pin), Inno Setup, NSIS, Velopack, Visual Studio Installer Projects | [wixtoolset.org](https://wixtoolset.org/)                                |

## Frameworks a omitir o fijar versión

Para evitar riesgo de licencia comercial o restrictiva, aplicar estas reglas de gobierno:

- **NPOI**:
  - Omitir `>= 2.8.0` en organizaciones sujetas a pago de OSMF.
  - Fijar en `2.7.4` si se requiere versión libre conocida.
  - Evitar `2.7.5` por el incidente de dependencia `NSax` (LGPL-3.0).
- **WiX Toolset**:
  - Omitir `>= 6.0.0` si no se adoptará OSMF.
  - Fijar en `5.x` o migrar a alternativas libres (Inno Setup, NSIS, Velopack, VS Installer Projects).
- **MassTransit**:
  - Mantener `8.x` si no se adopta licencia comercial de `9.x`.
- **Moq**:
  - Aunque se puede utilizar evitando versiones con `SponsorLink` es imperativo preferir NSubstitute/FakeItEasy para evitar posibles cambios futuros.

> Regla operativa: antes de introducir una librería o actualizar major version, validar en FOSSED su estado de licencia y alternativas.

## Últimas Noticias

- **2026-02-04:** [What's New in NPOI 2.8.0: A New Chapter for Sustainable Open Source](https://tonyqus.medium.com/whats-new-in-npoi-2-8-0-a-new-chapter-for-sustainable-open-source-e0a9ae383a07)
  - NPOI 2.8.0 introduce modelo OSMF (Open Source Maintenance Fee). También se elimina NSax para corregir conflicto previo de licencia.

- **2025-09-30:** [Open Source Maintenance Fee v1.1](https://robmensching.com/blog/posts/2026/02/04/osmf-v11/)
  - Actualización de OSMF EULA para WiX Toolset con umbral mínimo de ingresos anuales (USD 10,000) antes de aplicar el fee.

- **2025-07-08:** [NSax dependency introduces LGPL-3.0 into Apache-2.0 NPOI (2.7.5)](https://github.com/nissl-lab/npoi/issues/1652)
  - NPOI 2.7.5 incorporó NSax (LGPL-3.0), generando incompatibilidad para equipos que restringen dependencias (L)GPL. NSax fue removido posteriormente en 2.8.0.

- **2025-07-02:** [AutoMapper and MediatR Roadmaps](https://www.jimmybogard.com/automapper-and-mediatr-roadmaps/)
  - Publicación de roadmap tras comercialización de AutoMapper y MediatR.

- **2025-05-12:** [AutoMapper and MediatR Commercial Editions Launch Today](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/)
  - Lanzamiento oficial de ediciones comerciales para AutoMapper y MediatR.

- **2025-05-12:** [Open Source Maintenance Fee Two Months In](https://robmensching.com/blog/posts/2025/05/12/open-source-maintenance-fee-two-months-in/)
  - Balance inicial del modelo OSMF en WiX Toolset.

- **2025-04-07:** [AutoMapper and MediatR Licensing Update](https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/)
  - Detalles de licenciamiento y transición comercial.

- **2025-04-05:** [WiX v6.0.0 available](https://www.firegiant.com/blog/2025/4/7/wix-v600-available/)
  - WiX 6 mantiene código abierto, pero con enforcement de OSMF para organizaciones con ingresos.

- **2025-04-16:** [Actualización de licenciamiento de AutoMapper y MediatR](https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/)
  - Jimmy Bogard anuncia la decisión de comercializar AutoMapper y MediatR para asegurar su sostenibilidad a largo plazo. El post enfatiza la transparencia en el proceso. Etiquetas: [automapper] [mediatr]

- **2025-04-05:** [Milan Jovanović: MediatR y MassTransit se vuelven comerciales: Qué significa para ti](https://www.milanjovanovic.tech/blog/mediatr-and-masstransit-going-commercial-what-this-means-for-you)
  - Importantes cambios en el ecosistema .NET: MediatR, AutoMapper y MassTransit migran a licencias comerciales. Fluent Assertions también anunció planes similares. Etiquetas: [mediatr] [masstransit] [automapper]

- **2025-04-03:** [Anuncio de MassTransit v9](https://masstransit.io/introduction/v9-announcement)
  - MassTransit v9 adopta un modelo de licencia comercial, buscando sostenibilidad e innovación continua. La versión 8 permanece open source para la comunidad. Etiqueta: [masstransit]

- **2025-04-02:** [Jimmy Bogard: AutoMapper y MediatR se vuelven comerciales](https://www.jimmybogard.com/automapper-and-mediatr-going-commercial/)
  - Para asegurar la sostenibilidad a largo plazo de sus proyectos OSS, Jimmy Bogard anuncia la comercialización de AutoMapper y MediatR. Etiquetas: [automapper] [mediatr]

- **2025-03-06:** [IdentityServer4 vuelve a ser público](https://blog.duendesoftware.com/posts/20250306-identityserver4-public-again/)
  - El equipo de Duende reafirma su compromiso con soluciones seguras y estándares para .NET. IdentityServer4, aunque ahora público, está fuera de soporte y contiene vulnerabilidades conocidas. Etiqueta: [identityserver]

- **2025-02-27:** [Entendiendo la eliminación de IdentityServer4 y sus alternativas](https://www.featbit.co/articles2025/identityserver4-removed-github-alternatives)
  - IdentityServer4 fue retirado de GitHub y migró a un modelo comercial bajo el nombre Duende IdentityServer, debido a los altos costos de mantenimiento. Desde noviembre de 2022, la mayoría de los casos de uso requieren licencia comercial. Etiqueta: [identityserver4]

- **2025-01-16:** [Dev Class: Otro proyecto open source cambia a licencia restrictiva: Fluent Assertions tras asociación con Xceed](https://devclass.com/2025/01/16/another-open-source-project-shifts-to-restrictive-license-fluent-assertions-following-xceed-partnership/)
  - Fluent Assertions, antes open source bajo Apache 2.0, ahora requiere licencia paga para uso comercial. Etiqueta: [fluent-assertions]

- **2025-01-15:** [Nick Chapsas: Deja de usar FluentAssertions ahora (YouTube)](https://www.youtube.com/watch?v=A06nNjBKV7I)
  - Video donde se explica el cambio de licencia de Fluent Assertions v8, que obliga a las empresas a pagar $130 USD por desarrollador al año. Etiqueta: [fluent-assertions]

- **2024-08-09:** [¿Qué pasa con IdentityServer4 y qué alternativas elegir?](https://modlogix.com/blog/identityserver4-alternatives-best-options-and-the-near-future-of-identityserver/)
  - IdentityServer fue renombrado y cambió su política de monetización desde noviembre de 2022. Etiqueta: [identityserver]

- **2023-08-15:** [Daniel Cazzulino: SponsorLink, retroalimentación y próximos pasos](https://www.cazzulino.com/sponsorlink-feedback.html)
  - Reflexión sobre la sostenibilidad del open source y experiencias personales con SponsorLink y Moq. Etiqueta: [moq]

- **2023-08-09:** [YouTube: ¡Elimina Moq de tus proyectos .NET ahora! (YouTube)](https://www.youtube.com/watch?v=A06nNjBKV7I)
  - Nick Chapsas analiza el caso Moq y SponsorLink, que ha causado polémica en la comunidad .NET. Etiqueta: [moq]

- **2023-08-08:** [Coding Bolt: Un análisis profundo de SponsorLink: implicaciones para open source y privacidad](https://codingbolt.net/2023/08/08/a-deep-dive-into-sponsorlink-implications-for-open-source-and-privacy/)
  - SponsorLink, integrado en Moq 4.20, busca promover el patrocinio de proyectos open source, pero ha generado debate sobre privacidad y sostenibilidad. Etiqueta: [moq]

- **2022-07-16:** [Cambios de licencia para productos Six Labors](https://sixlabors.com/posts/license-changes/)
  - Las librerías de Six Labors, como ImageSharp, requieren gran esfuerzo de desarrollo y mantenimiento, lo que llevó a cambios en su modelo de licenciamiento. Etiqueta: [imagesharp]

- **2020-10-01:** [El futuro de IdentityServer](https://leastprivilege.com/2020/10/01/the-future-of-identityserver/)
  - Brock Allen y Dominick Baier anuncian cambios importantes en IdentityServer tras más de 10 años de desarrollo, explicando los motivos detrás de la transición. Etiqueta: [identityserver]

## FAQ

### ¿De qué trata este sitio?

Este sitio es un centro de recursos para desarrolladores que buscan comprender y gestionar licencias de software open source. Ofrece información sobre distintos tipos de licencias, su uso y la gestión de proyectos. El objetivo es brindar el razonamiento detrás de las licencias, permitiendo tomar decisiones informadas. No se desalientan las licencias comerciales, sino que se busca fomentar la comprensión para mejores elecciones. Este sitio es un proyecto independiente de un solo desarrollador y no está afiliado a ninguna licencia específica.

### Una librería se volvió comercial. ¿Cuáles son mis opciones?

¡No entres en pánico! 😎 Primero, revisa los precios y considera pagar si valoras la librería y planeas seguir usándola. A menudo puedes mantenerte en la última versión gratuita fijando su versión. Si la licencia lo permite, bifurcar (fork) la librería para mantenimiento personal es otra opción. Finalmente, explora alternativas open source.

### ¿Por qué existe sentimiento negativo hacia algunas librerías que se volvieron comerciales?

El sentimiento negativo surge principalmente por la forma en que se realiza el cambio. Por ejemplo, Moq generó polémica por recolectar correos electrónicos y hacer llamadas de red en secreto a través de SponsorLink. FluentAssertions también recibió críticas por un cambio abrupto y costoso de licencia, incluyendo cuestionamientos sobre contribuciones previas. Los principales problemas son: falta de transparencia, cambios repentinos, privacidad dudosa (Moq) y percepción de falta de respeto. Ejemplos positivos como MassTransit (versionado claro y planes públicos), ImageSharp (licencia dual justa) y Jimmy Bogard (MediatR, AutoMapper) con comunicación transparente y sin penalizaciones, muestran que la transparencia, el respeto y la comunicación clara facilitan la aceptación. El proceso de comercialización debe manejarse cuidadosamente para evitar reacciones negativas.

### ¿Cómo puedo contribuir a este proyecto?

Si deseas proponer cambios, agregar información o contribuir de cualquier manera, visita el repositorio de GitHub en https://github.com/dariusz-wozniak/fossed. Allí puedes crear issues para sugerencias o problemas, o hacer un pull request. ¡Todas las contribuciones son bienvenidas y apreciadas!
