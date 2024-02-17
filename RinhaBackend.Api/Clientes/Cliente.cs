using RinhaBackend.Api.Transacoes;

namespace RinhaBackend.Api.Clientes;

public sealed class Cliente(int limite, int saldo)
{
    public int Id { get; set; }
    public int Limite { get; set; } = limite;
    public int Saldo { get; set; } = saldo;
    private int SaldoLimite => Limite * -1;

    public void ExecutaTransacao(Transacao transacao)
    {
        if (transacao.Valor <= 0)
            throw new TransacaoInvalidaException("Valor inválido");


        switch (transacao.Tipo)
        {
            case "c":
                Saldo += transacao.Valor;
                break;
            
            case "d":
            {
                var novoSaldo = Saldo - transacao.Valor;

                if (novoSaldo > SaldoLimite)
                    Saldo = novoSaldo;
                
                break;
            }
            
            default:
                throw new TransacaoInvalidaException("Limite insuficiente");
        }
    }
}