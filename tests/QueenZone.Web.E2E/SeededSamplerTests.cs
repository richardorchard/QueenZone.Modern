namespace QueenZone.Web.E2E;

[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class SeededSamplerTests
{
    [TestCase(0)]
    [TestCase(-1)]
    public void SampleFirstLastAndRandom_RejectsNonPositiveCap(int cap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SeededSampler.SampleFirstLastAndRandom([1, 2, 3], cap));
    }

    [Test]
    public void SampleFirstLastAndRandom_ReturnsAllItemsWhenTheyFit()
    {
        int[] items = [1, 2, 3];

        var sampled = SeededSampler.SampleFirstLastAndRandom(items, items.Length);

        Assert.That(sampled, Is.EqualTo(items));
    }

    [Test]
    public void SampleFirstLastAndRandom_WithCapOne_ReturnsOnlyFirstItem()
    {
        var sampled = SeededSampler.SampleFirstLastAndRandom([1, 2, 3], 1);

        Assert.That(sampled, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void SampleFirstLastAndRandom_WithCapTwo_ReturnsFirstAndLastItems()
    {
        var sampled = SeededSampler.SampleFirstLastAndRandom([1, 2, 3, 4], 2);

        Assert.That(sampled, Is.EqualTo(new[] { 1, 4 }));
    }

    [Test]
    public void SampleFirstLastAndRandom_IsRepeatableAndHonoursCap()
    {
        var items = Enumerable.Range(1, 20).ToArray();

        var first = SeededSampler.SampleFirstLastAndRandom(items, 5);
        var second = SeededSampler.SampleFirstLastAndRandom(items, 5);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Count.EqualTo(5));
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first[0], Is.EqualTo(items[0]));
            Assert.That(first[1], Is.EqualTo(items[^1]));
            Assert.That(first, Is.Unique);
        });
    }
}
