namespace Polygon.Tests;

public class CoreGeometryTests
{
    [Fact]
    public void AddFirstPoint_CreatesNewCurrentPolygon()
    {

        var initial = AppUpdate.Init();
        var msg = new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now);

        var result = AppUpdate.Update(msg, initial);

        result.Present.CurrentPolygon.IsSome.Should().BeTrue("current polygon should exist");
        result.Present.CurrentPolygon.IfSome(poly =>
        {
            poly.Points.Count.Should().Be(1, "should have one point");
        });
    }

    [Fact]
    public void AddSecondPoint_ExtendsCurrentPolygon()
    {

        var initial = AppUpdate.Init();
        var afterFirst = AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), initial);
        var msg = new Msg.AddPoint(new Coord(30.0, 40.0), TestTime.Now);

        var afterSecond = AppUpdate.Update(msg, afterFirst);

        afterSecond.Present.CurrentPolygon.IfSome(poly =>
        {
            poly.Points.Count.Should().Be(2, "should have two points");
        });
    }

    [Fact]
    public void AddedPoints_MaintainVisualOrder()
    {

        var p1 = new Coord(10.0, 20.0);
        var p2 = new Coord(30.0, 40.0);
        var p3 = new Coord(50.0, 60.0);

        var result = AppUpdate.Init()
            .Pipe(state => AppUpdate.Update(new Msg.AddPoint(p1, TestTime.Now), state))
            .Pipe(state => AppUpdate.Update(new Msg.AddPoint(p2, TestTime.Now), state))
            .Pipe(state => AppUpdate.Update(new Msg.AddPoint(p3, TestTime.Now), state));

        result.Present.CurrentPolygon.IfSome(poly =>
        {
            var visualOrder = poly.Points.Rev().ToArray();
            visualOrder.Length.Should().Be(3, "should have 3 points");
            visualOrder[0].Should().Be(p1);
            visualOrder[1].Should().Be(p2);
            visualOrder[2].Should().Be(p3);
        });
    }

    [Fact]
    public void FinishPolygon_MovesCurrentToFinished()
    {

        var withPoints = AppUpdate.Init()
            .Pipe(state => AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), state))
            .Pipe(state => AppUpdate.Update(new Msg.AddPoint(new Coord(30.0, 40.0), TestTime.Now), state));

        var result = AppUpdate.Update(new Msg.FinishPolygon(), withPoints);

        result.Present.CurrentPolygon.IsNone.Should().BeTrue("current polygon should be cleared");
        result.Present.FinishedPolygons.Count.Should().Be(1, "should have one finished polygon");
    }

    [Fact]
    public void MultiplePolygons_CanBeCreated()
    {

        var result = AppUpdate.Init()
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(0.0, 0.0), TestTime.Later(0)), s))
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 0.0), TestTime.Later(400)), s))
            .Pipe(s => AppUpdate.Update(new Msg.FinishPolygon(), s))
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(20.0, 20.0), TestTime.Later(800)), s))
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(30.0, 30.0), TestTime.Later(1200)), s))
            .Pipe(s => AppUpdate.Update(new Msg.FinishPolygon(), s));

        result.Present.FinishedPolygons.Count.Should().Be(2, "should have two finished polygons");
        result.Present.CurrentPolygon.IsNone.Should().BeTrue("current should be clear after finish");
    }
}