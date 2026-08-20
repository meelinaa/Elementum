using Elementum.Application.Inbound.UseCases.Metals;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Moq;

namespace Elementum.UnitTests.Application.UseCases;

public class GetMetalsUseCaseTests
{
    private readonly Mock<IPriceHistoryReadRepository> _repositoryMock = new();
    private readonly GetMetalsUseCase _useCase;

    public GetMetalsUseCaseTests()
    {
        _useCase = new GetMetalsUseCase(_repositoryMock.Object);
    }

    // [R]IGHT-BICEP: Verifies that GetAllAsync retrieves all seeded metals and maps them to DTOs
    [Fact]
    public async Task GetAllAsync_ReturnsAllMappedMetals()
    {
        // Arrange
        var metals = new List<Metals>
        {
            new() { Id = 1, Symbol = "XAU", Name = "Gold" },
            new() { Id = 2, Symbol = "XAG", Name = "Silver" }
        };

        _repositoryMock.Setup(r => r.GetMetalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(metals);

        // Act
        var result = (await _useCase.GetAllAsync(CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("XAU", result[0].Symbol);
        Assert.Equal("Gold", result[0].Name);
        Assert.Equal("XAG", result[1].Symbol);
        Assert.Equal("Silver", result[1].Name);
    }

    // [R]IGHT-BICEP: Verifies that GetByIdAsync returns the matching metal DTO when ID exists
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMetalDto()
    {
        // Arrange
        var metal = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };
        _repositoryMock.Setup(r => r.GetMetalById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metal);

        // Act
        var result = await _useCase.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("XAU", result.Symbol);
        Assert.Equal("Gold", result.Name);
    }

    // [B]OUNDARY / [E]RROR: Verifies that GetByIdAsync returns null when ID does not exist
    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetMetalById(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Metals?)null);

        // Act
        var result = await _useCase.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    // [R]IGHT-BICEP: Verifies that GetBySymbolAsync returns the matching metal DTO when symbol exists
    [Fact]
    public async Task GetBySymbolAsync_WhenFound_ReturnsMetalDto()
    {
        // Arrange
        var metal = new Metals { Id = 2, Symbol = "XAG", Name = "Silver" };
        _repositoryMock.Setup(r => r.GetMetalBySymbol("XAG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metal);

        // Act
        var result = await _useCase.GetBySymbolAsync("XAG", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("XAG", result.Symbol);
        Assert.Equal("Silver", result.Name);
    }

    // [B]OUNDARY / [E]RROR: Verifies that GetBySymbolAsync returns null when symbol does not exist
    [Fact]
    public async Task GetBySymbolAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetMetalBySymbol("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Metals?)null);

        // Act
        var result = await _useCase.GetBySymbolAsync("UNKNOWN", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
