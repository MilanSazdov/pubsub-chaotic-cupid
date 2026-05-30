using System.Collections.Concurrent;
using ChaoticCupid.Server.Models;

namespace ChaoticCupid.Server.Services;

public sealed class PersonRegistry : IPersonRegistry
{
    private readonly ConcurrentDictionary<string, Person> _byUsername =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _connToUser = new();

    public bool TryAdd(Person p)
    {
        if (!_byUsername.TryAdd(p.Username, p)) return false;
        _connToUser[p.ConnectionId] = p.Username;
        return true;
    }

    public Person? GetByConnectionId(string id)
    {
        if (_connToUser.TryGetValue(id, out var user) &&
            _byUsername.TryGetValue(user, out var person))
        {
            return person;
        }
        return null;
    }

    public bool RemoveByConnectionId(string id)
    {
        if (!_connToUser.TryRemove(id, out var user)) return false;
        return _byUsername.TryRemove(user, out _);
    }

    public IReadOnlyCollection<Person> Snapshot() => _byUsername.Values.ToList();
}
