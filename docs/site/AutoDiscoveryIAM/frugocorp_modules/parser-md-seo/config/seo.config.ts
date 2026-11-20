import type { SeoConfig } from '@ngx-docs-markdown-kit/parser-md-seo';

/** Defaults de SEO del sitio -- edita este archivo directamente (requiere recompilar) para
 *  personalizarlos, NO un JSON publico pedido por HTTP en runtime: los meta tags/JSON-LD deben
 *  quedar en el HTML servido desde el primer render para que un crawler los indexe, sin depender
 *  de que una peticion asincrona resuelva a tiempo (ver provideSeoConfig() en
 *  @ngx-docs-markdown-kit/parser-md-seo). Solo hace falta declarar las claves que quieras
 *  personalizar -- el resto cae al default de la libreria (ver DEFAULT_SEO_CONFIG). */
export const SEO_CONFIG: Partial<SeoConfig> = {
  description: "Autenticación JWT y RBAC dinámico para APIs de ASP.NET Core: los permisos se descubren solos a partir de tus endpoints.",
  keywords: "AutoDiscoveryIAM, ASP.NET Core, minimal APIs, RBAC dinámico, JWT, permisos, NuGet",
  // Banner (Open Graph, panoramico) + cuadrada -- ver README de parser-md-seo para el "por que"
  // de 2 imagenes en vez de una sola. Reemplaza ambas por las tuyas cuando tengas tu propio
  // branding (mismos tamanos recomendados: 1200x630 y 1080x1080).
  ogImages: [
    { url: '/images/FrugoCorp-Logo-1200x630.jpg', width: 1200, height: 630, type: 'image/jpeg', alt: 'AutoDiscoveryIAM' },
    { url: '/images/FrugoCorp-Logo-1080x1080.jpg', width: 1080, height: 1080, type: 'image/jpeg', alt: 'AutoDiscoveryIAM' },
  ],
  organizationLogo: '/images/FrugoCorp-Logo-1080x1080.jpg',
  // null hasta que configures tu dominio real -- sin esto, canonical/og:url/og:image/JSON-LD se
  // omiten (no hay forma de adivinar el dominio final en build-time).
  siteUrl: "https://autodiscoveryiam.frugocorp.com",
  author: "FrugoCorp",
  socialLinks: ["https://github.com/frugocorp/AutoDiscoveryIAM"],
  // Coincide con <html lang="es"> de index.html -- si cambias uno, cambia el otro.
  contentLanguage: 'es',
  // Legado (solo IE lo respeta) -- inofensivo en cualquier navegador moderno, se queda por
  // completitud (ver local/project/meta-example.html, la referencia de diseno de este bloque).
  xUaCompatible: 'IE=edge',
  // Sin handle real que poner por default -- completa el tuyo si tu sitio tiene cuenta de X/Twitter.
  twitterSite: null,
  favicons: [
    { rel: 'icon', href: '/images/FrugoCorp-Icon-128x128.png', type: 'image/png', sizes: '128x128' },
    { rel: 'apple-touch-icon', href: '/images/FrugoCorp-Icon-128x128.png', sizes: '128x128' },
    // Sin "mask-icon" por default -- exige un SVG monocromo/silueta (Safari pinned tab), distinto
    // de un logo a color normal; agrega el tuyo (rel: 'mask-icon', color: '#tu-color') si tienes uno.
  ],
  manifest: '/site.webmanifest',
  msapplicationTileColor: '#ffffff',
};
