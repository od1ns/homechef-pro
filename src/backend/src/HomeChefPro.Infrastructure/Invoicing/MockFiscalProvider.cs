using System.Text.Json;
using HomeChefPro.Application.Invoicing.Abstractions;

namespace HomeChefPro.Infrastructure.Invoicing;

/// <summary>
/// Development-only fiscal provider. Hands out MOCK-{n}/CTRL-{n} numbers
/// monotonically. Replace by a real provider impl (Z-Comp, HKA, etc.) when
/// the chef has an account.
/// </summary>
public sealed class MockFiscalProvider : IFiscalProvider
{
    private static int _seq;

    public string ProviderName => "mock";

    public Task<FiscalEmissionResult> EmitAsync(
        FiscalEmissionRequest request,
        CancellationToken ct = default)
    {
        var seq = Interlocked.Increment(ref _seq);
        var fiscalNumber = $"MOCK-{seq:D8}";
        var controlNumber = $"CTRL-{seq:D8}";
        var rawResponse = JsonSerializer.Serialize(new
        {
            provider = ProviderName,
            fiscalNumber,
            controlNumber,
            order = request.OrderNumber,
            issuedAt = DateTimeOffset.UtcNow,
        });

        return Task.FromResult(new FiscalEmissionResult(
            Succeeded: true,
            FiscalNumber: fiscalNumber,
            ControlNumber: controlNumber,
            RawResponseJson: rawResponse,
            FailureReason: null));
    }
}
