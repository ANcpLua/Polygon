using FsCheck.Xunit;

namespace Polygon.Tests;

public class HistoryPropertyTests
{
    [Property]
    public bool UndoInvertsApply(int initialState, int newValue)
    {
        var history = History<int>.Init(initialState);
        var updated = history.Apply((msg, s) => msg, newValue);
        var undone = updated.Undo();

        return updated.CanUndo && undone.Present == initialState;
    }

    [Property]
    public bool RedoInvertsUndo(int initialState, int newValue)
    {
        var history = History<int>.Init(initialState);
        var updated = history.Apply((msg, s) => msg, newValue);
        var undone = updated.Undo();
        var redone = undone.Redo();

        return undone.CanRedo && redone.Present == updated.Present;
    }

    [Property]
    public bool ApplyClearsFuture(int initialState, int[] messages)
    {
        var history = History<int>.Init(initialState);

        history = messages.Take(3).Aggregate(history, (current, msg) => current.Apply((m, s) => s + m, msg));

        if (history.CanUndo)
        {
            history = history.Undo();
            history = history.Apply((m, s) => s + m, 999);
            return !history.CanRedo;
        }

        return true;
    }

    [Property]
    public bool CanUndoImpliesPastNotEmpty(int initialState, int[] messages)
    {
        var history = History<int>.Init(initialState);

        history = messages.Take(5).Aggregate(history, (current, msg) => current.Apply((m, s) => s + m, msg));

        return history.CanUndo == !history.Past.IsEmpty;
    }

    [Property]
    public bool CanRedoImpliesFutureNotEmpty(int initialState, int[] messages)
    {
        var history = History<int>.Init(initialState);

        history = messages.Take(5).Aggregate(history, (current, msg) => current.Apply((m, s) => s + m, msg));

        for (int i = 0; i < 3 && history.CanUndo; i++)
        {
            history = history.Undo();
        }

        return history.CanRedo == !history.Future.IsEmpty;
    }

    [Property]
    public bool UndoWhenEmptyIsIdentity(int initialState)
    {
        var history = History<int>.Init(initialState);
        var afterUndo = history.Undo();

        return afterUndo.Present == history.Present
               && afterUndo.Past.IsEmpty
               && afterUndo.Future.IsEmpty;
    }

    [Property]
    public bool RedoWhenEmptyIsIdentity(int initialState)
    {
        var history = History<int>.Init(initialState);
        var afterRedo = history.Redo();

        return afterRedo.Present == history.Present
               && afterRedo.Past.IsEmpty
               && afterRedo.Future.IsEmpty;
    }

    [Property]
    public bool MultipleUndoRedoCycles(int initialState, int[] messages)
    {
        var history = History<int>.Init(initialState);

        history = messages.Take(10).Aggregate(history, (current, msg) => current.Apply((m, s) => s + m, msg));

        var stateAfterApplies = history.Present;

        while (history.CanUndo)
        {
            history = history.Undo();
        }

        while (history.CanRedo)
        {
            history = history.Redo();
        }

        return history.Present == stateAfterApplies;
    }
}