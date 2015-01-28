namespace NsqSharp.Tests
{
    public class Result<T>
    {
        public Result(bool shouldPass, T expected)
        {
            ShouldPass = shouldPass;
            Expected = expected;
        }

        public bool ShouldPass { get; private set; }
        public T Expected { get; private set; }
    }
}
