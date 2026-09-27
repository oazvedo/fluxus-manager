namespace FluxusManager.UnitTests.TestSupport;

/// <summary>Relógio controlado pelo teste: começa em "agora" e só anda com <see cref="Avancar"/>.</summary>
public sealed class Relogio : TimeProvider
{
    private DateTimeOffset _agora = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _agora;

    public void Avancar(TimeSpan tempo) => _agora += tempo;
}
