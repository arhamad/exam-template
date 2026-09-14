using ExamTemplate.Documents.Versioning;

namespace ExamTemplate.Tests.Versioning;

public class ShufflerTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(50)]
    public void Permutation_contains_every_index_exactly_once(int count)
    {
        var permutation = Shuffler.Permutation(count, new ShuffleRandom(7), avoidIdentity: false);

        Assert.Equal(Enumerable.Range(0, count), permutation.Order());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Avoiding_identity_never_returns_the_original_order(int count)
    {
        for (ulong seed = 0; seed < 1000; seed++)
            Assert.NotEqual(Enumerable.Range(0, count), Shuffler.Permutation(count, new ShuffleRandom(seed), avoidIdentity: true));
    }

    [Fact]
    public void Single_item_stays_in_place_even_when_avoiding_identity() =>
        Assert.Equal(new[] { 0 }, Shuffler.Permutation(1, new ShuffleRandom(1), avoidIdentity: true));

    [Fact]
    public void Same_seed_gives_the_same_permutation() =>
        Assert.Equal(
            Shuffler.Permutation(20, new ShuffleRandom(99), avoidIdentity: true),
            Shuffler.Permutation(20, new ShuffleRandom(99), avoidIdentity: true));
}
