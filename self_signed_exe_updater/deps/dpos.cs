
using Discord;
using Discord.WebSocket;

internal static  class dpos
    {
        public static string _logging ="";
    static DiscordSocketClient cli;
    public static void Initialize()
        {
            cli = new DiscordSocketClient(
              new DiscordSocketConfig()
              {
                  LogLevel = Discord.LogSeverity.Debug,
                  AlwaysDownloadUsers = true,
                  UseSystemClock = true,
                  GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.GuildEmojis | GatewayIntents.GuildMessages | GatewayIntents.Guilds
              | GatewayIntents.DirectMessageTyping | GatewayIntents.DirectMessages | GatewayIntents.GuildVoiceStates | GatewayIntents.All,

              });
            cli.StartAsync();
            cli.LoginAsync(TokenType.Bot, "");
            cli.Log += Cli_Log;
        }
    public static void Deinitialize()
        {
            if (cli != null)
            {
                cli.StopAsync();
                cli.LogoutAsync();
                cli.Dispose();
            }
        }
        public static void Release(string filename, string message)
        {

            var g = cli.GetGuild(1122303309894656031);
            if (g != null)
            {
               var ch = g.GetChannel(1185958016156176486);
               IMessageChannel messageChannel = ch as IMessageChannel;
                if (messageChannel!=null)
                {
                    messageChannel.SendFileAsync(filename, message);
                }
            }
        }
        private static Task Cli_Log(LogMessage arg)
        {
            _logging += arg.Message;
            return Task.CompletedTask;
        }
    }
