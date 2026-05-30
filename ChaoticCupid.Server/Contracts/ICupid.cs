namespace ChaoticCupid.Server.Contracts;

// Interface #2: Cupid's own contract, driven by the periodic background timer.
public interface ICupid
{
    Task DeliverLettersAsync(CancellationToken ct);
}
