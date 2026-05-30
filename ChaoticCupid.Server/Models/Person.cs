using System.Collections.Concurrent;

namespace ChaoticCupid.Server.Models;

public sealed class Person
{
    public required string Username { get; init; }
    public required string City { get; init; }
    public required int Age { get; init; }
    public required string PhoneNumber { get; init; }
    public string ConnectionId { get; set; } = string.Empty;

    // 0 = no pending letter, 1 = pending. Flipped atomically (CAS) so a tick
    // and a ConfirmReceipt call cannot race.
    private int _hasUnreadLetter;
    public bool HasUnreadLetter => Volatile.Read(ref _hasUnreadLetter) == 1;
    public bool TryMarkAsUnread() => Interlocked.CompareExchange(ref _hasUnreadLetter, 1, 0) == 0;
    public void MarkAsRead() => Interlocked.Exchange(ref _hasUnreadLetter, 0);

    private readonly ConcurrentDictionary<string, byte> _blocked = new(StringComparer.OrdinalIgnoreCase);
    public bool IsBlocked(string username) => _blocked.ContainsKey(username);
    public void Block(string username) => _blocked.TryAdd(username, 0);
}
