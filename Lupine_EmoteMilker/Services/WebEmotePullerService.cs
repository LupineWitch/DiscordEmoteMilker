using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace LupineEmoteMilker.Services
{
    public sealed class WebEmotePullerService
    {
        private readonly ILoggingService _logger;

        /// <summary>
        /// As per Microsoft's documentation, there should be one instance of HttpClient per application, to prevent Sockets leak.
        /// HttpClient is thread safe, so it's ok to use it in injected services.
        /// </summary>
        private readonly HttpClient _client; 

        public WebEmotePullerService(ILoggingService logger)
        {
            _logger = logger;
            LogMessage message = new LogMessage(LogSeverity.Info, nameof(WebEmotePullerService), string.Format("{0} initialized", nameof(WebEmotePullerService)));
            _logger.LogAsync(message);
            _client = new HttpClient();
        }

        public async Task<string> DownloadAndZipEmotesAsync(IEnumerable<KeyValuePair<string,string>> emoteNamesUrls, int size = 128, string format = "png")
        {
            string folderName = Path.GetRandomFileName();
            string tempDir = Path.Combine(Path.GetTempPath(), folderName);
            if(Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);
            List<Task<HttpResponseMessage>> responses = new List<Task<HttpResponseMessage>>();

            foreach (KeyValuePair<string, string> emoteNameUrl in emoteNamesUrls)
            {
                Dictionary<string, string> urlQueryParams = new Dictionary<string, string>()
                {
                    ["size"] = size.ToString(),
                    ["quality"] = "lossless",
                    ["name"] = emoteNameUrl.Key,
                };
                LogMessage message = new LogMessage(
                  LogSeverity.Debug,
                  nameof(WebEmotePullerService),
                  string.Format("Request for:", emoteNameUrl.Value.GetUrlWithQueryString(urlQueryParams)));
                _logger.LogAsync(message);
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, emoteNameUrl.Value.GetUrlWithQueryString(urlQueryParams));
                responses.Add(_client.SendAsync(requestMessage));
            }

            while(responses.Count > 0)
            {
                Task<HttpResponseMessage> imageTaskResult = await Task.WhenAny(responses);
                responses.Remove(imageTaskResult);
                Regex matchEmoteName = new Regex("(?<=&name=\"*)[^\"]*");
                string requestUrl = imageTaskResult.Result.RequestMessage.RequestUri.ToString();
                Match match =  matchEmoteName.Match(requestUrl);
                string imageName = match.Equals(Match.Empty) ? requestUrl.Split('?').First().Split('/').Last() : match.Value + "." + format;

                if (!imageTaskResult.Result.IsSuccessStatusCode)
                {
                    LogMessage downloadFailed = new LogMessage(
                        LogSeverity.Error,
                        nameof(WebEmotePullerService),
                        string.Format("Failed to download: {0}, code: {1}, Reasoning: {2}", imageName, imageTaskResult.Result.StatusCode, imageTaskResult.Result.ReasonPhrase));
                    _logger.LogAsync(downloadFailed);
                    continue;
                }


                Stream imageStream = await imageTaskResult.Result.Content.ReadAsStreamAsync();
                imageStream.Position = 0;
                using (FileStream fileStream = File.Open(Path.Combine(tempDir,imageName),FileMode.CreateNew))
                {
                    await imageStream.CopyToAsync(fileStream);
                    fileStream.Close();
                }
                imageStream.Close();
                await imageStream.DisposeAsync();
            }

                string zipFilename = Path.Combine(Path.GetTempPath(), folderName + ".zip");
                ZipFile.CreateFromDirectory(tempDir, zipFilename);
                return zipFilename;
        }

    }
}
