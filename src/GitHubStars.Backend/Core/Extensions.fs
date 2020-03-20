namespace GitHubStars.Backend

open FSharpPlus.Data

module Result =
    let fold fn state =
        Seq.fold (fun state next ->
            match state, next with
            | Ok ys, Ok y -> fn ys y |> Ok
            | Error e, _ -> Error e
            | _, Error e -> Error e
        ) state

module Async =
    let wrap x =
        async { return x }
        
module Core =
    let resultOrException result =
        result
        |> Result.mapError exn
        |> ResultOrException.Result
