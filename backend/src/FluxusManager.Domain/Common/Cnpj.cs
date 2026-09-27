namespace FluxusManager.Domain.Common;

/// <summary>
/// Regras do CNPJ, inclusive o alfanumérico (Receita Federal, a partir de julho de 2026):
/// 12 caracteres [0-9A-Z] seguidos de 2 dígitos verificadores numéricos.
/// </summary>
public static class Cnpj
{
    public const int Length = 14;

    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Remove pontuação e espaços e passa para maiúsculas: "12.abc.345/01de-35" → "12ABC34501DE35".</summary>
    public static string Normalizar(string cnpj)
        => string.Concat(cnpj.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    /// <summary>Confere formato e dígitos verificadores de um CNPJ já normalizado.</summary>
    public static bool EhValido(string cnpj)
    {
        if (cnpj.Length != Length)
            return false;

        var raiz = cnpj[..12];
        if (!raiz.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c))
            || !cnpj[12..].All(char.IsAsciiDigit)
            || cnpj.All(c => c == cnpj[0]))
            return false;

        var first = DigitoVerificador(raiz, FirstWeights);
        var second = DigitoVerificador(raiz + first, SecondWeights);

        return cnpj[12] == first && cnpj[13] == second;
    }

    /// <summary>"11222333000181" → "11.222.333/0001-81".</summary>
    public static string Formatar(string cnpj)
        => cnpj.Length == Length ? $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}" : cnpj;

    // Valor de cada caractere = código ASCII - 48 (dígitos 0-9, letras A=17 ... Z=42).
    private static char DigitoVerificador(string valor, int[] pesos)
    {
        var resto = valor.Select((c, i) => (c - '0') * pesos[i]).Sum() % 11;
        return resto < 2 ? '0' : (char)('0' + 11 - resto);
    }
}
