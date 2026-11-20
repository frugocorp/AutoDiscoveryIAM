# 📅 Milestone `[repo-milestone]`

📍 **Dónde vive:** el repositorio → *Issues* → *Milestones*. Se asigna en el issue, en el campo **Milestone** de la barra lateral.

Un milestone agrupa los issues de un mismo frente de trabajo (por ejemplo, la seguridad de RBAC o el sitio de documentación). Cada milestone tiene su carpeta local en `local/project/milestones/[repo-milestone]/issues/`, donde viven los archivos de issue, plan y PR.

---

## Reglas

- **Nombre:** kebab-case terminado en `-milestone` (por ejemplo `cross-cutting-issues-milestone`). El nombre en GitHub y el de la carpeta local son idénticos.
- **Solo milestones que ya existen.** El valor de `[repo-milestone]` tiene que ser uno de los milestones del repositorio (los ves en *Issues* → *Milestones*, o con `gh api repos/{owner}/{repo}/milestones`).
- **Un milestone nuevo lo decide el responsable del repo, no la IA.** Si ningún milestone existente encaja, se propone uno nuevo y se espera la aprobación antes de usarlo. Al aprobarse, se crea en GitHub y su carpeta local con el mismo nombre.
