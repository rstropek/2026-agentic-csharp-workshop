using System.IO.Pipelines;
using GraderCli;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GraderTests.Cli;

/// <summary>
/// Runs <c>grader mcp</c> in memory and connects an MCP client to it, like an MCP client that launches the server: the
/// server's stdin and stdout are in-memory pipes instead of the process's standard streams.
/// </summary>
public sealed class McpServerRunner : IAsyncDisposable
{
    /// <summary>
    /// The protocol revision the tests use: the client requests it and does not fall back to older revisions.
    /// </summary>
    public const string ProtocolVersion = "2026-07-28";

    private readonly PipeWriter serverInput;
    private readonly Task<int> server;
    private readonly StringWriter error;

    private McpServerRunner(McpClient client, PipeWriter serverInput, Task<int> server, StringWriter error)
    {
        Client = client;
        this.serverInput = serverInput;
        this.server = server;
        this.error = error;
    }

    public McpClient Client { get; }

    /// <param name="createHttpClient">Creates the client for the API, e.g. <c>factory.CreateClient</c> of a <see cref="GraderApiFactory"/>.</param>
    public static async Task<McpServerRunner> StartAsync(Func<HttpClient> createHttpClient)
    {
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();
        var error = new StringWriter();
        var context = new CliContext
        {
            // The MCP server must not write to stdout (only JSON-RPC messages on its transport) or read stdin.
            Output = TextWriter.Null,
            Error = error,
            Input = TextReader.Null,
            IsInputRedirected = false,
            GetEnvironmentVariable = _ => null,
            CreateHttpClient = createHttpClient,
            CreateMcpServerTransport = () => new StreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()),
        };
        var ct = TestContext.Current.CancellationToken;
        // The in-memory test server accepts any host.
        var server = CliApp.RunAsync(["mcp", "--api-url", "http://localhost"], context, ct);
        var client = await McpClient.CreateAsync(
            new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()),
            new McpClientOptions { ProtocolVersion = ProtocolVersion },
            cancellationToken: ct);
        return new(client, clientToServer.Writer, server, error);
    }

    /// <summary>
    /// Disconnects the client and closes the server's stdin, like a stdio client shutting down the server, and checks
    /// that the server exited without errors.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        // The client transport does not close the stream it writes to; a real client closes the process's stdin.
        await serverInput.CompleteAsync();
        var exitCode = await server;
        Assert.True(exitCode == 0, $"grader mcp exited with {exitCode}: {error}");
        error.Dispose();
    }
}
