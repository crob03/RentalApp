using RentalApp.Database.Repositories;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Repositories;

public class CategoryRepositoryTests : IClassFixture<DatabaseFixture<CategoryRepositoryTests>>
{
    private readonly DatabaseFixture<CategoryRepositoryTests> _fixture;

    public CategoryRepositoryTests(DatabaseFixture<CategoryRepositoryTests> fixture)
    {
        _fixture = fixture;
    }

    private CategoryRepository CreateSut() => new(_fixture.ContextFactory);

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        var sut = CreateSut();

        var results = await sut.GetAllAsync();

        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        var sut = CreateSut();

        var results = (await sut.GetAllAsync()).ToList();

        Assert.True(
            string.Compare(results[0].Name, results[1].Name, StringComparison.Ordinal) <= 0
        );
    }
}
