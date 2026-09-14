using ExamTemplate.Documents.Versioning;

namespace ExamTemplate.Tests.Versioning;

public class SeedDerivationTests
{
    private static readonly Guid SectionOne = new("00000000-0000-0001-0000-000000000000");
    private static readonly Guid SectionTwo = new("00000000-0000-0002-0000-000000000000");

    [Fact]
    public void Same_inputs_give_the_same_seed() =>
        Assert.Equal(
            SeedDerivation.Derive(12345, 1, SeedPurpose.QuestionOrder, SectionOne),
            SeedDerivation.Derive(12345, 1, SeedPurpose.QuestionOrder, SectionOne));

    [Fact]
    public void Every_input_changes_the_derived_seed()
    {
        var baseline = SeedDerivation.Derive(12345, 1, SeedPurpose.QuestionOrder, SectionOne);

        Assert.NotEqual(baseline, SeedDerivation.Derive(12346, 1, SeedPurpose.QuestionOrder, SectionOne));
        Assert.NotEqual(baseline, SeedDerivation.Derive(12345, 2, SeedPurpose.QuestionOrder, SectionOne));
        Assert.NotEqual(baseline, SeedDerivation.Derive(12345, 1, SeedPurpose.OptionOrder, SectionOne));
        Assert.NotEqual(baseline, SeedDerivation.Derive(12345, 1, SeedPurpose.QuestionOrder, SectionTwo));
    }
}
