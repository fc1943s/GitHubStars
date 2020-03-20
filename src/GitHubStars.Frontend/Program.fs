namespace GitHubStars.Frontend

open Fable.Core
open GitHubStars.Shared.Model
open Elmish
open Elmish.React
open Fable.React
open Fulma

#if DEBUG
open Elmish.HMR
#endif

            
module Program =
    JsInterop.importAll "./node_modules/bulma/bulma.sass"
    JsInterop.importAll "./node_modules/bulmaswatch/cyborg/bulmaswatch.scss"
    JsInterop.importAll "./node_modules/@fortawesome/fontawesome-free/css/all.css"
    JsInterop.importAll "./public/index.scss"
    
    type Message =
        | QueryRepositories of user:string
        | QueryRepositoriesByTag of tagPartialSearch:string
        | SetRepositories of user:string * repositories:UserRepository list * tagPartialSearch:string
        | SetTags of repository:UserRepository * tags:string list * local:bool
        | SetError of error:string
        | ResetState
        
    type State =
        { User: string
          Repositories: UserRepository list
          FilteredRepositories: UserRepository list
          LoadingRepositories: bool
          LoadingFilteredRepositories: bool
          TagPartialSearch: string
          EditingRepository: UserRepository option
          Error: string }
        static member inline Default =
            { User = ""
              Repositories = []
              FilteredRepositories = []
              LoadingRepositories = false
              LoadingFilteredRepositories = false
              TagPartialSearch = ""
              EditingRepository = None
              Error = "" }
    
    let update msg state =
        match msg with
        | QueryRepositories user ->
            let state =
                { state with
                      LoadingRepositories = true
                      Error = "" }
                
            let cmd =
                Cmd.fromRequest
                    (Api.queryRepositoriesAsync user "")
                    (function
                        | Ok repositories -> SetRepositories (user, repositories, "")
                        | Error error -> SetError error)
                    
            state, cmd
        
        | QueryRepositoriesByTag tagPartialSearch ->
            if tagPartialSearch = "" then
                let state =
                    { state with
                          Error = ""
                          TagPartialSearch = ""
                          FilteredRepositories = [] }
                    
                state, Cmd.none
            else
                let state =
                    { state with
                          Error = ""
                          LoadingFilteredRepositories = true }
                    
                let cmd =
                    Cmd.fromRequest
                        (Api.queryRepositoriesAsync state.User tagPartialSearch)
                        (function
                            | Ok repositories -> SetRepositories (state.User, repositories, tagPartialSearch)
                            | Error error -> SetError error)
                        
                state, cmd
                
        | SetRepositories (user, repositories, tagPartialSearch) ->
            let state =
                { state with
                    User = user
                    LoadingRepositories = false
                    LoadingFilteredRepositories = false
                    TagPartialSearch = tagPartialSearch
                    Repositories = if tagPartialSearch = "" then repositories else state.Repositories
                    FilteredRepositories = if tagPartialSearch <> "" then repositories else state.FilteredRepositories }
            
            state, Cmd.none
        
        | SetTags (repository, tags, local) ->
            if not local then
                let cmd =
                    Cmd.fromRequest
                        (Api.setTagsAsync state.User repository.Repository.Id tags)
                        (function
                            | Ok () -> SetTags (repository, tags, true)
                            | Error error -> SetError error)
                        
                state, cmd
            else
                let replace =
                    List.map (fun x ->
                        if x.Repository.Id = repository.Repository.Id
                        then { x with Tags = tags |> List.toArray }
                        else x
                    )
                    
                let state =
                    { state with
                          Repositories = state.Repositories |> replace
                          FilteredRepositories = state.FilteredRepositories |> replace }
                
                state, Cmd.none
                
        | SetError error ->
            { state with
                Error = error
                LoadingRepositories = false
                LoadingFilteredRepositories = false }, Cmd.none
            
        | ResetState ->
            State.Default, Cmd.none
        
    let lazyView (props: {| State: State
                            Dispatch: Message -> unit |}) =
        
        let actions = {|
            QueryRepositories = fun user ->
                props.Dispatch (QueryRepositories user)
                
            QueryRepositoriesByTag = fun tagSearchPartial ->
                props.Dispatch (QueryRepositoriesByTag tagSearchPartial)
            
            SetTags = fun (repository, tags) ->
                props.Dispatch (SetTags (repository, tags, false))
                
            HideError = fun _ ->
                props.Dispatch (SetError "")
                
            ResetState = fun _ ->
                props.Dispatch ResetState
        |}
        
        Container.container [ Container.IsFluid ][
            
            if props.State.Error <> "" then
                ErrorComponent.``default``
                    {| Error = props.State.Error
                       HideError = actions.HideError |}
                       
            HeaderComponent.``default``
                {| User = props.State.User
                   ResetState = actions.ResetState |}
                
            match props.State with
            | { User = ""; LoadingRepositories = false } ->
                SearchPageComponent.``default``
                    {| QueryRepositories = actions.QueryRepositories |}
                
            | { User = ""; LoadingRepositories = true } ->
                SearchPageComponent.loading
                
            | _ ->
                RepositoryListPageComponent.``default``
                    {| Repositories =
                           if props.State.TagPartialSearch <> ""
                           then props.State.FilteredRepositories
                           else props.State.Repositories
                       QueryRepositoriesByTag = actions.QueryRepositoriesByTag
                       LoadingFilteredRepositories = props.State.LoadingFilteredRepositories
                       SetTags = actions.SetTags |}
        ]
        
    let viewWrapper =
        fun (state: State) (dispatch: Message -> unit) ->
            FunctionComponent.Of lazyView
                {| State = state
                   Dispatch = dispatch |}

    let init () = State.Default, Cmd.none
    
    Program.mkProgram init update viewWrapper
    |> Program.withReactSynchronous "app"
    |> Program.run

