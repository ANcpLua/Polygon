using LanguageExt;

namespace Polygon;

public abstract record Msg
{
    private Msg() { }

    public sealed record AddPoint(Coord Point, DateTime Timestamp) : Msg;
    public sealed record SetCursorPos(Option<Coord> Position) : Msg;
    public sealed record FinishPolygon : Msg;
    public sealed record Undo : Msg;
    public sealed record Redo : Msg;
}

public static class MsgExtensions
{
    public static TResult Match<TResult>(
        this Msg msg,
        Func<Coord, DateTime, TResult> onAddPoint,
        Func<Option<Coord>, TResult> onSetCursorPos,
        Func<TResult> onFinishPolygon,
        Func<TResult> onUndo,
        Func<TResult> onRedo)
    {
        return msg switch
        {
            Msg.AddPoint m => onAddPoint(m.Point, m.Timestamp),
            Msg.SetCursorPos m => onSetCursorPos(m.Position),
            Msg.FinishPolygon => onFinishPolygon(),
            Msg.Undo => onUndo(),
            Msg.Redo => onRedo()
        };
    }

    public static bool CreatesHistory(this Msg msg) =>
        msg is not Msg.SetCursorPos and not Msg.Undo and not Msg.Redo;
}