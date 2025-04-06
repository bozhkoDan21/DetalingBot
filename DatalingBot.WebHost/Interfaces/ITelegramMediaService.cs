using Telegram.Bot.Types;

public interface ITelegramMediaService
{
    Task<Message> ForwardPhotoToManagerAsync(long clientId, string fileId);
    Task<string> GetTemporaryPhotoLinkAsync(string fileId);
}
