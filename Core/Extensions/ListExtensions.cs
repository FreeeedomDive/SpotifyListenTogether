using Core.Commands.Recognize;

namespace Core.Extensions;

public static class ListExtensions
{
    public static List<T> AddIf<T>(this List<T> list, bool condition, T commandType)
    {
        if (condition)
        {
            list.Add(commandType);
        }

        return list;
    }
}