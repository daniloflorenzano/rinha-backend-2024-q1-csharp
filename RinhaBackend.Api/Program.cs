using System.Data;
using System.Text.Json;
using Npgsql;
using RinhaBackend.Api;
using RinhaBackend.Api.Clientes;
using RinhaBackend.Api.Transacoes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(
    Environment.GetEnvironmentVariable(
        "DB_CONNECTION_STRING") ??
    "Username=postgres;Password=mysecretpassword;Host=localhost;Database=rinha;Pooling=true;MaxPoolSize=15;Connection Lifetime=0;");

// builder.Services
//     .AddNpgsqlDataSource(
//         "Username=postgres;Password=mysecretpassword;Host=localhost;Database=rinha;Pooling=true;MaxPoolSize=15;Connection Lifetime=0;");


var app = builder.Build();

app.MapPost("/clientes/{id}/transacoes", async (int id, HttpRequest request, NpgsqlDataSource dataSource) =>
{
    try
    {
        var transacao = await request.ReadFromJsonAsync<Transacao>();
        if (transacao == null)
            return Results.BadRequest();
        transacao.ValidaCampos();

        ResultadoRequisicao<Cliente> resultado;
        do
        {
            resultado = await ExecutaTransacao(dataSource, transacao, id);
            // if (!resultado.Sucesso)
            // {
            //     var random = new Random().Next(1, 5);
            //     Thread.Sleep(TimeSpan.FromMilliseconds(random));
            // }
        } while (!resultado.Sucesso);

        var cliente = resultado.Retorno;
        if (cliente == null)
            return Results.NotFound();

        var clienteDto = new
        {
            cliente.Limite, cliente.Saldo
        };

        return Results.Ok(clienteDto);
    }
    catch (JsonException)
    {
        return Results.UnprocessableEntity();
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
});

async Task<ResultadoRequisicao<Cliente>> ExecutaTransacao(NpgsqlDataSource dataSource, Transacao transacao,
    int clienteId)
{
    NpgsqlConnection? conn = null;

    try
    {
        Cliente cliente;

        conn = await dataSource.OpenConnectionAsync();

        await using (var dbTransaction = conn.BeginTransaction(IsolationLevel.RepeatableRead))
        {
            await using (var cmd = new NpgsqlCommand("SELECT limite, saldo FROM clientes WHERE id = $1",
                             conn,
                             dbTransaction))
            {
                cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = clienteId });
                await cmd.PrepareAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return new ResultadoRequisicao<Cliente> { Sucesso = true };

                cliente = new Cliente(
                    reader.GetInt32(0),
                    reader.GetInt32(1)
                );

                cliente.ExecutaTransacao(transacao);
            }

            await using (var cmd = new NpgsqlCommand("""
                                                     INSERT INTO transacoes (cliente_id, valor, descricao, tipo, realizada_em)
                                                     VALUES ($1, $2, $3, $4, $5)
                                                     """, conn, dbTransaction))
            {
                cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = clienteId });
                cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = transacao.Valor });
                cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = transacao.Descricao });
                cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = transacao.Tipo });
                cmd.Parameters.Add(new NpgsqlParameter<DateTime> { TypedValue = transacao.RealizadaEm });

                await cmd.PrepareAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new NpgsqlCommand("UPDATE clientes SET saldo = $1 WHERE id = $2", conn,
                             dbTransaction))
            {
                cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = cliente.Saldo });
                cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = clienteId });

                await cmd.PrepareAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await dbTransaction.CommitAsync();
        }

        await conn.CloseAsync();

        return new ResultadoRequisicao<Cliente> { Sucesso = true, Retorno = cliente };
    }
    catch (NpgsqlException)
    {
        // caso a transação tenha falhado por já haver uma outra executando com os mesmos dados
        return new ResultadoRequisicao<Cliente> { Sucesso = false };
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
    finally
    {
        if (conn != null)
            await conn.CloseAsync();
    }
}


app.MapGet("/clientes/{id}/extrato", async (int id, NpgsqlDataSource dataSource) =>
{
    try
    {
        var resultado = await ObtemExtrato(dataSource, id);

        if (!resultado.Sucesso)
            return Results.Problem();

        var extrato = resultado.Retorno;

        if (extrato == null)
            return Results.NotFound();

        return Results.Ok(extrato);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
});

async Task<ResultadoRequisicao<Extrato>> ObtemExtrato(NpgsqlDataSource dataSource, int clienteId)
{
    NpgsqlConnection? conn = null;

    try
    {
        Extrato extrato;

        conn = await dataSource.OpenConnectionAsync();
        //await using (var dbTransaction = conn.BeginTransaction(IsolationLevel.RepeatableRead))
        await using (var cmd = new NpgsqlCommand("SELECT saldo, limite FROM clientes WHERE id = $1", conn))
        {
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = clienteId });
            await cmd.PrepareAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return new ResultadoRequisicao<Extrato>() { Sucesso = true };

            extrato = new()
            {
                Saldo = new()
                {
                    Total = reader.GetInt32(0),
                    Limite = reader.GetInt32(1),
                    DataExtrato = DateTime.Now
                }
            };
        }

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT valor, descricao, tipo, realizada_em FROM transacoes WHERE cliente_id = $1
                            ORDER BY realizada_em DESC LIMIT 10
                         """, conn))
        {
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = clienteId });
            await cmd.PrepareAsync();

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                extrato.UltimasTransacoes = new List<Transacao>();
                return new ResultadoRequisicao<Extrato>() { Sucesso = true, Retorno = extrato };
            }

            var transacoes = new List<Transacao>();

            do
            {
                transacoes.Add(new()
                {
                    Valor = reader.GetInt32(0),
                    Descricao = reader.GetString(1),
                    Tipo = reader.GetString(2),
                    RealizadaEm = reader.GetDateTime(3)
                });
            } while (await reader.ReadAsync());

            extrato.UltimasTransacoes = transacoes;
        }

        await conn.CloseAsync();

        return new ResultadoRequisicao<Extrato>() { Sucesso = true, Retorno = extrato };
    }
    catch (NpgsqlException)
    {
        return new ResultadoRequisicao<Extrato>() { Sucesso = false };
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
    finally
    {
        if (conn != null)
            await conn.CloseAsync();
    }
}

app.Run();