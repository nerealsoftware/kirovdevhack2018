using System;
using System.Collections.Generic;

namespace TSA.Interfaces {
    public interface ITopic
    {
        IReadOnlyList<ValueTuple<IDocument, float>> Documents { get; }
    }
}