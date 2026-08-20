// Tasks.Domain.Tests/TaskAggregate/DescriptionTests.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.Tests.TaskAggregate;

public class DescriptionTests
{
    [Fact]
    public void Create_ComTextoValido_RetornaSucesso()
    {
        var result = Description.Create("Uma descrição qualquer");

        Assert.True(result.IsSuccess);
        Assert.Equal("Uma descrição qualquer", result.Value!.Value);
    }

    [Fact]
    public void Create_ComTextoVazio_RetornaSucesso()
    {
        // Descrição PODE ser vazia (campo opcional).
        var result = Description.Create(string.Empty);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value!.Value);
    }

    [Fact]
    public void Create_ComNull_RetornaSucessoVazio()
    {
        // null vira string vazia — descrição é opcional.
        var result = Description.Create(null!);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value!.Value);
    }

    [Fact]
    public void Create_Com2001Caracteres_RetornaFalha()
    {
        var textoGigante = new string('a', Description.MaxLength + 1);

        var result = Description.Create(textoGigante);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_FazTrimDoTexto()
    {
        var result = Description.Create("  descrição  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("descrição", result.Value!.Value);
    }
}
