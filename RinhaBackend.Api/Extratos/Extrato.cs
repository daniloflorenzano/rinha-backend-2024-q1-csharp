using RinhaBackend.Api.Transacoes;

namespace RinhaBackend.Api.Extratos;

public sealed class Extrato
{
    public Saldo saldo { get; set; }
    public List<Transacao> ultimas_transacoes { get; set; }
}

public sealed class Saldo
{
    public long Total { get; set; }
    public DateTime Data_Extrato { get; set; } = DateTime.Now;
    public long Limite { get; set; }
}