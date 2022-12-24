using System.CommandLine;
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using Tcp2Np;

var command = new RootCommand();

var endpointArgument = new Argument<IPEndPoint>(
    static result => IPEndPoint.Parse(result.Tokens[0].Value));

var pipeNameArgument = new Argument<string>();

command.AddArgument(endpointArgument);
command.AddArgument(pipeNameArgument);

var parseResult = command.Parse(args);

if (parseResult.Errors.Any())
{
    foreach (var error in parseResult.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return 1;
}

var endpoint = parseResult.GetValueForArgument(endpointArgument);
var pipeName = parseResult.GetValueForArgument(pipeNameArgument);

var appBuilder = Host.CreateApplicationBuilder(parseResult.UnparsedTokens.ToArray());

appBuilder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

appBuilder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
});

appBuilder.Services.Configure<ProxyServiceOptions>(
    options =>
    {
        options.Endpoint = endpoint;
        options.PipeName = pipeName;
    });

appBuilder.Services.AddHostedService<ProxyService>();

await appBuilder.Build().RunAsync();

return 0;
