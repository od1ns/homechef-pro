using System.Text.Json;
using HomeChefPro.Application.Invoicing.Abstractions;

namespace HomeChefPro.Infrastructure.Invoicing;

/// <summary>
/// Development-only fiscal provider. Numera con timestamp + secuencia para
/// que los numeros sean unicos a traves de restarts del proceso (un contador
/// estatico en memoria colisionaba con facturas existentes en la BD despues
/// de un docker compose up --build).
///
/// Reemplazar por un provider real (Z-Comp, HKA, etc.) cuando el chef tenga
/// cuenta fiscal.
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
        var stamp = DateTimeOffset.UtcNow.ToString(
            "yyyyMMddHHmmss",
            System.Globalization.CultureInfo.InvariantCulture);
        var fiscalNumber = $"MOCK-{stamp}-{seq:D4}";
        var controlNumber = $"CTRL-{stamp}-{seq:D4}";
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
