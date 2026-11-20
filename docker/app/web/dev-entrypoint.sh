#!/bin/sh
set -e

# "ng serve" (dev-server de Vite) valida el header Host con su propio mecanismo, separado del
# NG_ALLOWED_HOSTS que usa el motor SSR/produccion (server.mjs). Verificado contra "ng serve
# --help" en esta misma imagen: "--allowed-hosts" en el CLI solo acepta booleano ("permitir
# cualquier host"), NO una lista de hosts especificos -- eso solo se puede configurar dentro de
# angular.json (serve.options.allowedHosts como array), no via flag. Repetir el flag con distintos
# valores (uno por host de APP_ALLOWED_HOSTS) truena con "Unknown argument", confirmado en la
# practica.
#
# Se acepta el booleano (todos los hosts) SOLO aqui, en desarrollo -- no es una relajacion de
# seguridad real: este servidor nunca corre en produccion (la etapa "final" usa server.mjs +
# NG_ALLOWED_HOSTS, que si valida hosts especificos de verdad), y en desarrollo ya esta detras de
# la misma red interna de Docker + nginx.
#
# --host 0.0.0.0: "ng serve" solo escucha en localhost por default -- sin esto, el servidor de
# desarrollo arranca pero no es alcanzable desde fuera del contenedor.
#
# --poll 1000: el código fuente llega por bind mount desde el host (docker-compose.override.yml)
# -- los eventos nativos de sistema de archivos (inotify) no se propagan de forma confiable a
# través de un bind mount de Windows (Docker Desktop -> WSL2 -> contenedor Linux), así que sin
# esto "ng serve" nunca detecta los cambios y hay que bajar/levantar el contenedor para verlos.
# Con --poll, Angular revisa los archivos cada 1000ms en vez de esperar un evento del SO -- mismo
# problema y mismo tipo de solución que DOTNET_USE_POLLING_FILE_WATCHER en "dotnet watch"
# (docker/api). En Linux nativo (bind mount sin la capa de Windows) esto no hace falta, pero
# tampoco cuesta nada dejarlo siempre activo.
exec npm start -- --host 0.0.0.0 --port "$APP_PORT_CONTAINER" --allowed-hosts --poll 1000
