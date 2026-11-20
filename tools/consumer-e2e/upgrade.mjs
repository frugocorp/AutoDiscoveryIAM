// Prueba de actualización: fase "before" con la versión vieja, fase "after" con la nueva.
import { readFileSync, writeFileSync } from 'node:fs';
const BASE = 'http://127.0.0.1:5099';
const STATE = new URL('./upgrade-state.json', import.meta.url);
const phase = process.argv[2];
let failures = 0;
const check = (ok, label, extra = '') => { console.log(`${ok ? 'OK  ' : 'FAIL'} ${label}${extra ? ` -> ${extra}` : ''}`); if (!ok) failures++; };
async function waitHealth() { for (let i = 0; i < 90; i++) { try { if ((await fetch(`${BASE}/health`)).ok) return; } catch {} await new Promise((r) => setTimeout(r, 1000)); } throw new Error('sin /health'); }
async function call(method, path, token, body) {
  const res = await fetch(`${BASE}${path}`, { method, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body === undefined ? undefined : JSON.stringify(body) });
  let json = null; try { json = await res.json(); } catch {} return { status: res.status, json };
}
const login = async (email, password) => (await call('POST', '/auth/login', null, { email, password })).json.data.token;
const findDeep = (n, p) => { if (Array.isArray(n)) { for (const x of n) { const f = findDeep(x, p); if (f) return f; } return null; } if (n && typeof n === 'object') { if (p(n)) return n; for (const v of Object.values(n)) { const f = findDeep(v, p); if (f) return f; } } return null; };

await waitHealth();
const dev = await login('dev@example.com', 'AdminPass123!');
if (phase === 'before') {
  const sync = await call('POST', '/permissions/sync', dev, { forceFullSync: false, dryRun: false });
  check(sync.status === 200, '[vieja] sync', String(sync.status));
  const perms = await call('GET', '/permissions?pageSize=100', dev);
  const crear = findDeep(perms.json, (o) => o.key === 'pedidos.crear_pedido');
  const role = await call('POST', '/roles', dev, { name: 'Vendedor', description: 'Rol creado con la versión vieja', isActive: true });
  const rolePublicId = role.json?.data?.publicId;
  await call('PUT', `/roles/${rolePublicId}/permissions/${crear.publicId}`, dev);
  const users = await call('GET', '/users?pageSize=100', dev);
  const regular = findDeep(users.json, (o) => o.email === 'user@example.com');
  const assign = await call('PUT', `/users/${regular.publicId}/roles/${rolePublicId}`, dev);
  check(assign.status === 200, '[vieja] rol Vendedor asignado al usuario normal', String(assign.status));
  const user = await login('user@example.com', 'UserPass123!');
  check((await call('POST', '/pedidos', user)).status === 200, '[vieja] POST /pedidos = 200');
  const borrador = (await call('POST', '/pedidos/borrador', user)).status;
  console.log(`     [vieja] POST /pedidos/borrador (segunda ruta del slice) = ${borrador} (403 esperado: bug corregido en la nueva)`);
  writeFileSync(STATE, JSON.stringify({ rolePublicId }));
} else {
  const { rolePublicId } = JSON.parse(readFileSync(STATE, 'utf8'));
  const user = await login('user@example.com', 'UserPass123!');
  check((await call('POST', '/pedidos', user)).status === 200, '[nueva] los datos sobrevivieron: POST /pedidos = 200 sin tocar nada');
  check((await call('POST', '/pedidos/borrador', user)).status === 200, '[nueva] segunda ruta del slice = 200');
  check((await call('GET', '/pedidos', user)).status === 403, '[nueva] sin permiso sigue siendo 403');
  const role = await call('GET', `/roles/${rolePublicId}`, dev);
  check(role.status === 200 && role.json?.data?.name === 'Vendedor', '[nueva] el rol creado con la vieja sigue igual');
  const roles = await call('GET', '/roles?pageSize=100', dev);
  const admin = findDeep(roles.json, (o) => o.name === 'AdminAutoDiscoveryIAM');
  const adminPerms = await call('GET', `/roles/${admin.publicId}/permissions?pageSize=100`, dev);
  const has = (k) => !!findDeep(adminPerms.json, (o) => o.key === k);
  check(has('users.get_users') && has('roles.get_roles') && has('users.activation'), '[nueva] el seed agregó los permisos nuevos al rol administrador');
  const sync = await call('POST', '/permissions/sync', dev, { forceFullSync: false, dryRun: true });
  check(sync.status === 200, '[nueva] sync sigue funcionando', String(sync.status));
}
console.log(failures ? `\n${failures} verificaciones fallaron` : '\nFase completa sin fallas');
process.exit(failures ? 1 : 0);
