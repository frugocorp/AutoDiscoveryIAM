# Generico a proposito -- no conoce el nombre del proyecto Angular en particular, ni asume una
# carpeta de salida distinta a la que ya usa el CLI (dist/<PROJECT_NAME>). Pensado para copiarse tal
# cual a cualquier otro proyecto Angular, ajustando solo las variables en .env
# (APP_BUILD_CONTEXT/APP_PROJECT_PATH/APP_PROJECT_NAME, puertos, RENDER_MODE).

# RENDER_MODE elige, en build-time, cual de las 2 etapas "final-*" de abajo se usa como "final" --
# debe coincidir con el "outputMode" real que angular.json del proyecto copiado ya tiene
# (server/static, ver create-ngx-docs-site/scaffold.ts) -- este ARG no CAMBIA ese valor, solo elige
# como se SIRVE el resultado ya generado por "ng build" en la etapa "build" de abajo (identica en
# ambos modos). Declarado en scope GLOBAL (antes del primer FROM) para que cualquier "FROM"
# posterior de este mismo archivo pueda interpolarlo.
ARG RENDER_MODE=server

#############################################################################################

# Etapa build: instala dependencias y genera el build de produccion (browser + server, SSR).
# node:24-alpine para coincidir con la version de Node ya validada en desarrollo (node -v local) --
# revisa por una LTS mas reciente si esta ya quedo vieja.
FROM node:24-alpine AS build

ARG PROJECT_PATH
# WORKDIR ya apunta a la ruta anidada (no a /src a secas) -- necesario para que node_modules quede
# exactamente donde el override de desarrollo (docker-compose.override.yml) lo espera vía su
# working_dir/bind mount, ambos basados en APP_PROJECT_PATH. Mismo motivo que el
# "WORKDIR /src/${PROJECT_PATH}" de la etapa "dev" del Dockerfile de la API.
WORKDIR /src/${PROJECT_PATH}

# "npm run build" (abajo) prerenderiza rutas -- eso ejecuta SSR real, incluido cualquier fetch que
# el propio proceso haga contra si mismo (ej. ContentService leyendo public/content/*.md via HTTP,
# ver ese archivo en el sitio generado). Angular 22 valida el header Host de CADA peticion entrante
# (proteccion SSRF nativa) incluidas esas auto-peticiones -- sin este ENV aca (no solo en la etapa
# "final", que corre DESPUES de este build y nunca re-ejecuta el prerender), esa auto-peticion
# durante el prerenderizado recibe 400 y el HTML prerenderizado queda con el fallback de "no
# encontrado" horneado adentro, sin ningun error visible en el log del build.
ARG APP_ALLOWED_HOSTS
ENV NG_ALLOWED_HOSTS=${APP_ALLOWED_HOSTS}

# Copiamos solo package*.json primero para aprovechar el cache de capas de Docker en "npm ci"
# aunque cambie el resto del codigo fuente -- misma razon que copiar solo el .csproj primero en el
# Dockerfile de la API.
COPY ${PROJECT_PATH}/package*.json ./

# El package.json del proyecto puede depender de paquetes propios que aun no estan publicados en
# el registro real de npm via "file:./vendor/<paquete>/<tarball>.tgz" (equivalente en npm de un
# feed local de NuGet). "npmfeed" es un build context adicional (ver docker-compose.yml/.override.yml,
# build.additional_contexts) que monta esa carpeta aqui -- npm ci la resuelve como cualquier otra
# ruta relativa dentro del WORKDIR. Si tu proyecto no usa ningun paquete "file:" local, deja
# APP_NPM_FEED_PATH vacio/sin definir en tu .env: este COPY no copia nada y "npm ci" simplemente
# resuelve todo contra el registro real, sin verse afectado.
COPY --from=npmfeed . ./vendor
RUN npm ci

# Ahora copiamos el resto del proyecto
COPY ${PROJECT_PATH}/. .

# "ng build" (via "npm run build") respeta lo que diga angular.json del proyecto copiado --
# "outputMode":"server" genera dist/<PROJECT_NAME>/browser + dist/<PROJECT_NAME>/server (bundle
# SSR real); "outputMode":"static" genera SOLO dist/<PROJECT_NAME>/browser (100% prerenderizado,
# sin bundle de servidor). Esta etapa es identica en ambos casos -- no necesita saber cual es,
# "RENDER_MODE" (ver ARG global) recien se usa mas abajo, para elegir COMO se sirve lo que sea que
# se haya generado aca.
RUN npm run build


#############################################################################################

# Etapa final para RENDER_MODE=server: solo Node + el build ya generado (con su carpeta
# dist/<PROJECT_NAME>/server/ real) -- sin devDependencies ni Angular CLI (imagen mucho mas chica
# que la etapa build).
FROM node:24-alpine AS final-server

ARG PROJECT_NAME
ARG PROJECT_PATH
WORKDIR /app

COPY --from=build /src/${PROJECT_PATH}/dist/${PROJECT_NAME} ./dist/${PROJECT_NAME}

# El propio server.ts que genera "ng new --ssr" ya lee PORT de las variables de entorno (con 4000
# como default si no se define, ver src/server.ts del proyecto) -- no hace falta ningun flag
# extra, solo esta variable, igual que ASPNETCORE_URLS en el Dockerfile de la API.
ARG APP_PORT_CONTAINER
ENV PORT=${APP_PORT_CONTAINER}
ENV NODE_ENV=production

# Angular 22 valida el header Host (proteccion SSRF nativa, ver angular.dev/best-practices/security)
# y responde 400 "Header \"host\" ... is not allowed" para cualquier host no listado aqui --
# incluido el que reenvia nginx como proxy reverso. NG_ALLOWED_HOSTS (variable de entorno leida
# automaticamente por AngularNodeAppEngine, sin tocar server.ts) trae la lista separada por comas
# desde APP_ALLOWED_HOSTS (.env) -- generico, no hardcodeado a ningun dominio en particular.
ARG APP_ALLOWED_HOSTS
ENV NG_ALLOWED_HOSTS=${APP_ALLOWED_HOSTS}
EXPOSE ${APP_PORT_CONTAINER}

# Se conserva como ENV (no solo ARG) porque el ENTRYPOINT de abajo la necesita en tiempo de
# ejecucion -- misma razon que PROJECT_NAME/API_PORT_CONTAINER en el Dockerfile de la API.
ENV PROJECT_NAME=${PROJECT_NAME}
ENTRYPOINT ["sh", "-c", "exec node \"dist/$PROJECT_NAME/server/server.mjs\""]


#############################################################################################

# Etapa final para RENDER_MODE=static: SIN Node.js en runtime -- nginx sirve
# dist/<PROJECT_NAME>/browser directamente (ya 100% archivos estaticos, un index.html real por
# ruta gracias al prerender completo -- ver app.routes.server.ts del proyecto). Con
# outputMode:"static" en angular.json, dist/<PROJECT_NAME>/server/ ni siquiera se genera -- solo
# copiamos browser/. Imagen mucho mas liviana en RAM que final-server (sin el motor SSR de Angular
# cargado en memoria para nunca usarlo -- todas las rutas ya estan prerenderizadas).
FROM nginx:1.29-alpine AS final-static

ARG PROJECT_NAME
ARG PROJECT_PATH
COPY --from=build /src/${PROJECT_PATH}/dist/${PROJECT_NAME}/browser /usr/share/nginx/html

# Mismo mecanismo que nginx-server-docker (repo aparte) -- la imagen oficial nginx:*-alpine corre
# "envsubst" sobre cualquier *.template de /etc/nginx/templates/ al arrancar, usando las variables
# de entorno declaradas (NGINX_PORT_CONTAINER, ver abajo), y escribe el resultado en
# /etc/nginx/conf.d/. "default.conf.template" reemplaza al default.conf que trae la imagen (sin
# esto quedarian los 2 compitiendo). Ruta parametrizada (no fija) por el mismo motivo que
# APP_DEV_ENTRYPOINT_PATH mas abajo -- si esta carpeta se copia a otro sitio que la anida distinto,
# la ruta real cambia aunque el Dockerfile sea identico.
ARG APP_NGINX_STATIC_CONF_PATH
COPY ${APP_NGINX_STATIC_CONF_PATH} /etc/nginx/templates/default.conf.template

ARG APP_PORT_CONTAINER
ENV NGINX_PORT_CONTAINER=${APP_PORT_CONTAINER}
EXPOSE ${APP_PORT_CONTAINER}


#############################################################################################

# Selector final -- resuelto 100% en build-time por Docker/BuildKit segun RENDER_MODE (ver ARG
# global al principio de este archivo). La imagen resultante contiene UNICAMENTE la rama elegida
# (la otra ni siquiera queda en el historial de capas de la imagen final).
FROM final-${RENDER_MODE} AS final


# =========================
# Etapa para desarrollo interactivo (equivalente a "dotnet watch" pero con "ng serve")
# =========================
FROM node:24-alpine AS dev

ARG PROJECT_PATH
# Igual que la etapa "build": WORKDIR anidado en vez de "/src" a secas, para que node_modules quede
# exactamente donde docker-compose.override.yml lo espera (su working_dir/bind mount usan la misma
# APP_PROJECT_PATH) -- sin esto, "ng serve" arranca pero resuelve node_modules desde una ruta
# distinta a la que el volumen del override aisla, y falla en tiempo de ejecucion.
WORKDIR /src/${PROJECT_PATH}

COPY ${PROJECT_PATH}/package*.json ./

# Mismo motivo/mecanismo que en la etapa "build" de arriba -- ver ese comentario para el detalle
# completo de "npmfeed"/APP_NPM_FEED_PATH.
COPY --from=npmfeed . ./vendor
RUN npm ci

# Ahora copiamos el resto -- en desarrollo esta carpeta se sobreescribe con el volumen del
# override (codigo fuente real del host), esto solo cubre el primer arranque antes de montar.
COPY ${PROJECT_PATH}/. .

# Se conserva como ENV (no solo ARG) porque el ENTRYPOINT de abajo la necesita en tiempo de
# ejecucion -- un ARG no persiste mas alla del build, y "$APP_PORT_CONTAINER" ahi abajo se
# evalua ya con el contenedor corriendo. Mismo motivo que PROJECT_NAME en la etapa "final".
ARG APP_PORT_CONTAINER
ENV APP_PORT_CONTAINER=${APP_PORT_CONTAINER}
EXPOSE ${APP_PORT_CONTAINER}

# Mismo motivo que en la etapa "final" -- "ng serve" en modo SSR corre el mismo motor de
# renderizado y valida Host igual.
ARG APP_ALLOWED_HOSTS
ENV NG_ALLOWED_HOSTS=${APP_ALLOWED_HOSTS}
ENV APP_ALLOWED_HOSTS=${APP_ALLOWED_HOSTS}

# "ng serve" (Vite) valida Host con su propio mecanismo, distinto de NG_ALLOWED_HOSTS -- ver
# dev-entrypoint.sh para el detalle completo de por que hace falta este script en vez de un
# ENTRYPOINT de una sola linea como en la etapa "final".
#
# Ruta parametrizada (no "docker/app/web/dev-entrypoint.sh" fijo) -- ver APP_DEV_ENTRYPOINT_PATH en
# .env: si esta misma carpeta genérica se copia a un proyecto que la anida un nivel mas profundo
# (ej. "docker/app/web/<sitio>/" en vez de "docker/app/web/" a secas), la ruta real del script
# cambia aunque el Dockerfile sea identico -- cada copia trae su propio dev-entrypoint.sh (sin
# compartir uno solo entre proyectos, misma atomicidad que el resto de esta carpeta).
ARG APP_DEV_ENTRYPOINT_PATH
COPY ${APP_DEV_ENTRYPOINT_PATH} /dev-entrypoint.sh
RUN chmod +x /dev-entrypoint.sh
ENTRYPOINT ["/dev-entrypoint.sh"]
