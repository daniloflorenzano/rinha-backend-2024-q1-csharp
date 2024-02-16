using System.Data;
using Npgsql;
using RinhaBackend.Api;
using RinhaBackend.Api.Clientes;
using RinhaBackend.Api.Transacoes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(
    Environment.GetEnvironmentVariable(
        "DB_CONNECTION_STRING") ??
    "ERRO de connection string!!!");

var app = builder.Build();

app.MapPost("/clientes/{id}/transacoes", async (int id, HttpRequest request, NpgsqlDataSource dataSource) =>
{
    var transacao = await request.ReadFromJsonAsync<Transacao>();
    if (transacao == null)
    {
        return Results.BadRequest();
    }
    
    NpgsqlConnection? conn = null;
    NpgsqlTransaction? dbTransaction = null;

    try
    {
        Cliente cliente;
        
        conn = await dataSource.OpenConnectionAsync();
        await using (dbTransaction = conn.BeginTransaction(IsolationLevel.RepeatableRead))

        await using (var cmd = new NpgsqlCommand("SELECT id, limite, saldo FROM clientes WHERE id = $1",
                         conn,
                         dbTransaction))
        {
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = id });
            await cmd.PrepareAsync();
            
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.NotFound();


            cliente = new Cliente(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2)
            );


            cliente.ExecutaTransacao(transacao);
        }

        await using (var cmd = new NpgsqlCommand("UPDATE clientes SET saldo = $1 WHERE id = $2", conn, dbTransaction))
        {
            cmd.Parameters.Add(new NpgsqlParameter<int> { Value = cliente.Saldo });
            cmd.Parameters.Add(new NpgsqlParameter<int> { Value = cliente.Id });
            await cmd.PrepareAsync();
            await cmd.ExecuteNonQueryAsync();
        }

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
        return Results.Problem();
    }
    finally
    {
        if (dbTransaction != null)
            await dbTransaction.DisposeAsync();

        if (conn != null)
            await conn.CloseAsync();
    }
});


app.MapGet("/clientes/{id}/extrato", (int id) =>
{
    return Results.Ok(new Extrato
    {
        Saldo = new Saldo
        {
            Total = -9098,
            DataExtrato = DateTime.Parse("2024-01-17T02:34:41.217753Z"),
            Limite = 100000
        },
        UltimasTransacoes =
        [
            new Transacao
            {
                Valor = 10,
                Tipo = "c",
                Descricao = "descricao",
                RealizadaEm = DateTime.Parse("2024-01-17T02:34:38.543030Z")
            },
            new Transacao
            {
                Valor = 90000,
                Tipo = "d",
                Descricao = "descricao",
                RealizadaEm = DateTime.Parse("2024-01-17T02:34:38.543030Z")
            }
        ]
    });
});

app.Run();