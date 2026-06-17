using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Interfaces.Documents;

public interface IDocumentComparisonService
{
    /// <summary>
    /// Compares the content of 2 to 5 documents using AI and returns the result as a Markdown string.
    /// </summary>
    Task<string> CompareDocumentsAsync(List<Guid> documentIds, Guid? requesterUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
