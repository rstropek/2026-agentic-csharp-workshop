# Keycloak configuration and workshop gotchas

## Preserve these realm relationships

- **Resource binding:** `mcp:tools` is a realm-default optional scope, assigned to newly discovered CIMD clients. Its audience mapper, the bearer-only resource client's ID, and that client's `resource_url` attribute all contain `http://localhost:7071/mcp`. Keep those aligned when changing the MCP resource. Keycloak's `resource-indicators` feature needs this mapping; the flag alone is insufficient.
- **Separate policies:** keep `require-pkce` under its own `any-client` condition. The CIMD URI condition runs at pre-authorization; PKCE auto-configuration/enforcement also needs later events.
- **CIMD trust:** the condition permits client IDs on `www.mcpjam.com`. The executor checks every URL-valued metadata property, including all redirects and the logo, so its domain allowlist is broader. Same-domain restriction is off for remote metadata with loopback callbacks. Keycloak fetches and caches [MCPJam's document](https://www.mcpjam.com/.well-known/oauth/client-metadata.json); document changes can require allowlist updates and outbound HTTPS access.
- **Minimal scopes:** this import defines only `mcp:tools`. An OIDC library requesting `profile email` can get `invalid_scope`; add the corresponding scope definitions/mappers and assignments if needed, or request the available scopes. Merely storing an email on the demo user does not expose an email token claim.
- **Refresh rotation:** zero reuse is allowed. Concurrent refreshes can produce `invalid_grant`. An invalid refresh attempt can also consume its token; obtain a fresh grant before further attempts. Ordinary session refresh does not require `offline_access`.

## Released behavior versus documentation

The [26.7.4 MCP guide](https://github.com/keycloak/keycloak/blob/26.7.4/docs/guides/securing-apps/mcp-authz-server.adoc) describes resource indicators as missing, but the released image includes experimental `resource-indicators`. Keep both bundled feature flags. The [release implementation](https://github.com/keycloak/keycloak/blob/26.7.4/services/src/main/java/org/keycloak/protocol/oidc/resourceindicators/ResourceIndicatorsPostProcessor.java) explains the audience lookup through `resource_url`.

## MCPJam Inspector

Run `npx @mcpjam/inspector@latest` (Node.js 22+). Use HTTP, the MCP URL, OAuth, registration **CIMD**, protocol **Auto**, and scope `mcp:tools`. Sign in as `demo` / `demo`, approve consent, then invoke a tool. In 3.11.0, **Set up later → Connect → Add Server** avoids an onboarding overlay that can cover the authorization prompt.

Its **Resource identifier mismatch** trace can show identical URLs: check the details for an HTTP-versus-HTTPS compliance warning. This loopback demo uses HTTP. Inspector's backend proxy also bypasses browser-to-resource CORS concerns; direct SPA access needs its own CORS configuration. Keep Inspector's callback URL among those declared in its hosted metadata when changing ports.

## Persistence and networking

H2 data, users, and password hashes persist in the project-prefixed `keycloak-data` volume at `/opt/keycloak/data`; the files contain the public seed passwords. Startup import **skips existing realms**. Apply ongoing changes to the realm and maintain the JSON for clean starts. An explicitly disposable `docker compose down -v` reset deletes project volumes, potentially including other services after a Compose merge. After reset, reload resource-server signing-key caches and reauthorize clients; removing/re-adding the Inspector connection clears stale tokens.

The bundled configuration assumes host-run applications. A container's `localhost` points to itself, while the canonical issuer remains `http://localhost:8080/realms/workshop`. Resolve reachability while preserving issuer validation. Port 7071 avoids macOS AirPlay's common use of 5000.
