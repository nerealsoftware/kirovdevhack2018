using System.Collections.Generic;

namespace TSA.Interfaces {
    public interface ITopic
    {
        IReadOnlyList<IDocument> Documents { get; }
    }
}