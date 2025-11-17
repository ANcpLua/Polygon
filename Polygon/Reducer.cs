using JetBrains.Annotations;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Polygon;

public static class DrawingUpdate
{
    private const int DoubleClickThresholdMs = 300;

    [Pure]
    public static DrawingModel Update(Msg msg, DrawingModel model)
    {
        return msg.Match(
            onAddPoint: (coord, timestamp) => AddPoint(coord, timestamp, model),
            onSetCursorPos: coord => model with { MousePos = coord },
            onFinishPolygon: () => FinishPolygon(model),
            onUndo: () => model,
            onRedo: () => model
        );
    }

    [Pure]
    private static DrawingModel AddPoint(Coord point, DateTime timestamp, DrawingModel model)
    {
        var isDoubleClick = model.LastClickTime.Match(
            Some: lastTime => (timestamp - lastTime).TotalMilliseconds < DoubleClickThresholdMs,
            None: () => false
        );

        if (isDoubleClick)
        {
            return FinishPolygon(model);
        }

        var newPolygon = model.CurrentPolygon.Match(
            Some: poly => poly.Prepend(point),
            None: () => PolyLine.Single(point)
        );
        return model with
        {
            CurrentPolygon = Some(newPolygon),
            LastClickTime = Some(timestamp)
        };
    }

    [Pure]
    private static DrawingModel FinishPolygon(DrawingModel model)
    {
        return model.CurrentPolygon.Match(
            Some: currentPoly => model with
            {
                CurrentPolygon = None,
                FinishedPolygons = currentPoly.Cons(model.FinishedPolygons)
            },
            None: () => model
        );
    }
}

public static class AppUpdate
{
    [Pure]
    public static History<DrawingModel> Update(Msg msg, History<DrawingModel> history)
    {
        return msg switch
        {
            Msg.Undo => history.Undo(),
            Msg.Redo => history.Redo(),
            Msg.SetCursorPos => history with { Present = DrawingUpdate.Update(msg, history.Present) },
            _ when msg.CreatesHistory() => history.Apply(DrawingUpdate.Update, msg),
            _ => history
        };
    }

    [Pure]
    public static History<DrawingModel> Init() => History<DrawingModel>.Init(DrawingModel.Empty);
}