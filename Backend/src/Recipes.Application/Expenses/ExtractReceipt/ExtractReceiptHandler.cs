using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.ExtractReceipt;

public sealed class ExtractReceiptHandler : IRequestHandler<ExtractReceiptCommand, ErrorOr<ExtractedReceiptDto>>
{
    private readonly IReceiptExtractionService _service;

    public ExtractReceiptHandler(IReceiptExtractionService service)
    {
        _service = service;
    }

    public async Task<ErrorOr<ExtractedReceiptDto>> Handle(ExtractReceiptCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.ImageBytes);
        var result = await _service.ExtractAsync(stream, request.ContentType, cancellationToken);
        return result;
    }
}
