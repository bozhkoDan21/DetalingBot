using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace DatalingBot.WebHost.Services
{
    public class TelegramMediaService : ITelegramMediaService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ICustomLogger _logger;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IMapper _mapper;

        public TelegramMediaService(
        ITelegramBotClient botClient,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ICustomLogger logger,
        IMapper mapper) 
        {
            _botClient = botClient;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _mapper = mapper; 
        }

        public async Task<Message> ForwardPhotoToManagerAsync(long clientId, string fileId)
        {
            // Получаем chat_id менеджера из БД или конфига
            long managerChatId = await GetManagerChatIdAsync();

            return await _botClient.SendPhotoAsync(
                chatId: managerChatId,
                photo: InputFile.FromFileId(fileId),
                caption: $"Фото от клиента #{clientId}");
        }

        public async Task<string> GetTemporaryPhotoLinkAsync(string fileId)
        {
            // Генерируем временную ссылку (Telegram File API)
            var file = await _botClient.GetFileAsync(fileId);
            return $"https://api.telegram.org/file/bot<token>/{file.FilePath}";
        }

        public async Task<DTO_Review> MapReviewWithPhotoUrlsAsync(Review review, ITelegramMediaService mediaService)
        {
            var dto = _mapper.Map<DTO_Review>(review);

            if (!string.IsNullOrEmpty(review.PhotoBeforeTempId))
            {
                dto.PhotoBeforeUrl = await mediaService.GetTemporaryPhotoLinkAsync(review.PhotoBeforeTempId);
            }

            if (!string.IsNullOrEmpty(review.PhotoAfterTempId))
            {
                dto.PhotoAfterUrl = await mediaService.GetTemporaryPhotoLinkAsync(review.PhotoAfterTempId);
            }

            return dto;
        }

        private async Task<long> GetManagerChatIdAsync()
        {
            // Вариант 1: Из конфигурации (простой способ)
            // return long.Parse(_configuration["Telegram:ManagerChatId"]);

            // Вариант 2: Из базы данных (более гибкий)
            await using var context = _dbContextFactory.CreateDbContext();
            var manager = await context.Users
                .Where(u => u.IsManager)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();

            return manager?.TelegramChatId
                ?? throw new InvalidOperationException("No manager found in database");
        }
    }
}
