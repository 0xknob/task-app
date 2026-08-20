// Tasks.Domain.Tests/TaskAggregate/TitleTests.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.Tests.TaskAggregate;

public class TitleTests
{
    [Fact]
    public void Create_ComTextoValido_RetornaSucesso()
    {
        var result = Title.Create("Comprar pão");

        Assert.True(result.IsSuccess);
        Assert.Equal("Comprar pão", result.Value!.Value);
    }

    [Fact]
    public void Create_FazTrimDoTexto()
    {
        var result = Title.Create("  Comprar pão  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Comprar pão", result.Value!.Value);
    }

    [Fact]
    public void Create_ComTextoVazio_RetornaFalha()
    {
        var result = Title.Create(string.Empty);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Create_ComEspacosEmBranco_RetornaFalha()
    {
        var result = Title.Create("   ");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_ComNull_RetornaFalha()
    {
        var result = Title.Create(null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_Com201Caracteres_RetornaFalha()
    {
        var textoGigante = new string('a', Title.MaxLength + 1);

        var result = Title.Create(textoGigante);

        Assert.True(result.IsFailure);
        Assert.Contains($"{Title.MaxLength}", result.Error);
    }

    [Fact]
    public void Create_Com200Caracteres_RetornaSucesso()
    {
        var textoLimite = new string('a', Title.MaxLength);

        var result = Title.Create(textoLimite);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Title_EhValueObject_IgualPorValor()
    {
        var a = Title.Create("Mesmo título").Value!;
        var b = Title.Create("Mesmo título").Value!;

        Assert.Equal(a, b);
    }

    [Fact]
    public void Title_TitulosDiferentes_NaoSaoIguais()
    {
        var a = Title.Create("A").Value!;
        var b = Title.Create("B").Value!;

        Assert.NotEqual(a, b);
    }
}
