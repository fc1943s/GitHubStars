namespace GitHubStars.Backend

open Expecto
open Expecto.Flip
open GitHubStars.Shared.Model
open Newtonsoft.Json

module ApiTests =
    let tests = testList "Api" [
        test "Tests" {
            Helpers.useApi (fun request -> async {
                let! statusCode, body = request "GET" "/api/users/fc1943s/repositories" ""
                Expect.equal "" 200 statusCode
                
                do!
                    body
                    |> JsonConvert.DeserializeObject<UserRepository[]>
                    |> fun repos -> async {
                        Expect.isGreaterThan "" (repos.Length, 0)
                        
                        let repo = repos |> Array.find (fun x -> x.Repository.Name = "fsharp")
                        
                        let! statusCode, _ =
                            request
                                "POST"
                                (sprintf "/api/users/fc1943s/repositories/%s/tags" repo.Repository.Id)
                                """ "cross-platform" """
                        Expect.equal "" 201 statusCode
                        
                        
                        
                        let! statusCode, _ =
                            request
                                "PUT" 
                                (sprintf "/api/users/fc1943s/repositories/%s/tags" repo.Repository.Id)
                                """["dotnet", "functional", "language"]"""
                        Expect.equal "" 204 statusCode
                        
                        
                        
                        let! statusCode, body = request "GET" "/api/users/fc1943s/repositories?tag=e" ""
                        Expect.equal "" 200 statusCode
                        
                        body
                        |> JsonConvert.DeserializeObject<UserRepository[]>
                        |> Array.collect (fun x -> x.Tags)
                        |> Expect.equal "" [| "dotnet"; "functional"; "language" |]
                        
                        
                        
                        let! statusCode, _ =
                            request
                                "DELETE" 
                                (sprintf "/api/users/fc1943s/repositories/%s/tags/functional" repo.Repository.Id)
                                ""
                        Expect.equal "" 204 statusCode
                        
                        
                        
                        let! statusCode, body = request "GET" "/api/users/fc1943s/repositories?tag=e" ""
                        Expect.equal "" 200 statusCode
                        
                        body
                        |> JsonConvert.DeserializeObject<UserRepository[]>
                        |> Array.collect (fun x -> x.Tags)
                        |> Expect.equal "" [| "dotnet"; "language" |]
                    }
            })
        }
    ]

