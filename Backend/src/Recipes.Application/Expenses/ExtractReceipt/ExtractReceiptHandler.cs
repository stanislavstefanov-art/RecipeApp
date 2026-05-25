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
        try
        {
            using var stream = new MemoryStream(request.ImageBytes);
            return await _service.ExtractAsync(stream, request.ContentType, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Failure("ReceiptExtraction.Failed", ex.Message);
        }
    }
}
