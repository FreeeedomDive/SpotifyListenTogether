using SqlRepositoryBase.Core.Models;

namespace Core.Sessions.Storage;

public class SessionStorageElement : SqlStorageElement
{
    public string SerializedSession { get; set; }
}