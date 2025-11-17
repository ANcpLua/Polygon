using JetBrains.Annotations;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Polygon;

public readonly record struct Coord(double X, double Y);

public readonly record struct PolyLine
{
    public Lst<Coord> Points { get; }

    private PolyLine(Lst<Coord> points) => Points = points;

    [Pure]
    public PolyLine Prepend(Coord coord) => new(coord.Cons(Points));

    public static PolyLine Empty => new(Lst<Coord>.Empty);
    public static PolyLine Single(Coord coord) => new(List(coord));
}

public readonly record struct DrawingModel
{
    public Lst<PolyLine> FinishedPolygons { get; init; }
    public Option<PolyLine> CurrentPolygon { get; init; }
    public Option<Coord> MousePos { get; init; }
    public Option<DateTime> LastClickTime { get; init; }

    public DrawingModel()
    {
        FinishedPolygons = Lst<PolyLine>.Empty;
        CurrentPolygon = None;
        MousePos = None;
        LastClickTime = None;
    }

    public static DrawingModel Empty => new();
}