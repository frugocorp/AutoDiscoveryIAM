---
[slug]: docker-db
[title]: Docker: db/
[description]: Cómo levantar una base de datos SQL Server de desarrollo en un contenedor.
[keywords]: Docker, SQL Server, base de datos, desarrollo
[readme_flavors]: github, npmjs
---

# Docker — db/

## Qué es

Un contenedor de SQL Server (`mcr.microsoft.com/mssql/server:2025-latest`, con `sqlcmd`/`bcp` instalados) pensado **únicamente para desarrollo local** — para quien no tiene ya una instancia propia de SQL Server a la mano y no quiere instalarla directamente en su máquina. **No está pensado para producción**: no trae backups, réplicas, ni ningún endurecimiento de seguridad más allá de lo que ya trae la imagen oficial.

---

## Levantarlo

Entra a la carpeta:

````code-block
```bash
cd docker/db
```
````

Copia la plantilla de variables de entorno (completa los valores antes de levantarlo):

````code-block
```bash
cp .env.template .env
```
````

Construye y levanta el contenedor en segundo plano:

````code-block
```bash
docker-compose up --build -d
```
````

---

## Variables de `.env`

| Variable                  | Para qué sirve                                                                         |
| -------------------------- | --------------------------------------------------------------------------------------- |
| `MSSQL_DB_NAME`             | Nombre de la base de datos a crear.                                                     |
| `MSSQL_SA_PASSWORD`         | Contraseña del usuario `sa`. Debe cumplir la política de contraseñas de SQL Server (mínimo 8 caracteres, mayúscula, minúscula, número y/o símbolo) o el contenedor falla al arrancar. |
| `MSSQL_ACCEPT_EULA`         | Debe ser `Y` — sin esto la imagen oficial de Microsoft no arranca.                       |
| `MSSQL_PID`                 | Edición de SQL Server (ej. `Developer`) — `Developer` es gratuita y suficiente para desarrollo local. |
| `MSSQL_TCP_PORT_HOST`       | Puerto expuesto en tu máquina (ej. `1433`).                                             |
| `MSSQL_TCP_PORT_CONTAINER`  | Puerto interno del contenedor (normalmente el mismo `1433`).                             |
| `MSSQL_VOLUME_PERSIST_DB`   | Ruta/volumen donde persisten los datos entre reinicios del contenedor.                   |

---

## Conectar la API a este contenedor

Apunta `ConnectionStrings:DefaultConnection` (en tu `appsettings.json`, o `ConnectionStrings__DefaultConnection` como variable de entorno) a `localhost` y el puerto que hayas puesto en `MSSQL_TCP_PORT_HOST`:

````code-block
```text
Server=localhost,1433;Initial Catalog={{MSSQL_DB_NAME}};User ID=sa;Password={{MSSQL_SA_PASSWORD}};TrustServerCertificate=True;
```
````

Si en vez de correr la API directo en tu máquina la corres también en Docker (ver [Docker: api/](/tutorials/docker/docker-api)), ambos contenedores necesitan estar en la misma red de Docker para que el nombre del servicio (`adiam-mssql`) resuelva entre ellos — hoy cada `docker-compose` de esta carpeta es independiente y no comparte red con `api/` automáticamente; conéctalos a la misma red externa si necesitas ambos contenedores hablándose entre sí.
