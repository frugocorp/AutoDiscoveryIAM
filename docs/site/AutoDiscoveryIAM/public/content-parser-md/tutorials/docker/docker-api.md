---
[slug]: docker-api
[title]: Docker: api/
[description]: Cómo levantar en un contenedor la API del host de ejemplo.
[keywords]: Docker, API, Docker Compose, SampleHost
[readme_flavors]: github, npmjs
---

# Docker — api/

## Desarrollo (por defecto)

Entra a la carpeta:

````code-block
```bash
cd docker/api
```
````

Construye y levanta el contenedor:

````code-block
```bash
docker-compose up --build
```
````

Docker Compose fusiona automáticamente `docker-compose.yml` (base) con
`docker-compose.override.yml` (que sí existe en este directorio) cuando no se especifica
`-f` explícitamente. `docker-compose.override.yml` define `target: dev` para la API, así que
este comando **siempre levanta la imagen de desarrollo** (`dotnet watch run`, con el código
fuente montado como volumen), sin importar lo que diga `docker-compose.yml` por sí solo.

---

## Producción

Para levantar solo la imagen de producción (`target: final`, sin `dotnet watch` ni montaje de
código fuente), hay que excluir explícitamente el override:

Entra a la carpeta:

````code-block
```bash
cd docker/api
```
````

Construye y levanta solo la imagen de producción, excluyendo el override de desarrollo:

````code-block
```bash
docker-compose -f docker-compose.yml up --build -d
```
````

`docker-compose.yml` fija `target: final` de forma explícita para este servicio — antes no lo
hacía, y como el `Dockerfile` tiene la etapa `dev` como última etapa textual, Docker usaba `dev`
como destino implícito incluso al excluir el override. Con `target: final` explícito, este
comando siempre construye y levanta la imagen de producción real.

---

## Configuración (`.env`)

Copiar `.env.template` a `.env` y completar los valores antes de levantar cualquiera de los dos
modos. Para qué sirve cada variable, ver los comentarios en `docker-compose.yml` (el archivo que
sí se trackea, a diferencia de `.env`) — no se repiten aquí para no mantener la misma explicación
en dos lugares.

---

## Reutilizar esta carpeta en otro proyecto

Pensada para copiarse tal cual (`docker/api/` completa: `Dockerfile`, `docker-compose.yml`,
`.override.yml`, `.env.template`) a cualquier otro proyecto .NET:

- **Solo con ajustar `.env`** (`API_BUILD_CONTEXT`, `API_DOCKERFILE_PATH`, `API_PROJECT_PATH`,
  `API_PROJECT_NAME`, puertos, feed local de NuGet si aplica) ya apunta al proyecto nuevo — el
  `Dockerfile` es genérico, no tiene el nombre de ningún proyecto ni paquete hardcodeado.
- **Lo único que sí hay que buscar y reemplazar a mano** son los nombres que Docker Compose no
  permite templatizar con variables (confirmado en la práctica: Compose valida las llaves del YAML
  contra su esquema antes de sustituir variables) — el nombre del servicio (`adiam-api`) en los 3 archivos. La red `edge-net` **no** se renombra:
  es la red compartida de todo el servidor que crea `nginx-server-docker`, y la usan todos los
  proyectos que ese proxy publica.

---

## Base de datos (no incluida)

Este `docker-compose` **no** levanta ningún contenedor de SQL Server — solo el contenedor de la
API. Necesitas una SQL Server alcanzable desde el contenedor (tu propia instancia local, un
servidor remoto, o el contenedor desechable de [`../db/`](/tutorials/docker/docker-db) pensado justo para
esto), apuntada vía `ConnectionStrings__DefaultConnection` en `appsettings.json` o como variable
de entorno. Sin esto, el contenedor arranca, pero la conexión a base de datos
falla en el arranque (reintentos con backoff, ver `Program.cs` del `SampleHost`) y la app no queda
operativa.

---

## Proxy reverso compartido

Este `docker-compose.yml` no publica ningún puerto al host, a propósito: la única forma de llegar
al contenedor es por la red Docker `edge-net`. Esa red **no** la crea este stack (`external: true`):
la crea el proxy reverso compartido del servidor, [`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker), un repositorio
aparte que se encarga del TLS, de los certificados y de las plantillas `.conf` que hacen
`proxy_pass` hacia `adiam-api:API_PORT_CONTAINER`. Por eso ese stack tiene que estar levantado
**antes** que este: si la red no existe, `docker compose up` falla con un error claro en vez de
crear una red propia.

Si necesitas pegarle al contenedor directo para depurar, agrega un `ports:` temporal en un archivo
de Compose adicional (`docker compose -f docker-compose.yml -f tu-archivo.yml up`), sin versionarlo,
y bórralo después.

### `UseForwardedHeaders()` es obligatorio detrás de un proxy que termina TLS

Como el tráfico llega por un proxy que termina TLS y reenvía tráfico plano al contenedor,
tu propio `Program.cs` necesita `app.UseForwardedHeaders();` **antes** de `app.UseAutoDiscoveryIAM();`
— sin esto, `Request.Scheme` siempre se ve como `http` (lo que tu API recibe internamente), aunque
el sitio sea `https` desde afuera, y eso rompe cosas como las URLs absolutas que genera Swagger
UI/Scalar. Ver el aviso 💡 correspondiente en la sección [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs)
— `AddAutoDiscoveryIAM()` ya configura qué headers confiar y desde qué rango
de red (`172.16.0.0/12`, el que Docker asigna por defecto a sus redes bridge), pero la llamada a
`UseForwardedHeaders()` en sí queda a propósito en tu propio `Program.cs`, porque es una decisión de
topología de red específica de cada despliegue.

### Hardening recomendado para tu propia configuración de nginx

Si usas tu propio stack de nginx (en vez de `nginx-server-docker`) para exponer este contenedor
con TLS, unos puntos verificados en la práctica que vale la pena aplicar en tu `.conf`:

- **`server_tokens off;`** — sin esto, nginx expone su versión exacta en el header `Server` y en las
  páginas de error por default, facilitando que alguien busque CVEs específicos de esa versión.
- **`add_header ... always;`** en cada header de seguridad (`Strict-Transport-Security`,
  `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`) — sin
  `always`, nginx solo los manda en respuestas 2xx/3xx, justo cuando no van en un 4xx/5xx (que es
  cuando más importan, con alguien sondeando el servidor).
- **Lista explícita de cifrados** (perfil "Intermediate" de Mozilla) en vez de alias viejos tipo
  `EECDH`/`EDH`, que ya no distinguen cifrados débiles:
  ````code-block
  ```text
  ssl_ciphers "ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305";
  ```
  ````
  TLSv1.3 no necesita listarse: sus cifrados los negocia OpenSSL aparte y ya son todos fuertes por diseño.
- **Cuidado con `HSTS preload` sobre un certificado wildcard compartido entre varios proyectos**:
  activar `preload` en `Strict-Transport-Security` aplica esa política a **todo** el dominio en los
  navegadores que ya la vieron (Chromium la trae precargada de fábrica), y es casi irreversible —
  sacarlo de la lista de Chromium tarda meses. Si algún otro subdominio del mismo wildcard aún no
  tiene https listo, queda roto. Considera dejar solo `max-age` + `includeSubdomains` hasta que
  todos los proyectos que comparten el certificado estén listos para `preload` a la vez.
- **Rate limiting a nivel de borde** (`limit_req_zone`, declarado fuera de cualquier `server{}`, y
  `limit_req` dentro del `location`) — defensa en profundidad: la app (`RateLimiting`, con el
  limitador nativo de ASP.NET Core, ver [Configuración](/references/configuration-testing/configuration)) ya limita por endpoint,
  pero eso corre *después* de que la petición ya atravesó todo el pipeline de .NET. Una zona en el
  borde frena floods genéricos antes de que le cuesten CPU/DB a la API.
- **Certificado servido = hoja + cadena intermedia concatenados**, nunca solo la hoja — sin la
  cadena intermedia, clientes con validación estricta (curl sin bundle de CAs del sistema, libs
  Java/Android viejas) pueden fallar el handshake, aunque Chrome/Firefox lo disimulen completando la
  cadena solos vía AIA. **Si usas Let's Encrypt** (como permite [`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker)),
  esto ya viene resuelto automáticamente (`fullchain.pem`/`chain.pem` generados solos, sin fusionar
  nada a mano) — lo de abajo solo aplica si ya tienes un certificado descargado de tu VPS/proveedor
  cloud/comprado aparte. Si tu proveedor te entrega la hoja y los intermedios por separado, en
  PowerShell (Windows) puedes fusionarlos así — usa siempre rutas **absolutas** con
  `[System.IO.File]`, porque `Set-Location`/`cd` no siempre actualiza el directorio de trabajo real
  que usan los métodos estáticos de .NET:
  ````code-block
  ```powershell
  $sslDir = "C:\ruta\a\tu\carpeta\ssl\TuDominio"

  # 1. Respaldo de la hoja original antes de tocarla (por si algo sale mal)
  Copy-Item "$sslDir\TuDominio_ssl_certificate.cer" "$sslDir\TuDominio_ssl_certificate.cer.leaf-only.bak"

  # 2. Fusionar los intermedios en un solo archivo de cadena (en orden, sin el root CA)
  [System.IO.File]::WriteAllText("$sslDir\_.TuDominio_ssl_TRUSTED_CHAIN_INTERMEDIATE.cer", (Get-Content "$sslDir\intermediate1.cer" -Raw))
  [System.IO.File]::AppendAllText("$sslDir\_.TuDominio_ssl_TRUSTED_CHAIN_INTERMEDIATE.cer", (Get-Content "$sslDir\intermediate2.cer" -Raw))

  # 3. Pegar la cadena intermedia al final del certificado hoja (fullchain in-place)
  [System.IO.File]::AppendAllText("$sslDir\TuDominio_ssl_certificate.cer", (Get-Content "$sslDir\_.TuDominio_ssl_TRUSTED_CHAIN_INTERMEDIATE.cer" -Raw))
  ```
  ````
  El archivo de cadena intermedia (paso 2) también sirve como valor de `ssl_trusted_certificate`
  (necesario para que `ssl_stapling_verify` pueda validar la respuesta OCSP) — debe apuntar solo a
  la cadena, nunca a la hoja.

Nada de esto es parte de `AddAutoDiscoveryIAM()`/`UseAutoDiscoveryIAM()`: la librería no dicta cómo
configuras tu proxy reverso, esto es guía práctica para quien decida usar nginx como el suyo propio.
