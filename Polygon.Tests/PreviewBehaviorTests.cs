using static LanguageExt.Prelude;

namespace Polygon.Tests;

public class PreviewBehaviorTests
{
    [Fact]
    public void SetCursor_WithNoCurrent_UpdatesMousePosOnly()
    {

        var initial = AppUpdate.Init();
        var cursor = Some(new Coord(100.0, 200.0));
        var msg = new Msg.SetCursorPos(cursor);

        var result = AppUpdate.Update(msg, initial);

        result.Present.MousePos.Equals(cursor).Should().BeTrue("mousePos should be updated");
        result.Present.CurrentPolygon.IsNone.Should().BeTrue("current polygon should remain None");
        result.Present.FinishedPolygons.Count.Should().Be(0, "finished polygons should remain empty");
    }

    [Fact]
    public void SetCursor_WithCurrentPolygon_UpdatesMousePos()
    {

        var withCurrent = AppUpdate.Init()
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), s));

        var cursor = Some(new Coord(100.0, 200.0));
        var msg = new Msg.SetCursorPos(cursor);

        var result = AppUpdate.Update(msg, withCurrent);

        result.Present.MousePos.Equals(cursor).Should().BeTrue("mousePos should be updated");
        result.Present.CurrentPolygon.IsSome.Should().BeTrue("current polygon should remain");
    }

    [Fact]
    public void SetCursor_DoesNotModifyPolygons()
    {

        var withData = AppUpdate.Init()
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), s))
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(30.0, 40.0), TestTime.Now), s))
            .Pipe(s => AppUpdate.Update(new Msg.FinishPolygon(), s))
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(50.0, 60.0), TestTime.Now), s));

        var cursor = Some(new Coord(999.0, 999.0));
        var msg = new Msg.SetCursorPos(cursor);

        var after = AppUpdate.Update(msg, withData);

        after.Present.FinishedPolygons.Count.Should().Be(withData.Present.FinishedPolygons.Count,
            "finished polygons unchanged");
        after.Present.CurrentPolygon.Equals(withData.Present.CurrentPolygon).Should().BeTrue(
            "current polygon unchanged");
    }
}