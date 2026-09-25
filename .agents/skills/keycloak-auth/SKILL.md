---
name: keycloak-auth
description: Provision local workshop Keycloak from bundled Docker Compose and realm-import files, and explain the latest MCP authorization protocols, supporting OIDC/OAuth contract, and interoperability gotchas. Use for Keycloak-based web-app and MCP training.
---

# Keycloak Auth

Supply reproducible Keycloak infrastructure and explain the authorization protocols and interoperability requirements for an Angular SPA, C# backend, and C# MCP server workshop.

The bundled assets provide a complete starting point for an empty repository.

## Provision Keycloak

Resolve these paths relative to **this SKILL.md**, wherever the skill is installed:

- [assets/compose.yaml](assets/compose.yaml) → `compose.yaml` in the target project.
- [assets/workshop-realm.json](assets/workshop-realm.json) → `auth/workshop-realm.json` in the target project; create `auth/` if needed.

Inspect existing files before copying. Merge into existing Compose/realm configuration when necessary, preserving unrelated work. Edit the generated copies, not installed assets. The Compose bind mount depends on those relative output paths. The template omits a fixed project name so separate repositories do not share the original workshop's containers or volume; check port availability before startup.

Run from the target project:

```sh
docker compose config --quiet
docker compose up -d --wait
```

Plain `docker compose up` also starts Keycloak and imports the realm automatically. The configuration uses Keycloak **26.7.4** with experimental `cimd` and `resource-indicators` features, local HTTP on loopback, demo passwords, and embedded H2 storage. Strict protocol conformance requires HTTPS for authorization-server endpoints and protected-resource identifiers.

| Contract | Default |
| --- | --- |
| Admin Console / bootstrap credentials | `http://localhost:8080/admin/` / `admin` + `admin` |
| Realm / demo credentials | `workshop` / `demo` + `demo` |
| Issuer | `http://localhost:8080/realms/workshop` |
| MCP resource identifier | `http://localhost:7071/mcp` |
| MCP permission scope | `mcp:tools` |
| Inspector origin | `http://127.0.0.1:6274` |

The realm includes the MCP protected-resource definition. Register the web app's login client with its actual origin and callback URLs, and coordinate resource URLs with the applications participants build.

## Explain or adapt the protocol contract

Use [the MCP documentation index](https://modelcontextprotocol.io/llms.txt) to locate the latest published specification. Keep the Keycloak image pinned unless an upgrade is requested or necessary.

Use [references/protocols.md](references/protocols.md) to find the relevant MCP Markdown documentation for discovery, registration, token handling, and transport requirements.

Read [references/gotchas.md](references/gotchas.md) for the bundled realm's configuration relationships and Keycloak/Inspector interoperability gotchas.

Ask for missing client origin, callback/logout URLs, or resource identity when they affect a requested registration; do not guess framework routes. Starting the default Keycloak instance does not depend on those decisions. Keep requested configuration changes reproducible in the generated realm import. Startup import does not update an existing realm, and a routine configuration change does not justify deleting its volume.

Report generated paths, start command, credentials, and relevant identifiers.
