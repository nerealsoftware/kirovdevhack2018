using System.Collections.Generic;

namespace TSA.Interfaces
{
    public interface ITopicGrouper
    {
        IReadOnlyList<ITopic> GroupDocuments(
            IDocumentSource source,
            int numberOfGroups );
    }
}