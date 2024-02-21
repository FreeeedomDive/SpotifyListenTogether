using Core.Sessions;
using Core.Sessions.Models;

namespace Core.Commands.Base.Interfaces;

public interface ICommandWithSession
{
    Session Session { get; set; }
}