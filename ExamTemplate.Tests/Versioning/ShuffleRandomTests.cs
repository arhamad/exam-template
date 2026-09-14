using ExamTemplate.Documents.Versioning;

namespace ExamTemplate.Tests.Versioning;

public class ShuffleRandomTests
{
    [Fact]
    public void Seed_zero_matches_the_splitmix64_reference()
    {
        var random = new ShuffleRandom(0);

        Assert.Equal(0xE220A8397B1DCDAFUL, random.NextUInt64());
        Assert.Equal(0x6E789E6AA1B965F4UL, random.NextUInt64());
        Assert.Equal(0x06C45D188009454FUL, random.NextUInt64());
    }

    [Fact]
    public void Seed_42_matches_the_splitmix64_reference()
    {
        var random = new ShuffleRandom(42);

        Assert.Equal(0xBDD732262FEB6E95UL, random.NextUInt64());
        Assert.Equal(0x28EFE333B266F103UL, random.NextUInt64());
        Assert.Equal(0x47526757130F9F52UL, random.NextUInt64());
    }

    [Fact]
    public void NextInt_stays_within_bounds_and_reaches_every_value()
    {
        var random = new ShuffleRandom(2026);
        var seen = new HashSet<int>();

        for (var draw = 0; draw < 100_000; draw++)
        {
            var value = random.NextInt(7);
            Assert.InRange(value, 0, 6);
            seen.Add(value);
        }

        Assert.Equal(7, seen.Count);
    }
}
