using Core.Commands.Base;
using Core.Commands.Recognize;

namespace Core.Commands.Factory;

public interface ICommandsFactory
{
    ICommandBase Build(CommandType commandType);
}