namespace GitHubStars.Backend

open FSharpPlus.Data

module Async =
    let wrap x =
        async { return x }
        
module Core =
    let resultOrException result =
        result
        |> Result.mapError exn
        |> ResultOrException.Result
