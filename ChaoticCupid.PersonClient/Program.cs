using ChaoticCupid.Shared;
using Microsoft.AspNetCore.SignalR.Client;

const string DefaultHubUrl = "https://localhost:7284/cupidHub";
string hubUrl = args.Length > 0 ? args[0] : DefaultHubUrl;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Chaotic Cupid Person Client ===");
Console.WriteLine($"Server: {hubUrl}");
Console.WriteLine();

string username = AskText("Username (non-empty, no digits): ", ValidateUsername);
string city     = AskText("City     (non-empty)            : ", ValidateNonEmpty);
int    age      = AskInt ("Age      (positive integer)     : ");
string phone    = AskText("Phone    (digits, optional +)   : ", ValidatePhone);

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

var registered = new TaskCompletionSource<bool>();

connection.On<LetterDto>("LetterArrived", DisplayLetter);

connection.On<bool, string>("RegistrationStatus", (ok, msg) =>
{
    Console.WriteLine(ok ? $"[REGISTERED] {msg}" : $"[ERROR] {msg}");
    registered.TrySetResult(ok);
});

connection.On<string>("Notification", m => Console.WriteLine($"[INFO] {m}"));

try
{
    await connection.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Failed to connect: {ex.Message}");
    return;
}

await connection.InvokeAsync("InitSinglePerson", username, city, age, phone);

if (!await registered.Task)
{
    await connection.DisposeAsync();
    return;
}

Console.WriteLine();
Console.WriteLine("Commands:");
Console.WriteLine("  <Enter> or 'ok'     confirm receipt of current letter");
Console.WriteLine("  /block <username>   block that user");
Console.WriteLine("  exit                quit the client");
Console.WriteLine();

while (true)
{
    string? input = Console.ReadLine();
    if (input is null) continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    if (input.StartsWith("/block ", StringComparison.OrdinalIgnoreCase))
    {
        string target = input.Substring(7).Trim();
        if (target.Length > 0)
        {
            await connection.InvokeAsync("BlockUser", target);
        }
    }
    else
    {
        await connection.InvokeAsync("ConfirmReceipt");
        Console.WriteLine("[OK] Ready for next letter.");
    }
}

await connection.DisposeAsync();


static void DisplayLetter(LetterDto l)
{
    Console.WriteLine();
    Console.WriteLine("--- NEW LETTER ---");
    Console.WriteLine($"From:    {l.FromUsername}");
    Console.WriteLine($"City:    {l.FromCity}");
    Console.WriteLine($"Age:     {l.FromAge}");
    if (!string.IsNullOrEmpty(l.FromPhoneNumber))
    {
        Console.WriteLine($"Phone:   {l.FromPhoneNumber}");
    }
    Console.WriteLine($"Message: \"{l.Message}\"");
    Console.WriteLine("------------------");
    Console.WriteLine("Press <Enter> (or type 'ok') to confirm so the next letter can arrive.");
    Console.WriteLine();
}

static (bool ok, string? err) ValidateUsername(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return (false, "Cannot be empty.");
    if (s.Any(char.IsDigit)) return (false, "Cannot contain digits.");
    return (true, null);
}

static (bool ok, string? err) ValidateNonEmpty(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return (false, "Cannot be empty.");
    return (true, null);
}

static (bool ok, string? err) ValidatePhone(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return (false, "Cannot be empty.");
    s = s.Trim();
    if (s.StartsWith('-')) return (false, "Phone cannot be negative.");
    string digits = s.TrimStart('+');
    if (!digits.All(char.IsDigit))
        return (false, "Only digits allowed (optional leading +).");
    return (true, null);
}

static string AskText(string prompt, Func<string, (bool ok, string? err)> validate)
{
    while (true)
    {
        Console.Write(prompt);
        string s = Console.ReadLine() ?? string.Empty;
        var (ok, err) = validate(s);
        if (ok) return s.Trim();
        Console.WriteLine($"  ! {err}");
    }
}

static int AskInt(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s)) { Console.WriteLine("  ! Cannot be empty."); continue; }
        if (!int.TryParse(s, out int n))   { Console.WriteLine("  ! Must be a number, not characters."); continue; }
        if (n < 0)                          { Console.WriteLine("  ! Cannot be negative."); continue; }
        if (n == 0)                         { Console.WriteLine("  ! Must be greater than 0."); continue; }
        return n;
    }
}
