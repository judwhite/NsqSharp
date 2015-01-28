using System.Collections.Generic;

namespace NsqSharp.Tests
{
    public class TestData<TInput, TOutput> : Dictionary<TInput, Result<TOutput>>
    {
    }
}
