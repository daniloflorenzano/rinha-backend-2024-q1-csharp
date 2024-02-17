namespace RinhaBackend.Api;

public class ResultadoRequisicao<T>
{
    public bool Sucesso { get; set; }
    public T? Retorno { get; set; }
}