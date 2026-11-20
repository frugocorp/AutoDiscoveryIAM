// Escenario de punta a punta de una app real que consume el paquete (issue #103).
const BASE = process.env.BASE ?? 'http://127.0.0.1:5099';
const MODE = process.argv[2] ?? 'main';
let failures = 0;
const check = (ok, label, extra = '') => {
  console.log(`${ok ? 'OK  ' : 'FAIL'} ${label}${extra ? ` -> ${extra}` : ''}`);
  if (!ok) failures++;
};

async function waitHealth() {
  for (let i = 0; i < 90; i++) {
    try { if ((await fetch(`${BASE}/health`)).ok) return; } catch { /* aún arrancando */ }
    await new Promise((r) => setTimeout(r, 1000));
  }
  throw new Error('la app no respondió /health');
}

async function call(method, path, token, body) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  let json = null;
  try { json = await res.json(); } catch { /* sin cuerpo */ }
  return { status: res.status, json };
}

async function login(email, password) {
  const r = await call('POST', '/auth/login', null, { email, password });
  if (r.status !== 200) throw new Error(`login ${email}: ${r.status} ${JSON.stringify(r.json)}`);
  return r.json.data.token;
}

const findDeep = (node, pred) => {
  if (Array.isArray(node)) { for (const x of node) { const f = findDeep(x, pred); if (f) return f; } return null; }
  if (node && typeof node === 'object') { if (pred(node)) return node; for (const v of Object.values(node)) { const f = findDeep(v, pred); if (f) return f; } }
  return null;
};

await waitHealth();
const dev = await login('dev@example.com', 'AdminPass123!');
const user = await login('user@example.com', 'UserPass123!');

if (MODE === 'collision') {
  const sync = await call('POST', '/permissions/sync', dev, { forceFullSync: false, dryRun: true });
  check(sync.status === 400, 'sync con clave duplicada responde 400', String(sync.status));
  const msg = JSON.stringify(sync.json);
  check(msg.includes('users.create_user'), 'el error nombra la clave', msg.slice(0, 400));
  check(msg.includes('MiApi.Features.Users.Commands.CreateUser') && msg.includes('FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser'), 'el error nombra ambos namespaces');
  process.exit(failures ? 1 : 0);
}

// 1. Antes del sync: el endpoint propio no tiene permiso -> 403 para un usuario normal
const before = await call('POST', '/pedidos', user);
check(before.status === 403, 'antes del sync, POST /pedidos (usuario normal) = 403', String(before.status));

// 2. Sync como IsDevelopment
const sync = await call('POST', '/permissions/sync', dev, { forceFullSync: false, dryRun: false });
check(sync.status === 200, 'POST /permissions/sync (dev) = 200', String(sync.status));
const orch = sync.json?.data?.orchestrationResult ?? {};
console.log(`     descubiertos=${orch.permissionsDiscovered} creados=${orch.permissionsCreated}`);
const without = orch.endpointsWithoutPermission ?? [];
console.log(`     endpointsWithoutPermission=${JSON.stringify(without)}`);
check(without.some((e) => e.includes('/legacy/reporte')), 'reporta /legacy/reporte (sin Features)');
check(without.some((e) => e.includes('/weatherforecast')), 'reporta /weatherforecast (plantilla)');
check(without.some((e) => e.includes('/openapi')), 'reporta /openapi (MapOpenApi sin [PermissionsNotRequired])');

// 3. Los permisos propios existen
const perms = await call('GET', '/permissions?pageSize=100', dev);
const crear = findDeep(perms.json, (o) => o.key === 'pedidos.crear_pedido');
const listar = findDeep(perms.json, (o) => o.key === 'pedidos.get_pedidos');
check(!!crear, 'existe pedidos.crear_pedido');
check(!!listar, 'existe pedidos.get_pedidos');
const libKey = findDeep(perms.json, (o) => o.key === 'users.create_user');
check(!!libKey, 'las claves de la librería siguen (users.create_user)');

// 4. Rol propio con solo pedidos.crear_pedido, asignado al usuario normal
const role = await call('POST', '/roles', dev, { name: `Vendedor-${Date.now()}`, description: 'Rol E2E', isActive: true });
check(role.status === 200 || role.status === 201, 'POST /roles (dev)', String(role.status));
const rolePublicId = role.json?.data?.publicId;
const assignPerm = await call('PUT', `/roles/${rolePublicId}/permissions/${crear?.publicId}`, dev);
check(assignPerm.status === 200, 'asigna pedidos.crear_pedido al rol', `${assignPerm.status} ${assignPerm.status !== 200 ? JSON.stringify(assignPerm.json) : ''}`);
const users = await call('GET', '/users?pageSize=100', dev);
const regular = findDeep(users.json, (o) => o.email === 'user@example.com');
const assignRole = await call('PUT', `/users/${regular?.publicId}/roles/${rolePublicId}`, dev);
check(assignRole.status === 200, 'asigna el rol al usuario normal', `${assignRole.status} ${assignRole.status !== 200 ? JSON.stringify(assignRole.json) : ''}`);

// 5. Autorización real por rol
const userAgain = await login('user@example.com', 'UserPass123!');
const allowed = await call('POST', '/pedidos', userAgain);
check(allowed.status === 200, 'con el permiso, POST /pedidos = 200', String(allowed.status));
const denied = await call('GET', '/pedidos', userAgain);
check(denied.status === 403, 'sin el permiso, GET /pedidos = 403', String(denied.status));
const legacy = await call('GET', '/legacy/reporte', userAgain);
check(legacy.status === 403, 'endpoint sin permiso derivable, GET /legacy/reporte = 403', String(legacy.status));
const legacyDev = await call('GET', '/legacy/reporte', dev);
check(legacyDev.status === 200, 'IsDevelopment pasa igual (bypass de flags)', String(legacyDev.status));
const pdp = await call('GET', '/internal/auth/validate', dev);
check(pdp.status === 404, 'el endpoint PDP ya no existe', String(pdp.status));

console.log(failures ? `\n${failures} verificaciones fallaron` : '\nTodo el escenario pasó');
process.exit(failures ? 1 : 0);
