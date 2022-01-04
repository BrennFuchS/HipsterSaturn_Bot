using System.Configuration;
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
        static string ReadSetting(string key, string defaultValue)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var appSettings = configFile.AppSettings.Settings;
                if (appSettings[key] == null || string.IsNullOrEmpty(appSettings[key].Value))
                {
                    AddUpdateAppSettings(key, defaultValue);
                }
                string result = appSettings[key].Value;
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return null;
            }
        }
        static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        private readonly IDiscordRestChannelAPI _channelAPI;

        public CommandResponder(IDiscordRestChannelAPI channelAPI)
        {
            _channelAPI = channelAPI;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct)
        {
            var wantedPrefix = ReadSetting($"PREFIX_{gatewayEvent.GuildID}", Program.prefix);

            if ((gatewayEvent.Author.IsBot.IsDefined(out var isBot) && isBot) ||
            (gatewayEvent.Author.IsSystem.IsDefined(out var isSystem) && isSystem) || !gatewayEvent.Content.StartsWith(wantedPrefix))
            {
                return Result.FromSuccess();
            }

            var msg = gatewayEvent.Content;
            var msgC = msg.Split(' ').ToList();
            var pref = msgC[0].Substring(0, wantedPrefix.Length);
            var cmd = msgC[0].Substring(wantedPrefix.Length, msgC[0].Length-wantedPrefix.Length);
            msgC.RemoveAt(0);

            if (pref == wantedPrefix)
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
                    case "set":
                        {
                            if (!(msgC.Count > 0))
                            {
                                var embed = new Embed
                                (
                                    Title: "set command options:", 
                                    Description: "prefix [new prefix]", 
                                    Colour: Color.BlueViolet
                                );

                                replyResult = await _channelAPI.CreateMessageAsync
                                (
                                    gatewayEvent.ChannelID,
                                    embeds: new[] { embed },
                                    ct: ct
                                );
                            }
                            else
                            {
                                switch (msgC[0])
                                {
                                    case "prefix":
                                        {
                                            if (msgC.Count > 1)
                                            {
                                                AddUpdateAppSettings($"PREFIX_{gatewayEvent.GuildID}", msgC[1]);

                                                var embed = new Embed
                                                (
                                                    Title: "prefix",
                                                    Description: $"set to ```{msgC[1]}```",
                                                    Colour: Color.BlueViolet
                                                );

                                                replyResult = await _channelAPI.CreateMessageAsync
                                                (
                                                    gatewayEvent.ChannelID,
                                                    embeds: new[] { embed },
                                                    ct: ct
                                                );
                                            }
                                            else
                                            {
                                                var embed = new Embed
                                                (
                                                    Title: "prefix",
                                                    Description: $"current prefix is ```{$"PREFIX_{gatewayEvent.GuildID}"}```",
                                                    Colour: Color.BlueViolet
                                                );

                                                replyResult = await _channelAPI.CreateMessageAsync
                                                (
                                                    gatewayEvent.ChannelID,
                                                    embeds: new[] { embed },
                                                    ct: ct
                                                );
                                            }

                                            break;
                                        }
                                }
                            }

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
