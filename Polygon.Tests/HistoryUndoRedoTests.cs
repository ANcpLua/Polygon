using static LanguageExt.Prelude;

namespace Polygon.Tests;

public class HistoryUndoRedoTests
{

    private static DrawingModel IgnorePast(History<DrawingModel> model) =>
        model.Present with { MousePos = None };

    [Fact]
    public void AddPoint_CreatesPastHistoryEntry()
    {

        var initial = AppUpdate.Init();
        var msg = new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now);

        var result = AppUpdate.Update(msg, initial);

        result.Past.IsEmpty.Should().BeFalse("past should contain previous state");
    }

    [Fact]
    public void FinishPolygon_CreatesPastHistoryEntry()
    {

        var withCurrent = AppUpdate.Init()
            .Pipe(s => AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), s));

        var result = AppUpdate.Update(new Msg.FinishPolygon(), withCurrent);

        result.Past.IsEmpty.Should().BeFalse("past should contain previous state");
    }

    [Fact]
    public void SetCursor_DoesNotCreateHistory()
    {

        var initial = AppUpdate.Init();
        var msg = new Msg.SetCursorPos(Some(new Coord(10.0, 20.0)));

        var result = AppUpdate.Update(msg, initial);

        result.Past.IsEmpty.Should().BeTrue("past should remain empty for cursor movement");
        result.Future.IsEmpty.Should().BeTrue("future should remain empty for cursor movement");
    }

    [Fact]
    public void Undo_WithNoPast_IsNoOp()
    {

        var initial = AppUpdate.Init();

        var result = AppUpdate.Update(new Msg.Undo(), initial);

        IgnorePast(result).Should().Be(IgnorePast(initial), "state should be unchanged");
    }

    [Fact]
    public void Undo_RestoresPreviousState()
    {

        var initial = AppUpdate.Init();
        var afterAdd = AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), initial);

        var afterUndo = AppUpdate.Update(new Msg.Undo(), afterAdd);

        IgnorePast(afterUndo).Should().Be(IgnorePast(initial), "should restore initial state");
    }

    [Fact]
    public void Undo_SetsFutureForRedo()
    {

        var initial = AppUpdate.Init();
        var afterAdd = AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), initial);

        var afterUndo = AppUpdate.Update(new Msg.Undo(), afterAdd);

        afterUndo.Future.IsEmpty.Should().BeFalse("future should be set after undo");
    }

    [Fact]
    public void Redo_WithNoFuture_IsNoOp()
    {

        var initial = AppUpdate.Init();

        var result = AppUpdate.Update(new Msg.Redo(), initial);

        IgnorePast(result).Should().Be(IgnorePast(initial), "state should be unchanged");
    }

    [Fact]
    public void Redo_RestoresUndoneState()
    {

        var initial = AppUpdate.Init();
        var afterAdd = AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), initial);
        var afterUndo = AppUpdate.Update(new Msg.Undo(), afterAdd);

        var afterRedo = AppUpdate.Update(new Msg.Redo(), afterUndo);

        IgnorePast(afterRedo).Should().Be(IgnorePast(afterAdd), "should restore state after add");
    }

    [Fact]
    public void NewAction_AfterUndo_ClearsFuture()
    {

        var initial = AppUpdate.Init();
        var afterAdd = AppUpdate.Update(new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now), initial);
        var afterUndo = AppUpdate.Update(new Msg.Undo(), afterAdd);

        var afterNewAdd = AppUpdate.Update(new Msg.AddPoint(new Coord(30.0, 40.0), TestTime.Now), afterUndo);

        afterNewAdd.Future.IsEmpty.Should().BeTrue("future should be cleared after new action");
    }

    [Fact]
    public void MultipleUndo_MultipleRedo_Chain()
    {

        var s0 = AppUpdate.Init();
        var s1 = AppUpdate.Update(new Msg.AddPoint(new Coord(1.0, 1.0), TestTime.Now), s0);
        var s2 = AppUpdate.Update(new Msg.AddPoint(new Coord(2.0, 2.0), TestTime.Now), s1);
        var s3 = AppUpdate.Update(new Msg.AddPoint(new Coord(3.0, 3.0), TestTime.Now), s2);

        var afterUndo1 = AppUpdate.Update(new Msg.Undo(), s3);
        var afterUndo2 = AppUpdate.Update(new Msg.Undo(), afterUndo1);

        IgnorePast(afterUndo2).Should().Be(IgnorePast(s1), "should be at s1 after 2 undos");

        var afterRedo1 = AppUpdate.Update(new Msg.Redo(), afterUndo2);

        IgnorePast(afterRedo1).Should().Be(IgnorePast(s2), "should be at s2 after 1 redo");
    }

    [Fact]
    public void FinishEmpty_IsSafe()
    {

        var empty = AppUpdate.Init();

        var result = AppUpdate.Update(new Msg.FinishPolygon(), empty);

        IgnorePast(result).Should().Be(IgnorePast(empty), "model should be unchanged");
    }

    [Fact]
    public void UndoOnEmpty_IsEmpty()
    {

        var empty = AppUpdate.Init();

        var result = AppUpdate.Update(new Msg.Undo(), empty);

        IgnorePast(result).Should().Be(IgnorePast(empty), "model should be unchanged");
    }
}