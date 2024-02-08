using RinhaBackend.Api.Transacoes;

namespace RinhaBackend.Api;

public sealed class Extrato
{
    public Saldo Saldo { get; set; }
    public List<Transacao> UltimasTransacoes { get; set; }
}

public sealed class Saldo
{
    public long Total { get; set; }
    public DateTime DataExtrato { get; set; } = DateTime.Now;
    public long Limite { get; set; }
}