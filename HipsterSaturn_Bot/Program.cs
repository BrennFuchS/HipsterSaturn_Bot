using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Commands.Extensions;
using Remora.Discord.API;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Gateway.Results;
using Remora.Discord.Hosting.Extensions;
using Remora.Discord.Interactivity.Extensions;
using Remora.Discord.Pagination.Extensions;
using Remora.Rest.Core;
using Remora.Results;
using HipsterSaturn_Bot.Responders;

namespace HipsterSaturn_Bot
{
    internal class Program
    {
        public static string prefix;

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();

            var cancellationSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationSource.Cancel();
            };

            var botToken = config.GetSection("BOT_TOKEN").Get<string>();
            prefix = config.GetSection("PREFIX").Get<string>();

            var services = new ServiceCollection()
                .AddDiscordGateway(_ => botToken)
                .AddResponder<CommandResponder>()
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();

            var gatewayClient = services.GetRequiredService<DiscordGatewayClient>();
            var log = services.GetRequiredService<ILogger<Program>>();
            var runResult = await gatewayClient.RunAsync(cancellationSource.Token);

            if (!runResult.IsSuccess)
            {
                switch (runResult.Error)
                {
                    case ExceptionError exe:
                        {
                            log.LogError
                            (
                                exe.Exception,
                                "Exception during gateway connection: {ExceptionMessage}",
                                exe.Message
                            );

                            break;
                        }
                    case GatewayWebSocketError:
                    case GatewayDiscordError:
                        {
                            log.LogError("Gateway error: {Message}", runResult.Error.Message);
                            break;
                        }
                    default:
                        {
                            log.LogError("Unknown error: {Message}", runResult.Error.Message);
                            break;
                        }
                }
            }

            Console.WriteLine("Bye bye");
        }
    }
}
