using Core.Commands.Base;
using Core.Commands.Recognize;

namespace Core.Commands.Factory;

public interface ICommandsFactory
{
    CommandBase Build(CommandType commandType);
}