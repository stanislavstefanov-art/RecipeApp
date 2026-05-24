using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.ExtractReceipt;

public sealed record ExtractReceiptCommand(byte[] ImageBytes, string ContentType) : IRequest<ErrorOr<ExtractedReceiptDto>>;
