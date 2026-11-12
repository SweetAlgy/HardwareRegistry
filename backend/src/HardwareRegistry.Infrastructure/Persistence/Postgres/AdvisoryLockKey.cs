using System.Buffers.Binary;
using System.Security.Cryptography;

namespace HardwareRegistry.Infrastructure.Persistence.Postgres
{
    /// <summary>
    /// A 64-bit advisory lock key derived from arbitrary data. Both halves come from the hash, so
    /// unrelated subsystems can share the single advisory namespace without practical collisions.
    /// </summary>
    internal readonly record struct AdvisoryLockKey(int High, int Low)
    {
        public static AdvisoryLockKey From(ReadOnlySpan<byte> value)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(value, hash);

            return new(
                BinaryPrimitives.ReadInt32LittleEndian(hash[..4]),
                BinaryPrimitives.ReadInt32LittleEndian(hash[4..8])
            );
        }
    }
}
