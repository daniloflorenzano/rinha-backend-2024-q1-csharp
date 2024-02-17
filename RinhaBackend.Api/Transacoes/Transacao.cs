using RinhaBackend.Api.Clientes;

namespace RinhaBackend.Api.Transacoes;

public sealed class Transacao
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int Valor { get; set; }
    public string Descricao { get; set; }
    public string Tipo { get; set; }
    public DateTime RealizadaEm { get; set; } = DateTime.Now;

    public Transacao()
    {
    }
    
    public void ValidaCampos()
    {
        if (Valor < 0)
            throw new TransacaoInvalidaException("Valor inválido");

        if (string.IsNullOrWhiteSpace(Tipo) || Tipo != "c" && Tipo != "d")
            throw new TransacaoInvalidaException("Tipo inválido");

        if (string.IsNullOrWhiteSpace(Descricao) || Descricao.Length > 10 || Descricao.Length < 1)
            throw new TransacaoInvalidaException("Descrição inválida");
    }
}