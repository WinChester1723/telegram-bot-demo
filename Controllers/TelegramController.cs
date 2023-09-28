using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xabe.FFmpeg;


namespace TelegramBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramController : ControllerBase
    {
        private readonly ITelegramBotClient _telegramBotClient;

        public TelegramController(IConfiguration configuration)
        {
            var botToken = configuration.GetValue<string>("Token");
            _telegramBotClient = new TelegramBotClient(botToken);
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Video)
            {
                var video = update.Message.Video;
                var fileId = video.FileId;
                var file = await _telegramBotClient.GetFileAsync(fileId);
                var filePath = Path.Combine("temp", file.FileId + ".mp4");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await _telegramBotClient.DownloadFileAsync(file.FilePath, stream);
                }

                var audioPath = await ExtractAudioAsync(filePath);

                using (var stream = new FileStream(audioPath, FileMode.Open))
                {
                    var inputFile = new InputFileStream(stream);
                    await _telegramBotClient.SendAudioAsync(update.Message.Chat.Id, inputFile, title: "Extracted Audio");
                }

                System.IO.File.Delete(filePath);
                System.IO.File.Delete(audioPath);
            }

            return Ok();
        }

        private async Task<string> ExtractAudioAsync(string videoPath)
        {
            var audioPath = Path.ChangeExtension(videoPath, ".mp3");

            var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(videoPath, audioPath);
            await conversion.Start();

            return audioPath;
        }
    }
}
