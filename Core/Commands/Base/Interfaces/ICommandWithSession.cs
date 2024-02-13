using Core.Sessions;

namespace Core.Commands.Base.Interfaces;

public interface ICommandWithSession
{
    public Session Session { get; set; }
}