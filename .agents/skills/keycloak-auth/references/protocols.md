# MCP protocol documentation

Read the relevant Markdown source for the protocol question at hand. These links target the **2026-07-28** revision; use the [documentation index](https://modelcontextprotocol.io/llms.txt) to locate later published revisions.

| Topic | Markdown source |
| --- | --- |
| OAuth flow, scope challenges, response issuer validation (RFC 9207), resource indicators (RFC 8707), bearer tokens | [Authorization](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization/index.md) |
| Protected-resource metadata (RFC 9728), authorization-server metadata (RFC 8414), OIDC discovery and path rules | [Authorization server discovery](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization/authorization-server-discovery.md) |
| CIMD document requirements, registration priority, DCR compatibility | [Client registration](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization/client-registration.md) |
| PKCE, audience validation, refresh rotation, HTTPS, metadata-fetching security | [Authorization security considerations](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization/security-considerations.md) |
| HTTP transport, Origin validation, protocol-version headers | [Streamable HTTP](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http.md) |

For this realm's configuration and interoperability gotchas, see [gotchas.md](gotchas.md).
