namespace GitHubStars.Backend

open DotNet.Testcontainers.Containers.Builders
open DotNet.Testcontainers.Containers.Configurations.Databases
open DotNet.Testcontainers.Containers.Modules.Databases
open Expecto
open System.Runtime.InteropServices
open System.Text
open FSharpPlus.Data
open Giraffe
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open System.IO
open Microsoft.Extensions.DependencyInjection
open System
open System.Linq

module Helpers =
    let useDatabase fn =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            printfn "Operating system not supported. Skipping database tests..."
        else
            let containerBuilder =
                TestcontainersBuilder<PostgreSqlTestcontainer>()
                    .WithDatabase (PostgreSqlTestcontainerConfiguration
                        (Database = "postgres",
                         Username = "postgres",
                         Password = "postgres"))
                    
            let testContainer = containerBuilder.Build ()
            
            try
                testContainer.StartAsync().Wait ()
                
                testContainer.ConnectionString
                |> fn
            finally
                try
                    testContainer.StopAsync().Wait ()
                    testContainer.CleanUpAsync().Wait ()
                with _ -> ()

    let useApi requestBuilder = 
        useDatabase (fun connectionString ->
            use connection = new Npgsql.NpgsqlConnection (connectionString)
                
            connection
            |> Database.createTables
            |> ResultOrException.Result
            |> ignore
            
            let apiRouter = Api.apiRouter connectionString
            
            requestBuilder (fun method path (body: string) -> async {
                let context = DefaultHttpContext ()
                context.RequestServices <- ServiceCollection().AddGiraffe().BuildServiceProvider ()
                context.Response.Body <- new MemoryStream ()
                context.Request.Body <- new MemoryStream (Encoding.UTF8.GetBytes body)
                
                context.Request.Method <- method
                context.Request.Path <- PathString path
                context.Request.QueryString <- QueryString (String.Concat (path.SkipWhile ((<>) '?')))
                
                let! result = apiRouter (Some >> Task.FromResult) context |> Async.AwaitTask
                
                match result with
                | None -> return failwith "Error processing request"
                | Some ctx ->
                    ctx.Response.Body.Position <- 0L
                    use reader = new StreamReader (ctx.Response.Body, Encoding.UTF8)
                    let body = reader.ReadToEnd ()
                    
                    return ctx.Response.StatusCode, body
            })
            |> Async.RunSynchronously
        )
