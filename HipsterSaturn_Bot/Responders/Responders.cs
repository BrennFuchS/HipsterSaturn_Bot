using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Events;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HipsterSaturn_Bot.Responders
{
    public class CommandResponder : IResponder<IMessageCreate>
    {
        private readonly IDiscordRestChannelAPI _channelAPI;

        public CommandResponder(IDiscordRestChannelAPI channelAPI)
        {
            _channelAPI = channelAPI;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct)
        {
            if ((gatewayEvent.Author.IsBot.IsDefined(out var isBot) && isBot) ||
            (gatewayEvent.Author.IsSystem.IsDefined(out var isSystem) && isSystem) || !gatewayEvent.Content.StartsWith(Program.prefix))
            {
                return Result.FromSuccess();
            }

            var msg = gatewayEvent.Content;
            var msgC = msg.Split(' ').ToList();

            var pref = msgC[0].Substring(0, Program.prefix.Length);
            var cmd = msgC[0].Substring(Program.prefix.Length, msgC[0].Length-Program.prefix.Length);
            msgC.RemoveAt(0);

            if (pref == Program.prefix)
            {
                Result<IMessage> replyResult = default;

                switch (cmd)
                {
                    case "ping":
                        {
                            var embed = new Embed(Description: "Pinging...", Colour: Color.YellowGreen);
                            var curTime = DateTime.Now.Millisecond;

                            replyResult = await _channelAPI.CreateMessageAsync
                            (
                                gatewayEvent.ChannelID,
                                embeds: new[] { embed },
                                ct: ct
                            );

                            curTime = DateTime.Now.Millisecond - curTime;

                            embed = new Embed(Description: $"Pong! {curTime}ms", Colour: Color.Green);

                            await _channelAPI.EditMessageAsync
                            (
                                gatewayEvent.ChannelID,
                                messageID: replyResult.Entity.ID,
                                embeds: new[] { embed },
                                ct: ct
                            );

                            break;
                        }
                    case "say":
                        {
                            replyResult = await _channelAPI.CreateMessageAsync
                            (
                                gatewayEvent.ChannelID,
                                string.Join(' ', msgC)
                            );
                            break;
                        }
                }
                if (!replyResult.IsDefined()) return Result.FromSuccess();
                else return !replyResult.IsSuccess ? Result.FromError(replyResult) : Result.FromSuccess();
            }
            else return Result.FromSuccess();
        }
    }
}
