---
[slug]: third-party-data
[title]: Privacidad y datos de terceros
[description]: Qué datos personales recibe y almacena la librería con proveedores de identidad externos y qué controles aplica.
[keywords]: privacidad, datos personales, login social, Google OAuth
---

# Privacidad y datos de terceros

Este documento describe qué datos personales recibe, procesa y almacena `FrugoCorp.AspNetCore.AutoDiscoveryIAM` cuando un usuario inicia sesión con un proveedor de identidad externo (actualmente Google), y qué controles de seguridad se aplican sobre esos datos.

Es documentación técnica, no un aviso de privacidad legal. Antes de operar con usuarios reales, el equipo responsable de cada despliegue debe adaptar este contenido a su propio aviso de privacidad público, con asesoría legal en su jurisdicción — la sección [Responsabilidad de quien opera una instancia](/docs/security-rbac/third-party-data#responsabilidad-de-quien-opera-una-instancia) detalla qué implica esto en la práctica.

## Alcance del login social

El flujo de OAuth con Google solicita únicamente el scope `openid email`, con `access_type=online`. En términos concretos:

- **No se solicita ni se recibe ningún token de larga duración de Google.** No hay `access_token` ni `refresh_token` persistente — con `access_type=online`, Google ni siquiera los emite.
- El único artefacto que Google entrega es un `id_token` firmado, validado una sola vez (emisor, audiencia, expiración) y descartado de inmediato. Nunca se persiste.
- No se solicita acceso a Gmail, Drive, Contactos, nombre, foto de perfil ni ningún otro dato — solo la identidad básica (email) que Google ya verifica por su cuenta. El scope `profile` no se pide a propósito: la librería no persiste nombre ni ningún otro dato de perfil, así que pedirlo sería recolectar datos que nunca se usan.

## Datos almacenados

De la respuesta de Google, la librería persiste únicamente lo siguiente:

| Dato | Dónde se guarda | Detalle |
|---|---|---|
| Email | `Users.Email` | El mismo campo que ya usa cualquier cuenta registrada de forma tradicional (es el username del sistema). |
| Proveedor | `ExternalLogins.Provider` | Valor fijo (`"google"`) — una categoría, no un dato sensible. |
| Identificador de sujeto (`sub`) | `ExternalLogins.ProviderId` | Hasheado con SHA-256, nunca en claro. |

No se almacena nombre, foto de perfil ni ningún otro dato de Google más allá de lo anterior.

### Por qué el identificador de sujeto está hasheado

El `sub` que devuelve Google es estable pero no es una credencial: conocerlo por sí solo no permite iniciar sesión como esa persona, porque cada login real exige una autenticación vigente con Google que emite un `id_token` firmado, verificado por la librería en el momento.

Aun así, se guarda hasheado (SHA-256, determinista para permitir el match exacto en el login) para no persistir en claro un identificador que podría correlacionarse con otros sistemas que también hayan registrado el mismo `sub` de Google. El hash se calcula una vez por login/vinculación, sin impacto en el rendimiento del resto de la API.

### Por qué no existe un campo de email de proveedor por separado

`ExternalLogins` no tiene un campo `ProviderEmail` independiente — el email de Google se guarda únicamente en `Users.Email`. Esto evita una copia redundante de un dato personal en la base de datos: menos superficie de exposición ante una filtración, sin ninguna pérdida funcional.

## Evaluación de riesgo ante una filtración de base de datos

Si la base de datos se ve comprometida, lo que un atacante obtendría de un usuario vinculado a Google es:

- Su email — el mismo riesgo que ya existe para cualquier usuario registrado de forma tradicional (phishing dirigido, spam).
- El hecho de que esa cuenta usa Google para iniciar sesión — una fuga de metadatos menor, no de una credencial.
- **Ninguna posibilidad de acceder a la cuenta real de Google del usuario.** No hay tokens de Google que robar, y el identificador hasheado no sirve para autenticarse ante Google ni ante esta API sin pasar de nuevo por el flujo real de OAuth.

Vincular una cuenta de Google no introduce una categoría de riesgo adicional más allá de la que ya existe por operar una base de usuarios con email.

## Responsabilidad de quien opera una instancia

Este documento describe el comportamiento técnico del código tal como está en este repositorio. Antes de desplegar esta librería (o un fork) en producción con usuarios reales, el equipo operador debe:

1. **Publicar su propio aviso de privacidad**, adaptado a su jurisdicción (por ejemplo GDPR en la Unión Europea, CCPA en California, o la LFPDPPP en México) y a los datos reales que su aplicación agregue sobre esta base.
2. **Revisar este documento de nuevo si se agregan más scopes** de Google (o de otro proveedor de identidad) o más campos de perfil — cada scope adicional cambia el análisis de riesgo descrito arriba.
3. **Obtener asesoría legal** antes de operar con usuarios reales, especialmente si se maneja información de menores, datos de salud, financieros u otras categorías sensibles.

La licencia [MIT](/license) de este proyecto cubre el *código* (sin garantía, uso bajo el propio riesgo del operador) y no exime a quien opera una instancia real de sus obligaciones legales sobre los datos de sus propios usuarios.
