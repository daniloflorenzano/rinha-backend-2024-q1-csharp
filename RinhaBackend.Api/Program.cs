using Npgsql;
using RinhaBackend.Api.Clientes;
using RinhaBackend.Api.Transacoes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(
    Environment.GetEnvironmentVariable(
        "DB_CONNECTION_STRING") ??
    "ERRO de connection string!!!");

var app = builder.Build();

app.MapPost("/clientes/{id}/transacoes", async (int id, HttpRequest request, NpgsqlConnection conn) =>
{
    try
    {
        var transacao = await request.ReadFromJsonAsync<Transacao>();
        if (transacao == null)
            return Results.BadRequest();

        await using var cmd = new NpgsqlCommand("SELECT id, limite, saldo FROM clientes WHERE id = $1", conn)
        {
            Parameters = { new() { Value = 1 } }
        };

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Results.NotFound();

        var cliente = new Cliente(
            reader.GetInt32(0),
            reader.GetInt64(1),
            reader.GetInt64(2)
        );

        cliente.ExecutaTransacao(transacao);
        
        await using var cmd2 = new NpgsqlCommand("UPDATE clientes SET saldo = $1 WHERE id = $2", conn)
        {
            Parameters =
            {
                new() { Value = cliente.Saldo },
                new() { Value = cliente.Id }
            }
        };
        
        await cmd2.ExecuteNonQueryAsync();
        
        var clienteDto = new
        {
            cliente.Limite, cliente.Saldo
        };

        return Results.Ok(clienteDto);
    }
    catch (TransacaoInvalidaException e)
    {
        return Results.UnprocessableEntity(e.Message);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
});


app.MapGet("/clientes/{id}/extrato", (int id) =>
{
});

app.Run();