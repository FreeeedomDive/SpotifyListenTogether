using Core.Sessions;

namespace Core.Commands.Base.Interfaces;

public interface ICommandWithSession
{
    Session Session { get; set; }
}