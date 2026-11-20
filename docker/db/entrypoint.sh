#!/bin/bash
set -e

/opt/mssql/bin/sqlservr &
SQLSERVR_PID=$!
trap 'kill -TERM "$SQLSERVR_PID"' SIGTERM SIGINT

# Esperar a que el motor acepte conexiones antes de intentar crear la BD
for i in $(seq 1 50); do
    if sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" &> /dev/null; then
        break
    fi
    sleep 1
done

if [ -n "$MSSQL_DB_NAME" ]; then
    sqlcmd -S localhost -U sa -P "$SA_PASSWORD" \
        -Q "IF DB_ID(N'$MSSQL_DB_NAME') IS NULL CREATE DATABASE [$MSSQL_DB_NAME];"
fi

wait "$SQLSERVR_PID"
