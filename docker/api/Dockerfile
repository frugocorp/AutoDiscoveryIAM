# Con esta configuración crearemos una imagen que posteriormente montaremos en un contenedor.
# Generico a proposito -- no conoce el nombre del proyecto ni ningun paquete NuGet especifico, ni
# asume una carpeta destino en particular. Pensado para copiarse tal cual a cualquier proyecto
# .NET, ajustando solo las variables en .env (API_PROJECT_PATH/API_PROJECT_NAME, feed local de
# NuGet si aplica, puertos).

# FROM se utiliza para identificar etapas de compilacion dentro del Dockerfile y se puede le poner un alias
# Esto se implemento para el uso de varias etapas
# Cada etapa tiene sus archivos, los cuales pueden ser compartidos entre ellas

#############################################################################################

# Etapa build (Construccion) donde copiaremos los archivos del host a la imagen para
# porder trabajar con ellos dentro de la imagen.
# sdk:10.0 - Construye - kit completo para desarrollo de software
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Usaremos el método Release (Lanzamiento) ya que optimiza el rendimiento
# En caso de tener problemas y querer depurar el código, usar Debug (Depurar)
ARG BUILD_CONFIGURATION=Release

# Ruta (relativa a la raiz del repo) y nombre base del proyecto .NET a construir -- vienen de
# docker-compose.yml (build.args), que a su vez los toma de .env (API_PROJECT_PATH/API_PROJECT_NAME).
# El nombre base se reutiliza para el .csproj y para la .dll publicada (mismo nombre por convencion
# de .NET: un MiProyecto.csproj produce MiProyecto.dll salvo que se sobreescriba AssemblyName).
ARG PROJECT_PATH
ARG PROJECT_NAME

# Seleccionamos el Directorio de trabajo para esta etapa
WORKDIR /src

# Copiamos todos los archivos y carpetas del repo hacia nuestro directorio de trabajo.
COPY . .

# El NuGet.config del proyecto puede apuntar un feed local a una ruta del host (para paquetes
# propios que aun no estan publicados en un feed remoto), que no existe dentro del contenedor.
# "nugetfeed" es un build context adicional (ver docker-compose.yml/.override.yml,
# build.additional_contexts) que monta esa carpeta aqui -- --source la usa en vez de NuGet.config
# para esta restauracion puntual. Si tu proyecto no usa ningun feed local, deja
# NUGET_LOCAL_FEED_PATH vacio en tu .env: este COPY no copia nada y el --source extra
# simplemente no encuentra paquetes ahi, sin afectar la restauracion contra tus fuentes remotas.
COPY --from=nugetfeed . /nuget-feed
RUN dotnet restore "./${PROJECT_PATH}/${PROJECT_NAME}.csproj" --source /nuget-feed --source https://api.nuget.org/v3/index.json

# Seleccionamos el Directorio de trabajo en donde se encuenta el archivo de proyecto
WORKDIR "/src/${PROJECT_PATH}"

# Restauramos todas las dependencias del archivo de proyecto
RUN dotnet restore --source /nuget-feed --source https://api.nuget.org/v3/index.json

# Dentro de la imágen compilamos el proyecto y lo guardamos en /app/build. --no-restore: ya se
# restauro arriba con las fuentes correctas -- sin esto, dotnet build intenta una restauracion
# implicita usando NuGet.config (que puede tener rutas invalidas dentro del contenedor) y falla igual.
# Esto se utiliza para transformar el codigo fuente en codigo legible por maquina.
# Este proceso implica la búsqueda de errores, advertencias y problemas de sintaxis,
# y la creación de archivos ejecutables con sus dependencias necesarias.
RUN dotnet build "./${PROJECT_NAME}.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore


#############################################################################################

# Ahora tomamos la etapa build para tener acceso a los archivos que se generaron en ella,
# pero en esta etapa solo publicaremos nuestro proyecto para empaquetar nuestra aplicacion,
# creamos una nueva etapa ya que solo ocuparemos los archivos publicados, no necesitamos los
# demas archivos de la etapa build
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG PROJECT_NAME
RUN dotnet publish "./${PROJECT_NAME}.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore


#############################################################################################

# En esta etapa reconstruimos la imagen pero con el aspnet solo para ejecutar, el cual es mucho mas ligero que el sdk, la etapa la llamaremos base
# aspnet:10.0 - Ejecuta - Motor de ejecucion que controla las aplicaciones en ejecucion
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# curl se instala como root (antes de USER app) para poder usarlo en el HEALTHCHECK de la etapa
# final, contra el endpoint /health.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*
# Cambia el usuario efectivo del resto de la imagen (COPY/RUN posteriores, y el proceso final en
# tiempo de ejecucion) de root a "app" -- un usuario sin privilegios que la propia imagen base
# mcr.microsoft.com/dotnet/aspnet ya trae creado desde .NET 8. Principio de menor privilegio: si
# alguien explota una vulnerabilidad en la app corriendo adentro, queda limitado a los permisos de
# "app" (no puede modificar archivos del sistema, no puede abrir puertos <1024, etc.), no a los de
# root. Por eso "curl" se instala ANTES de este USER (instalar paquetes requiere root).
USER app
# Carpeta del anillo de claves de ASP.NET Core Data Protection (cifra los secretos de Authenticator),
# creada ya con dueño "app": docker-compose.yml monta ahí un volumen con nombre, que Docker inicializa
# con el dueño de esta carpeta. Sin persistirla, cada reconstrucción del contenedor genera claves
# nuevas y los usuarios con Authenticator ya no pueden completar el segundo factor.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys
# Seleccionamos el Directorio de trabajo para esta imagen
WORKDIR /app
# Usamos el puerto para http, ya que el https sera gestionado por medio de un proxy reverso (nginx u otro)
ARG API_PORT_CONTAINER
ENV ASPNETCORE_URLS=http://+:${API_PORT_CONTAINER}
# Se conserva tambien como ENV (no solo ARG) porque el HEALTHCHECK de la etapa final la necesita
# en tiempo de ejecucion — los ARG no persisten entre etapas de un multi-stage build.
ENV API_PORT_CONTAINER=${API_PORT_CONTAINER}
EXPOSE ${API_PORT_CONTAINER}


#############################################################################################

# Ahora que ya tenemos la imagen con el aspnet, publicamos nuestro proyecto para empaquetar nuestra aplicacion
# para esto tomamos lo que publicamos en la etapa publish y lo enviamos a la carpeta publish pero de la etapa base
# Esta etapa se llamara final
FROM base AS final
ARG PROJECT_NAME
# El nombre de la .dll publicada es el AssemblyName del proyecto, que NO siempre coincide con el
# nombre del .csproj (ej. este mismo proyecto: "AutoDiscoveryIAM.SampleHost.csproj" pero
# AssemblyName="FrugoCorp.AspNetCore.AutoDiscoveryIAM.SampleHost" -- descubierto adaptando este
# Dockerfile a un segundo proyecto real). Default = PROJECT_NAME (el caso comun, cuando no hay
# override de AssemblyName) -- pasa ASSEMBLY_NAME como build arg solo si tu .csproj lo sobreescribe.
ARG ASSEMBLY_NAME=${PROJECT_NAME}
# Se conservan como ENV (no solo ARG) porque el ENTRYPOINT de abajo las necesita en tiempo de
# ejecucion -- misma razon que API_PORT_CONTAINER arriba.
ENV PROJECT_NAME=${PROJECT_NAME}
ENV ASSEMBLY_NAME=${ASSEMBLY_NAME}
WORKDIR /app
COPY --from=publish /app/publish .
# Healthcheck contra /health: Docker (o un orquestador como Kubernetes/Swarm) revisa este endpoint
# periodicamente para saber si el contenedor de verdad puede atender peticiones, no solo si el
# proceso sigue vivo. Ejemplo generico: si la app no logra conectar a su base de datos al arrancar,
# el proceso de dotnet puede seguir corriendo (Docker lo reportaria "healthy" con solo ver que el
# PID existe) sin poder servir nada util -- el healthcheck expone esa diferencia. Requiere que el
# proyecto tenga un endpoint /health real que valide sus dependencias (ej. via
# Microsoft.Extensions.Diagnostics.HealthChecks + app.MapHealthChecks("/health")) -- si tu proyecto
# no lo tiene todavia, este HEALTHCHECK siempre falla; bórralo o ajusta la ruta mientras tanto.
HEALTHCHECK --interval=60s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -f http://localhost:$API_PORT_CONTAINER/health || exit 1
# Punto de partida del contenedor: la .dll publicada, ejecutada con dotnet. Forma "sh -c exec ..."
# (no el exec-form directo ["dotnet", "X.dll"]) porque el exec-form no expande variables de
# entorno -- se necesita sustituir $PROJECT_NAME en tiempo de ejecucion. "exec" reemplaza el
# proceso de sh por el de dotnet, para que reciba señales (SIGTERM, etc.) directamente en vez de
# quedar detras de un shell intermedio.
ENTRYPOINT ["sh", "-c", "exec dotnet \"$ASSEMBLY_NAME.dll\""]


# =========================
# Etapa para desarrollo interactivo con dotnet watch
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
ARG PROJECT_PATH
ARG PROJECT_NAME
WORKDIR /src

# Copiamos solo el archivo de proyecto primero, para aprovechar el cache de capas de Docker en
# dotnet restore aunque cambie el resto del codigo fuente.
COPY ["${PROJECT_PATH}/${PROJECT_NAME}.csproj", "${PROJECT_PATH}/"]
# Mismo motivo que en la etapa build de arriba: feed local opcional para paquetes que no vienen de
# un feed remoto.
COPY --from=nugetfeed . /nuget-feed
RUN dotnet restore "${PROJECT_PATH}/${PROJECT_NAME}.csproj" --source /nuget-feed --source https://api.nuget.org/v3/index.json

# Ahora copiamos el resto
COPY . .

# Configuración de puertos
ARG API_PORT_CONTAINER
ENV ASPNETCORE_URLS=http://+:${API_PORT_CONTAINER}
EXPOSE ${API_PORT_CONTAINER}

# Seleccionamos el Directorio de trabajo en donde se encuenta el archivo de proyecto
WORKDIR "/src/${PROJECT_PATH}"

# --no-launch-profile: ignora el launchSettings.json del PROYECTO .NET (Properties/launchSettings.json,
# el que usa "dotnet run"/Visual Studio para F5 normal -- no tiene relacion con ningun
# launchSettings.json a nivel Docker Compose) por completo, ya que ASPNETCORE_URLS viene fijado
# arriba vía ENV. Se ignora en vez de borrar el archivo porque el volumen del override de
# desarrollo monta el directorio del proyecto directamente desde el host, así que borrarlo en la
# imagen podía terminar afectando también el archivo real del working tree.
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
