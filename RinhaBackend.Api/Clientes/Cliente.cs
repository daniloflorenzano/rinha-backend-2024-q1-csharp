using RinhaBackend.Api.Transacoes;

namespace RinhaBackend.Api.Clientes;

public sealed class Cliente(int id, long limite, long saldo)
{
    public int Id { get; set; } = id;
    public long Limite { get; set; } = limite;
    public long Saldo { get; set; } = saldo;
    
    public void ExecutaTransacao(Transacao transacao)
    {
        if (transacao.Tipo == "c")
            Saldo -= transacao.Valor;
        
        
        else if (transacao.Tipo == "d")
        {
            var novoSaldo = Saldo - transacao.Valor;
            var saldoLimite = Limite - (Limite * 2);
            
            if (novoSaldo < saldoLimite)
                throw new TransacaoInvalidaException("Saldo insuficiente");
            
            Saldo = novoSaldo;
        }
    }
}