using FluxusManager.Domain.Common;

namespace FluxusManager.UnitTests.Empresas;

public class CnpjTests
{
    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11444777000161")]
    [InlineData("12ABC34501DE35")] // alfanumérico (a partir de julho de 2026)
    public void EhValido_ComDigitosCorretos(string cnpj)
    {
        Assert.True(Cnpj.EhValido(cnpj));
    }

    [Theory]
    [InlineData("11222333000182")] // segundo dígito errado
    [InlineData("11222333000191")] // primeiro dígito errado
    [InlineData("12ABC34501DE3A")] // dígito verificador não numérico
    [InlineData("11111111111111")] // todos iguais
    [InlineData("1122233300018")]  // 13 caracteres
    [InlineData("11.222.333/0001-81")] // não normalizado
    [InlineData("")]
    public void EhValido_Rejeita(string cnpj)
    {
        Assert.False(Cnpj.EhValido(cnpj));
    }

    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData(" 12.abc.345/01de-35 ", "12ABC34501DE35")]
    public void Normalizar_TiraPontuacaoEPassaParaMaiusculas(string entrada, string esperado)
    {
        Assert.Equal(esperado, Cnpj.Normalizar(entrada));
    }

    [Fact]
    public void Formatar_AplicaAMascara()
    {
        Assert.Equal("12.ABC.345/01DE-35", Cnpj.Formatar("12ABC34501DE35"));
    }
}
