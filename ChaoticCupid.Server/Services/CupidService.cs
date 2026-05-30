using ChaoticCupid.Server.Contracts;
using ChaoticCupid.Server.Hubs;
using ChaoticCupid.Server.Models;
using ChaoticCupid.Server.Utilities;
using ChaoticCupid.Shared;
using Microsoft.AspNetCore.SignalR;

namespace ChaoticCupid.Server.Services;

public sealed class CupidService : ICupid
{
    // Message at index 2 hides the sender's phone number (spec rule).
    private static readonly string[] Messages =
    {
        "I look forward to our meeting!",
        "I want to get to know you.",
        "I am not interested in meeting."
    };

    private readonly IPersonRegistry _registry;
    private readonly IHubContext<CupidHub> _hub;
    private readonly ILogger<CupidService> _log;

    public CupidService(
        IPersonRegistry registry,
        IHubContext<CupidHub> hub,
        ILogger<CupidService> log)
    {
        _registry = registry;
        _hub = hub;
        _log = log;
    }

    public async Task DeliverLettersAsync(CancellationToken ct)
    {
        var all = _registry.Snapshot();
        if (all.Count < 2)
        {
            _log.LogDebug("[CUPID] fewer than 2 persons; nothing to send");
            return;
        }

        foreach (var receiver in all)
        {
            if (ct.IsCancellationRequested) return;
            if (receiver.HasUnreadLetter) continue;

            var eligible = all
                .Where(s => !s.Username.Equals(receiver.Username, StringComparison.OrdinalIgnoreCase))
                .Where(s => !receiver.IsBlocked(s.Username))
                .ToList();
            if (eligible.Count == 0) continue;

            var bestSender = eligible
                .Select(s => (Sender: s, Score: Score(s, receiver)))
                .OrderByDescending(t => t.Score)
                .First().Sender;

            // CAS so a confirm racing with a tick cannot deliver twice.
            if (!receiver.TryMarkAsUnread()) continue;

            var letter = ComposeLetter(bestSender);
            await _hub.Clients.Client(receiver.ConnectionId)
                .SendAsync("LetterArrived", letter, ct);

            _log.LogInformation("[CUPID] {From} -> {To}", bestSender.Username, receiver.Username);
        }
    }

    private static int Score(Person sender, Person receiver)
    {
        int s = 0;
        if (string.Equals(sender.City, receiver.City, StringComparison.OrdinalIgnoreCase)) s += 30;
        if (Math.Abs(sender.Age - receiver.Age) <= 2) s += 20;
        s += CryptoRandom.Next(0, 101);
        return s;
    }

    private static LetterDto ComposeLetter(Person sender)
    {
        int idx = CryptoRandom.Next(0, Messages.Length);
        return new LetterDto
        {
            FromUsername = sender.Username,
            FromCity = sender.City,
            FromAge = sender.Age,
            FromPhoneNumber = idx == 2 ? null : sender.PhoneNumber,
            Message = Messages[idx]
        };
    }
}
