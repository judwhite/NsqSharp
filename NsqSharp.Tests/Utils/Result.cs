using System;

namespace NsqSharp.Tests.Utils
{
    public class Result<T>
    {
        public Result(T expected)
        {
            Expected = expected;
        }

        public Result(Type expectedException)
        {
            ExpectedException = expectedException;
        }

        public T Expected { get; private set; }

        public Type ExpectedException { get; private set; }

        public bool ShouldPass
        {
            get { return ExpectedException == null; }
        }
    }

    public class Result<T, TException> : Result<T>
        where TException : Exception
    {
        public Result()
            : base(typeof(TException))
        {
        }
    }
}
