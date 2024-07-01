using Microsoft.EntityFrameworkCore;
using SqlRepositoryBase.Core.Models;

namespace Core.Whitelist;

[Index(nameof(TelegramUserId))]
public class WhitelistStorageElement : SqlStorageElement
{
    public long TelegramUserId { get; set; }
}