using System.Security.Cryptography;

namespace ChaoticCupid.Server.Utilities;

public static class CryptoRandom
{
    // Random int in [minInclusive, maxExclusive) using RNGCryptoServiceProvider
    // as required by the specification.
    public static int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));

#pragma warning disable SYSLIB0023
        using var rng = new RNGCryptoServiceProvider();
#pragma warning restore SYSLIB0023

        var buf = new byte[4];
        rng.GetBytes(buf);
        uint v = BitConverter.ToUInt32(buf, 0);
        long range = (long)maxExclusive - minInclusive;
        return (int)(minInclusive + (v % range));
    }
}
