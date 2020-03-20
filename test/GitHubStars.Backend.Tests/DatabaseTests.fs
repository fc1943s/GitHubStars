namespace GitHubStars.Backend

open FSharpPlus.Data
open Expecto
open Expecto.Flip
open GitHubStars.Shared.Model

module DatabaseTests =
        
    let tests = testList "Database" [
        test "Tests" {
            Helpers.useDatabase (fun connectionString ->
                use connection = new Npgsql.NpgsqlConnection (connectionString)
                    
                connection
                |> Database.createTables
                |> ResultOrException.Result
                |> ignore
                    
                    
                connection
                |> Database.addTag "0000" "id:react" "js"
                |> Core.resultOrException
                
                connection
                |> Database.addTag "0000" "id:react" "ui"
                |> Core.resultOrException
                
                
                connection
                |> Database.addTag "fc1943s" "id:docker" "devops"
                |> Core.resultOrException
                
                connection
                |> Database.addTag "fc1943s" "id:react" "javascript"
                |> Core.resultOrException
                
                connection
                |> Database.addTag "fc1943s" "id:react" "frontend"
                |> Core.resultOrException
                
                connection
                |> Database.getRepositoriesWithTag "fc1943s" None
                |> function Ok x -> x | _ -> []
                |> List.map (fun x -> x.Tag)
                |> Expect.equal "" [ "devops"; "javascript"; "frontend" ]
                
                connection
                |> Database.getRepositoriesWithTag "fc1943s" (Some "e")
                |> function Ok x -> x | _ -> []
                |> List.map (fun x -> x.Tag)
                |> Expect.equal "" [ "devops"; "javascript"; "frontend" ]
                
                connection
                |> Database.addTag "fc1943s" "id:react" "javascript"
                |> function Error x -> x | _ -> ""
                |> Expect.equal "" "Tag already exists"
                
                connection
                |> Database.setTags "fc1943s" "id:react" [ "frontend"; "ui"; "frontend" ]
                |> Core.resultOrException
                |> Expect.equal "" [ 2; 1; 1 ]
                
                connection
                |> Database.getRepositoriesWithTag "fc1943s" None
                |> function Ok x -> x | _ -> []
                |> List.map (fun x -> x.Tag)
                |> Expect.equal "" [ "devops"; "frontend"; "ui" ]
                
                connection
                |> Database.deleteTag "fc1943s" "id:react" "frontend"
                |> Core.resultOrException
                |> Expect.equal "" 1
                
                connection
                |> Database.getRepositoriesWithTag "fc1943s" None
                |> function Ok x -> x | _ -> []
                |> List.map (fun x -> x.Tag)
                |> Expect.equal "" [ "devops"; "ui" ]
                
                
                connection
                |> Database.getRepositoriesWithTag "0000" None
                |> function Ok x -> x | _ -> []
                |> List.map (fun x -> x.Tag)
                |> Expect.equal "" [ "js"; "ui" ]
            )
        }
    ]


