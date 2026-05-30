using ChaoticCupid.Server.Contracts;
using ChaoticCupid.Server.Models;
using ChaoticCupid.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChaoticCupid.Server.Hubs;

public sealed class CupidHub : Hub, IPerson
{
    private readonly IPersonRegistry _registry;
    private readonly ILogger<CupidHub> _log;

    public CupidHub(IPersonRegistry registry, ILogger<CupidHub> log)
    {
        _registry = registry;
        _log = log;
    }

    public async Task InitSinglePerson(string username, string city, int age, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            await Clients.Caller.SendAsync("RegistrationStatus", false, "Username cannot be empty.");
            return;
        }
        if (username.Any(char.IsDigit))
        {
            await Clients.Caller.SendAsync("RegistrationStatus", false, "Username cannot contain digits.");
            return;
        }
        if (string.IsNullOrWhiteSpace(city))
        {
            await Clients.Caller.SendAsync("RegistrationStatus", false, "City cannot be empty.");
            return;
        }
        if (age <= 0)
        {
            await Clients.Caller.SendAsync("RegistrationStatus", false, "Age must be a positive integer.");
            return;
        }
        if (string.IsNullOrWhiteSpace(phoneNumber) ||
            !phoneNumber.TrimStart('+').All(char.IsDigit))
        {
            await Clients.Caller.SendAsync("RegistrationStatus", false,
                "Phone must be digits (optionally with leading +).");
            return;
        }

        var person = new Person
        {
            Username = username.Trim(),
            City = city.Trim(),
            Age = age,
            PhoneNumber = phoneNumber.Trim(),
            ConnectionId = Context.ConnectionId
        };

        if (_registry.TryAdd(person))
        {
            await Clients.Caller.SendAsync("RegistrationStatus", true, $"Welcome, {person.Username}!");
            _log.LogInformation("[SERVER] {Username} registered ({ConnId})",
                person.Username, Context.ConnectionId);
        }
        else
        {
            await Clients.Caller.SendAsync("RegistrationStatus", false, "Username already taken.");
        }
    }

    public Task ConfirmReceipt()
    {
        var person = _registry.GetByConnectionId(Context.ConnectionId);
        if (person is null) return Task.CompletedTask;
        person.MarkAsRead();
        _log.LogDebug("[SERVER] {Username} confirmed receipt", person.Username);
        return Task.CompletedTask;
    }

    public Task BlockUser(string username)
    {
        var person = _registry.GetByConnectionId(Context.ConnectionId);
        if (person is null || string.IsNullOrWhiteSpace(username)) return Task.CompletedTask;

        var target = username.Trim();
        person.Block(target);
        _log.LogInformation("[SERVER] {From} blocked {Target}", person.Username, target);
        return Clients.Caller.SendAsync("Notification", $"You blocked '{target}'.");
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.RemoveByConnectionId(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
