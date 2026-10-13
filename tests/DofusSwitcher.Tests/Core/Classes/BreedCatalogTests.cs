using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class BreedCatalogTests
{
    private const string Json = """
        {"total":2,"data":[
          {"id":1,"shortName":{"id":"685","de":"Féca","en":"Feca","fr":"Féca"},
           "img":"https://api.dofusdb.fr/img/breeds/symbol_1.png"},
          {"id":5,"shortName":{"id":"700","fr":"Xélor","en":"Xelor"},
           "img":"https://api.dofusdb.fr/img/breeds/symbol_5.png"},
          {"id":99,"shortName":{"fr":"SansImage"}}
        ]}
        """;

    [Fact]
    public void Maps_class_names_to_symbol_urls()
    {
        var urls = BreedCatalog.ParseSymbolUrls(Json);

        Assert.Equal("https://api.dofusdb.fr/img/breeds/symbol_1.png", urls["feca"]);
        Assert.Equal("https://api.dofusdb.fr/img/breeds/symbol_5.png", urls[ClassStyle.ToFileKey("Xélor")]);
        Assert.False(urls.ContainsKey("sansimage"));
        Assert.False(urls.ContainsKey("685"));
    }

    [Fact]
    public void Unexpected_shape_gives_empty_map()
    {
        Assert.Empty(BreedCatalog.ParseSymbolUrls("""{"message":"error"}"""));
    }
}
