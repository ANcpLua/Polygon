namespace Polygon.Tests;

public static class PipeExtensions
{
    public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> f) => f(input);
}

public static class TestTime
{
    public static DateTime Now => new(2025, 1, 1, 12, 0, 0);
    public static DateTime Later(int milliseconds) => Now.AddMilliseconds(milliseconds);
}
