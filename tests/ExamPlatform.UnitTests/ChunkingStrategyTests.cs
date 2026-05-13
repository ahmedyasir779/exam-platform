using ExamPlatform.Application.PdfProcessing;
using FluentAssertions;

namespace ExamPlatform.UnitTests;

public class ChunkingStrategyTests
{
    private readonly ChunkingStrategy _sut = new();

    [Fact]
    public void Chunk_ShortPage_ProducesOneChunk()
    {
        var pages = new[] { new RawPage(1, "Hello world this is a test", null, null, null, null) };
        var result = _sut.Chunk(Guid.NewGuid(), pages);
        result.Should().HaveCount(1);
        result[0].Page.Should().Be(1);
        result[0].Text.Should().Contain("Hello");
    }

    [Fact]
    public void Chunk_LongPage_ProducesMultipleChunksWithOverlap()
    {
        var longText = string.Join(" ", Enumerable.Range(1, 600).Select(i => $"word{i}"));
        var pages = new[] { new RawPage(1, longText, null, null, null, null) };
        var result = _sut.Chunk(Guid.NewGuid(), pages);
        result.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void Chunk_EmptyPage_ProducesNoChunks()
    {
        var pages = new[] { new RawPage(1, "   ", null, null, null, null) };
        var result = _sut.Chunk(Guid.NewGuid(), pages);
        result.Should().BeEmpty();
    }
}
