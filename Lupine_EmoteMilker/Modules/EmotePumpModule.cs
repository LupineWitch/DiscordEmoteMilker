using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LupineEmoteMilker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LupineEmoteMilker.Modules
{
    public sealed class EmotePumpModule : ModuleBase<SocketCommandContext>
    {
        private const int min_size = 16, max_size = 128;
        private ILoggingService _logger;
        private WebEmotePullerService _emoteMaidService;

        private readonly string[] allowedFormats = { "png", "webp", "jpg"};

        public EmotePumpModule(ILoggingService logger, WebEmotePullerService milkMaiden)
        {
            _logger = logger;
            LogMessage message = new LogMessage(LogSeverity.Info, nameof(EmotePumpModule),"Module Constructed");
            _logger.LogAsync(message);
            _emoteMaidService = milkMaiden;
        }

        [Command("milkAllEmotes")]
        [Summary
        ("Zips emotes custom emotes and sends the file in the channel")]
        public async Task MilkAllEmotesAsync([Summary("Size of images to milk from the Guild, max 128")] int size = 128,
                                             [Summary("Format of images(allowed we")] string format = "png" )
        {
            IEnumerable<KeyValuePair<string, string>> emoteUrls = Context.Guild.Emotes.Select(e => new KeyValuePair<string, string>(e.Name, e.Url.ChangeUrlFileExtension(format)));
            await this.FetchTheEmotes(size, format, emoteUrls);
        }


        [Command("milkEmotes")]
        [Summary
        ("Zips emotes custom emotes and sends the file in the channel")]
        public async Task MilkEmotesAsync([Summary("Size of images to milk from the Guild, max 128")] int size,
                                             [Summary("Format of images(allowed we")] string format, params string[] names )
        {
            IEnumerable<KeyValuePair<string, string>> emoteUrls = Context.Guild.Emotes.Where(e => names.Contains(e.Name)).Select(e => new KeyValuePair<string,string>(e.Name, e.Url));
            
            if(emoteUrls.Count() <= 0)
            {
                await Context.Channel.SendMessageAsync(string.Format("Couldn't find any emotes with those names! List: {0}", string.Join(',', names)));
                return;
            }

            await this.FetchTheEmotes(size, format, emoteUrls);
        }

        private async Task FetchTheEmotes(int size, string format, IEnumerable<KeyValuePair<string, string>> emoteNamesUrls)
        {
            if (min_size > size && size > max_size)
            {
                await Context.Channel.SendMessageAsync(string.Format("Bad {0} argument, minimum value {1}, max value {2}", nameof(size), min_size, max_size));
                return;
            }

            if (!allowedFormats.Contains(format))
            {
                await Context.Channel.SendMessageAsync(string.Format("Bad {0} argument, specified \"{1}\", valid formats: {2}", nameof(format), format, string.Join(',', allowedFormats)));
                return;
            }

            await Context.Channel.SendMessageAsync("Milking emotes... \uD83D\uDC04\uD83E\uDD5B");

            string zipFile = await _emoteMaidService.DownloadAndZipEmotesAsync(emoteNamesUrls, size, format);

            await Context.Channel.SendFileAsync(zipFile, "Your emoootes! Fresh, warm and unpasteurized!\uD83D\uDC04");
            try
            {
                File.Delete(zipFile);
            }
            catch (Exception e)
            {
                LogMessage logException = new LogMessage(LogSeverity.Error, nameof(EmotePumpModule), "Couldn't delete the file: " + zipFile, exception: e);
                await _logger.LogAsync(logException);
            }
        }
    }
}
