namespace GitHubStars.Frontend

open Fable.Core.JsInterop
open Elmish

module Cmd =
    let fromRequest request onSuccess =
        Cmd.OfAsync.perform
            (fun () -> request) ()
            onSuccess
            
module Ext =
    [<AbstractClass>]
    type IUseDebounce =
        // fsharplint:disable-next-line MemberNames
        abstract useDebouncedCallback: fn:('T -> unit) -> delay:int -> ('U -> unit)[]
        
        member _.Callbacks (callbacks: ('U -> unit)[]) =
            match callbacks with
            | [| debouncedCallback; cancelDebouncedCallback; callPending |] ->
                {| DebouncedCallback = debouncedCallback
                   CancelDebouncedCallback = cancelDebouncedCallback
                   CallPending = callPending |}
            | _ -> failwith "Error getting callbacks"
            
        member this.UseDebouncedCallback fn delay =
            this.useDebouncedCallback fn delay
            |> this.Callbacks
            |> fun x -> x.DebouncedCallback
            
    let useDebounce : IUseDebounce = importAll "use-debounce"
    
