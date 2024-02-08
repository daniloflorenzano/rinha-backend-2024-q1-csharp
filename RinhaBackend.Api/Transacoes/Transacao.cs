namespace RinhaBackend.Api.Transacoes;

public sealed class Transacao
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public long Valor { get; set; }
    public string Descricao { get; set; }
    public string Tipo { get; set; }
    public DateTime RealizadaEm { get; set; } = DateTime.Now;

    public Transacao(long valor, string descricao, string tipo)
    {
        ValidaCampos(valor, descricao, tipo);

        Valor = valor;
        Descricao = descricao;
        Tipo = tipo;
    }

    private void ValidaCampos(long valor, string descricao, string tipo)
    {
        if (valor < 0)
            throw new ArgumentException("Valor inválido", nameof(valor));

        if (string.IsNullOrWhiteSpace(tipo) || tipo != "c" && tipo != "d")
            throw new ArgumentException("Tipo inválido", nameof(tipo));

        if (string.IsNullOrWhiteSpace(descricao) || descricao.Length > 10 || descricao.Length < 1)
            throw new ArgumentException("Descrição inválida", nameof(descricao));
    }
}