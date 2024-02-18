using System.Text.Json.Serialization;
using RinhaBackend.Api.Transacoes;

namespace RinhaBackend.Api;

public sealed class Extrato
{
    public Saldo Saldo { get; set; }
    
    [JsonPropertyName("ultimas_transacoes")]
    public List<Transacao> UltimasTransacoes { get; set; }
}

public sealed class Saldo
{
    public long Total { get; set; }
    
    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; set; } = DateTime.Now;
    public long Limite { get; set; }
}