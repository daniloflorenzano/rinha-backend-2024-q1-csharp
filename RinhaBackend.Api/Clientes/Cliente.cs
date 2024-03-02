using RinhaBackend.Api.Transacoes;

namespace RinhaBackend.Api.Clientes;

public sealed class Cliente(int limite, int saldo)
{
    public int id { get; set; }
    public int limite { get; set; } = limite;
    public int saldo { get; set; } = saldo;
    private int SaldoLimite => limite * -1;

    public void ExecutaTransacao(Transacao transacao)
    {
        if (transacao.Valor <= 0)
            throw new TransacaoInvalidaException("Valor inválido");


        switch (transacao.Tipo)
        {
            case 'c':
                saldo += transacao.Valor;
                break;

            case 'd':
            {
                var novoSaldo = saldo - transacao.Valor;

                if (novoSaldo > SaldoLimite)
                    saldo = novoSaldo;

                break;
            }

            default:
                throw new TransacaoInvalidaException("Limite insuficiente");
        }
    }
}