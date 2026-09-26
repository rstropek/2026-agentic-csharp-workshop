using GraderTests.Cli;
using ModelContextProtocol.Client;

namespace GraderTests;

/// <summary>
/// Connects an MCP client to the MCP server of the API in memory (<c>/mcp</c> through the test server's
/// <see cref="HttpClient"/>), pinned to the protocol revision of <see cref="McpServerRunner.ProtocolVersion"/>.
/// </summary>
public static class ApiMcpClient
{
    /// <param name="httpClient">A client of the API in memory, e.g. from <see cref="GraderApiFactory"/>.</param>
    public static Task<McpClient> ConnectAsync(HttpClient httpClient) => McpClient.CreateAsync(
        new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            httpClient),
        new McpClientOptions { ProtocolVersion = McpServerRunner.ProtocolVersion },
        cancellationToken: TestContext.Current.CancellationToken);
}
