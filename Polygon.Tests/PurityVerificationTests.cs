namespace Polygon.Tests;

public class PurityVerificationTests
{
    [Fact]
    public void Update_SameInputs_AlwaysReturnsSameOutput()
    {
        var model = DrawingModel.Empty;
        var msg = new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now);

        var result1 = DrawingUpdate.Update(msg, model);
        var result2 = DrawingUpdate.Update(msg, model);
        var result3 = DrawingUpdate.Update(msg, model);

        result1.Should().BeEquivalentTo(result2);
        result2.Should().BeEquivalentTo(result3);
    }

    [Fact]
    public void Update_DoesNotMutateInput()
    {
        var originalModel = DrawingModel.Empty;
        var msg = new Msg.AddPoint(new Coord(10.0, 20.0), TestTime.Now);

        var finishedBefore = originalModel.FinishedPolygons.Count;
        var currentBefore = originalModel.CurrentPolygon;

        var result = DrawingUpdate.Update(msg, originalModel);

        originalModel.FinishedPolygons.Count.Should().Be(finishedBefore);
        originalModel.CurrentPolygon.Equals(currentBefore).Should().BeTrue();

        result.Should().NotBe(originalModel);
    }

    [Fact]
    public void Update_HasNoObservableSideEffects()
    {
        var model = DrawingModel.Empty;
        var msg = new Msg.AddPoint(new Coord(5.0, 5.0), TestTime.Now);

        var result = DrawingUpdate.Update(msg, model);

        var result2 = DrawingUpdate.Update(msg, model);
        result.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public void Update_IsReferentiallyTransparent()
    {
        var model = DrawingModel.Empty;
        var coord1 = new Coord(1.0, 1.0);
        var coord2 = new Coord(2.0, 2.0);

        var step1 = DrawingUpdate.Update(new Msg.AddPoint(coord1, TestTime.Now), model);
        var step2A = DrawingUpdate.Update(new Msg.AddPoint(coord2, TestTime.Now), step1);

        var step2B = DrawingUpdate.Update(
            new Msg.AddPoint(coord2, TestTime.Now),
            DrawingUpdate.Update(new Msg.AddPoint(coord1, TestTime.Now), model)
        );

        step2A.Should().BeEquivalentTo(step2B);
    }

    [Fact]
    public void Update_OrderOfCallsMatters_ButNotTiming()
    {
        var model = DrawingModel.Empty;
        var msg1 = new Msg.AddPoint(new Coord(1.0, 1.0), TestTime.Now);
        var msg2 = new Msg.AddPoint(new Coord(2.0, 2.0), TestTime.Now);

        var result1Immediate = DrawingUpdate.Update(msg1, model);

        Thread.Sleep(100);

        var result1Later = DrawingUpdate.Update(msg1, model);

        result1Immediate.Should().BeEquivalentTo(result1Later);

        var sequence12 = DrawingUpdate.Update(msg2,
            DrawingUpdate.Update(msg1, model));
        var sequence21 = DrawingUpdate.Update(msg1,
            DrawingUpdate.Update(msg2, model));

        sequence12.CurrentPolygon.IfSome(poly12 =>
            sequence21.CurrentPolygon.IfSome(poly21 =>
            {
                poly12.Points.Count.Should().Be(2);
                poly21.Points.Count.Should().Be(2);

                poly12.Points[0].Should().NotBe(poly21.Points[0]);
            })
        );
    }
}