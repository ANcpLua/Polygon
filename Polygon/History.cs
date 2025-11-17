using JetBrains.Annotations;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Polygon;

public readonly record struct History<T>
{
    public Lst<T> Past { get; }
    public T Present { get; init; }
    public Lst<T> Future { get; }

    public History(T initial)
    {
        Past = Lst<T>.Empty;
        Present = initial;
        Future = Lst<T>.Empty;
    }

    private History(Lst<T> past, T present, Lst<T> future)
    {
        Past = past;
        Present = present;
        Future = future;
    }

    [Pure]
    public static History<T> Init(T initial) => new(initial);

    [Pure]
    public History<T> Apply<TMsg>(Func<TMsg, T, T> step, TMsg msg)
    {
        var newState = step(msg, Present);
        return new History<T>(
            past: Present.Cons(Past),
            present: newState,
            future: Lst<T>.Empty
        );
    }

    [Pure]
    public History<T> Undo()
    {
        if (Past.IsEmpty)
            return this;

        var current = this;
        var headOption = (Option<T>)Past.Head();
        return headOption.Match(
            Some: prevState => new History<T>(
                past: toList(current.Past.Tail()),
                present: prevState,
                future: current.Present.Cons(current.Future)
            ),
            None: () => throw new InvalidOperationException("Empty past")
        );
    }

    [Pure]
    public History<T> Redo()
    {
        if (Future.IsEmpty)
            return this;

        var current = this;
        var headOption = (Option<T>)Future.Head();
        return headOption.Match(
            Some: nextState => new History<T>(
                past: current.Present.Cons(current.Past),
                present: nextState,
                future: toList(current.Future.Tail())
            ),
            None: () => throw new InvalidOperationException("Empty future")
        );
    }

    public bool CanUndo => !Past.IsEmpty;

    public bool CanRedo => !Future.IsEmpty;
}