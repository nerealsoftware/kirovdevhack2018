using System;
using System.Collections.Generic;

namespace TSA.Interfaces
{
    public interface IDocumentSource
    {
        IEnumerable<IDocument> GetDocuments();
    }
}
