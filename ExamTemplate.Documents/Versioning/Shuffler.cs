namespace ExamTemplate.Documents.Versioning;

public static class Shuffler
{
    private const int MaxAttempts = 8;

    /// <summary>A random order of <c>0..count-1</c>; with <paramref name="avoidIdentity"/> it never returns the original order (for 2+ items).</summary>
    public static int[] Permutation(int count, ShuffleRandom random, bool avoidIdentity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var order = new int[count];

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            FisherYates(order, random);
            if (!avoidIdentity || count < 2 || !IsIdentity(order))
                return order;
        }

        // Every draw repeated the original order (only plausible for two or three items): rotate by one instead.
        for (var index = 0; index < count; index++)
            order[index] = (index + 1) % count;
        return order;
    }

    private static void FisherYates(int[] order, ShuffleRandom random)
    {
        for (var index = 0; index < order.Length; index++)
            order[index] = index;

        for (var index = order.Length - 1; index > 0; index--)
        {
            var swap = random.NextInt(index + 1);
            (order[index], order[swap]) = (order[swap], order[index]);
        }
    }

    private static bool IsIdentity(int[] order)
    {
        for (var index = 0; index < order.Length; index++)
        {
            if (order[index] != index)
                return false;
        }

        return true;
    }
}
