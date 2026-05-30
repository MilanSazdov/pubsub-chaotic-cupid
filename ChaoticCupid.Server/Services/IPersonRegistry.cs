using ChaoticCupid.Server.Models;

namespace ChaoticCupid.Server.Services;

public interface IPersonRegistry
{
    bool TryAdd(Person person);
    Person? GetByConnectionId(string connectionId);
    bool RemoveByConnectionId(string connectionId);
    IReadOnlyCollection<Person> Snapshot();
}
