using FluxusManager.Infrastructure.Security;

namespace FluxusManager.UnitTests.Usuarios;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_NaoGuardaASenhaEmTexto()
    {
        var hash = _hasher.Hash("segredo123");

        Assert.DoesNotContain("segredo123", hash);
    }

    [Fact]
    public void Hash_DaMesmaSenha_GeraValoresDiferentes()
    {
        Assert.NotEqual(_hasher.Hash("segredo123"), _hasher.Hash("segredo123"));
    }

    [Fact]
    public void Verificar_ComASenhaCorreta_RetornaTrue()
    {
        var hash = _hasher.Hash("segredo123");

        Assert.True(_hasher.Verificar("segredo123", hash));
    }

    [Fact]
    public void Verificar_ComSenhaErrada_RetornaFalse()
    {
        var hash = _hasher.Hash("segredo123");

        Assert.False(_hasher.Verificar("outra123", hash));
    }
}
