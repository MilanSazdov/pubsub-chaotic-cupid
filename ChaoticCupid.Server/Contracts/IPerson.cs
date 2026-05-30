namespace ChaoticCupid.Server.Contracts;

// Interface #1: methods that registered persons call on the service.
public interface IPerson
{
    Task InitSinglePerson(string username, string city, int age, string phoneNumber);
    Task ConfirmReceipt();
    Task BlockUser(string username);
}
