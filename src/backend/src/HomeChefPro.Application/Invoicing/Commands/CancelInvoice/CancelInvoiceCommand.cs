using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Invoicing;
using MediatR;

namespace HomeChefPro.Application.Invoicing.Commands.CancelInvoice;

public sealed record CancelInvoiceCommand(Guid InvoiceId, string Reason) : IRequest;

public sealed class CancelInvoiceValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(4000);
    }
}

public sealed class CancelInvoiceHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<CancelInvoiceCommand>
{
    public async Task Handle(CancelInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([request.InvoiceId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);
        invoice.Cancel(request.Reason, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
