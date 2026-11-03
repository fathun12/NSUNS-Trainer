using NsunsTrainer.Native;

namespace NsunsTrainer.Cave;

/// <summary>
/// Side tree: every character has a node with three pointers at its start
/// (+0x00 left, +0x08 parent, +0x10 right). Characters on one side link to each other both ways.
/// Node start = value of the base register minus 0x28.
/// </summary>
public static class SideTree
{
    public const ulong BaseFromNode = 0x28;
    public const int MaxNodes = 12;
    private const int NodeBytes = 0x18;

    private static bool Plausible(ulong p) => p >= 0x10000 && p <= 0x7FFFFFFFFFFF && (p & 7) == 0;

    private static ulong[]? ReadLinks(ProcessMemory mem, ulong node)
    {
        var b = mem.Read(node, NodeBytes);
        if (b is null) return null;
        return new[] { BitConverter.ToUInt64(b, 0), BitConverter.ToUInt64(b, 8), BitConverter.ToUInt64(b, 16) };
    }

    /// <summary>Collect nodes linked both ways from the start node. The start node is always first.</summary>
    public static List<ulong>? Collect(ProcessMemory mem, ulong start, int max)
    {
        var startLinks = ReadLinks(mem, start);
        if (startLinks is null) return null;

        var seen = new HashSet<ulong> { start };
        var order = new List<ulong> { start };
        var cache = new Dictionary<ulong, ulong[]> { [start] = startLinks };
        var queue = new Queue<ulong>();
        queue.Enqueue(start);

        while (queue.Count > 0 && order.Count < max)
        {
            ulong cur = queue.Dequeue();
            foreach (ulong p in cache[cur])
            {
                if (!Plausible(p) || seen.Contains(p)) continue;

                var links = ReadLinks(mem, p);
                if (links is null || Array.IndexOf(links, cur) < 0) continue;

                seen.Add(p);
                order.Add(p);
                cache[p] = links;
                queue.Enqueue(p);
                if (order.Count >= max) break;
            }
        }
        return order;
    }
}