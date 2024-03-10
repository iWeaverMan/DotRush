using DotRush.Server.Extensions;
using DotRush.Server.Services;
using Microsoft.CodeAnalysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotRush.Server.Handlers;

public class DocumentSymbolHandler : DocumentSymbolHandlerBase {
    private readonly WorkspaceService solutionService;

    public DocumentSymbolHandler(WorkspaceService solutionService) {
        this.solutionService = solutionService;
    }

    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) {
        return new DocumentSymbolRegistrationOptions() {
            DocumentSelector = LanguageServer.SelectorForSourceCodeDocuments
        };
    }
    public override async Task<SymbolInformationOrDocumentSymbolContainer?> Handle(DocumentSymbolParams request, CancellationToken cancellationToken) {
        return await ServerExtensions.SafeHandlerAsync<SymbolInformationOrDocumentSymbolContainer?>(async () => {
            var documentPath = request.TextDocument.Uri.GetFileSystemPath();
            var documentId = solutionService?.Solution?.GetDocumentIdsWithFilePath(documentPath).FirstOrDefault();
            var document = solutionService?.Solution?.GetDocument(documentId);
            if (documentId == null || document == null)
                return null;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
                return null;
            
            var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken);
            if (root == null)
                return null;

            var documentSymbols = TraverseSyntaxTree(root.ChildNodes(), semanticModel);
            if (documentSymbols == null)
                return null;

            return new SymbolInformationOrDocumentSymbolContainer(documentSymbols.Select(it => new SymbolInformationOrDocumentSymbol(it)));
        });
    }

    private List<DocumentSymbol>? TraverseSyntaxTree(IEnumerable<SyntaxNode> nodes, SemanticModel semanticModel) {
        var result = new List<DocumentSymbol>();
        foreach (var node in nodes) {
            if (node is not MemberDeclarationSyntax)
                continue;

            var symbol = semanticModel.GetDeclaredSymbol(node);
            if (symbol == null)
                continue;

            result.Add(new DocumentSymbol() {
                Name = string.IsNullOrEmpty(symbol.Name) ? "<unnamed>" : symbol.Name,
                Kind = symbol.ToSymbolKind(),
                Range = node.GetLocation().ToRange(),
                SelectionRange = node.GetLocation().ToRange(),
                Children = TraverseSyntaxTree(node.ChildNodes(), semanticModel)
            });
        }
        return result;
    }
}