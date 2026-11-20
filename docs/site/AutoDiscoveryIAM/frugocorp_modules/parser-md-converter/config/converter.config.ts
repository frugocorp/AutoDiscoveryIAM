import type { ConverterConfig } from '@ngx-docs-markdown-kit/parser-md-converter';

// "levels" lo calcula scaffoldSite() UNA SOLA VEZ al crear el sitio (deteccion automatica del repo
// git que lo contiene, no una garantia -- revisa/corrige si tu convencion de carpetas es distinta,
// ver documentacion de parser-md-converter). El primero SIEMPRE es este mismo sitio; si tu repo NO
// es un monorepo (este sitio ES la raiz), el 2do nivel termina apuntando al mismo archivo y no hace
// nada (dedup automatico). Si mas adelante separas este sitio de un paquete de npm publicable en
// otra carpeta (ej. "../mi-paquete"), agrega esa ruta como un nivel mas, EN ORDEN, entre estos 2.
// El FLAVOR de cada nivel NO vive aca -- se declara en el "parser-md-converter.typeReadme" del
// propio package.json de ese nivel (default implicito: "github", no hace falta declararlo). El
// resto lo completa el equipo dev a mano cuando quiera activar cada pieza (badges, logo propio,
// mas idiomas).
export const CONVERTER_CONFIG = {
  title: 'AutoDiscoveryIAM',
  logo: '/images/FrugoCorp-Logo-300x300.jpg',
  // URL real del sitio -- null (default) cae a buscar "homepage" en el package.json de algun nivel
  // (lo normal si tu proyecto SI es un paquete de npm). Seteala a mano si tu proyecto no tiene
  // ningun package.json en "levels" (ej. una libreria .NET/NuGet) -- ver docs de parser-md-converter.
  siteUrl: 'https://autodiscoveryiam.frugocorp.com',
  levels: [
    { outputPath: 'README.md', path: '.', maxBytes: null },
    { outputPath: 'README.md', path: '../../..', maxBytes: null },
  ],
  nuget: {
    maxBytes: 1_000_000,
    // Ruta real donde ademas escribir el flavor "nuget" -- tipicamente el archivo que tu .csproj
    // empaqueta de verdad (ej. "../../../docs/nuget/README.md"). null (default): ese flavor sigue
    // quedando solo en "generated/README.nuget.md", como siempre.
    outputPath: '../../../docs/nuget/README.md',
  },
  badges: {
    stars: null,
    issues: null,
    docker: null,
    deepwiki: null,
    // "nuget": nombre real del paquete en NuGet (badge + link a nuget.org). "version": un string
    // literal, para cuando la version real vive en un .csproj que este paquete no sabe leer.
    nuget: 'FrugoCorp.AspNetCore.AutoDiscoveryIAM',
    version: null,
  },
  languages: [{ code: 'es', label: 'Español', file: 'README.md' }],
  // Badges automaticos de version/node/npm desde tu package.json -- apagalo si ese package.json es
  // el del sitio Angular, no el paquete real que estas documentando (ej. una libreria .NET).
  autoBadges: false,
  // Bloque "**Dependencias:**" del header -- mismo motivo que "autoBadges" para apagarlo.
  showDependencies: false,
  // Agrega " de {title}" a cada heading/backlink/anchor/indice -- apagalo en un README de un solo
  // proyecto (sin riesgo de choque de anchors con otro proyecto combinado en el mismo README).
  qualifyHeadings: false,
  // La pagina "home" sale como una seccion mas por default -- en "true", sale como introduccion
  // justo debajo del header, sin heading/backlink/indice propios.
  homeAsIntro: true,
} satisfies ConverterConfig;
