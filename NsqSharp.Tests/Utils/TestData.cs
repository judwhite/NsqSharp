using System.Collections.Generic;

namespace NsqSharp.Tests.Utils
{
    public class TestData<TInput, TOutput> : Dictionary<TInput, Result<TOutput>>
    {
    }
}
