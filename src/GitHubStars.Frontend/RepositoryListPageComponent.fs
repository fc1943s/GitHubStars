namespace GitHubStars.Frontend

open System
open Fable.React.Props
open GitHubStars.Shared.Model
open Fable.React
open Fulma
open Fable.FontAwesome
open Browser.Types

module RepositoryListPageComponent =
    let modal = FunctionComponent.Of (fun (props: {| Repository: UserRepository
                                                     SetTags: UserRepository * string list -> unit
                                                     CancelEditing: unit -> unit |}) ->
        let splitTags (text: string) =
            text.Split ','
            |> Array.map (fun x -> x.Trim ())
            |> Array.toList
            
        let state =
            Hooks.useState
                {| Text = String.Join (", ", props.Repository.Tags)
                   Valid = true |}
                   
        let save () =
            let tags =
                state.current.Text
                |> splitTags
                |> cleanTags
            
            props.SetTags (props.Repository, tags)
            
            props.CancelEditing ()
                   
        let events = {|
            OnInputChange = fun (e: Event) ->
                state.update
                    {| Text = e.Value
                       Valid = e.Value |> splitTags |> validateTags |}
                       
            OnInputKeyDown = fun (e: KeyboardEvent) ->
                if e.key = "Enter" then
                    save ()
            
            OnSaveClick = fun _ ->
                save ()
                
            OnCancel = fun _ ->
                props.CancelEditing ()
        |}
        
        Modal.modal [ Modal.IsActive true ][
              
            Modal.background [ Props [ OnClick events.OnCancel ] ][]
            
            Modal.Card.card [][
                Modal.Card.head [][
                    Modal.Card.title [][
                        str ("Edit tags for " + props.Repository.Repository.Name)
                    ]
                    
                    Delete.delete [ Delete.OnClick events.OnCancel ][]
                ]
                    
                Modal.Card.body [][
                    Input.text [ Input.Value state.current.Text
                                 Input.Props [ AutoFocus true
                                               OnChange events.OnInputChange
                                               OnKeyDown events.OnInputKeyDown ] ]
                ]
                    
                Modal.Card.foot [][
                    Button.button [ Button.Color IsSuccess
                                    Button.OnClick events.OnSaveClick
                                    Button.Disabled (not state.current.Valid) ][
                        str "Save"
                    ]
                    
                    Button.button [ Button.OnClick events.OnCancel ][
                        str "Cancel"
                    ]
                ]
            ]
        ]
    , memoizeWith = equalsButFunctions)
    
    let searchInput = FunctionComponent.Of (fun (props: {| LoadingFilteredRepositories: bool
                                                           QueryRepositoriesByTag: string -> unit |}) ->
        let state =
            Hooks.useState
                {| TagSearchPartial = "" |}
                   
        let debounceTagSearch =
            Ext.useDebounce.UseDebouncedCallback props.QueryRepositoriesByTag 700
            
        let events = {|
            OnInputChange = fun (e: Event) ->
                let tagSearchPartial = e.Value
                state.update (fun state -> {| state with TagSearchPartial = tagSearchPartial |} )
                debounceTagSearch tagSearchPartial
        |}
        
        Control.div [ Control.HasIconLeft
                      Control.HasIconRight
                      Control.Props [ Style [ Display DisplayOptions.Inline ] ] ][
            
            Input.text [ Input.Value state.current.TagSearchPartial
                         Input.Props [ Placeholder "search by tag"
                                       OnChange events.OnInputChange
                                       Style [ Width 300 ] ] ]
            
            Icon.icon [ Icon.Size IsSmall
                        Icon.IsLeft ][
                Fa.i [ Fa.Solid.Search ][]
            ]
            
            if props.LoadingFilteredRepositories then
                Icon.icon [ Icon.Size IsSmall
                            Icon.IsRight ][
                    Fa.i [ Fa.Spin
                           Fa.Solid.Spinner ][]
                ]
        ]
    , memoizeWith = equalsButFunctions)
        
    let ``default`` = FunctionComponent.Of (fun (props: {| Repositories: UserRepository list
                                                           QueryRepositoriesByTag: string -> unit
                                                           LoadingFilteredRepositories: bool
                                                           SetTags: UserRepository * string list -> unit |}) ->
        let state =
            Hooks.useState
                {| EditingRepository = None |}
            
        let events = {|
            OnCancelEditing = fun _ ->
                state.update (fun state -> {| state with EditingRepository = None |} )
                
            OnEditRepositoryClick = fun repository _ ->
                state.update (fun state -> {| state with EditingRepository = Some repository |} )
        |}
        
        div [][
            
            match state.current.EditingRepository with
            | None -> ()
            | Some repository ->
                modal
                    {| Repository = repository
                       SetTags = props.SetTags
                       CancelEditing = events.OnCancelEditing |}
            
            searchInput
                {| LoadingFilteredRepositories = props.LoadingFilteredRepositories
                   QueryRepositoriesByTag = props.QueryRepositoriesByTag |}
            
            Table.table [ Table.IsBordered
                          Table.IsFullWidth
                          Table.IsStriped
                          Table.Props [ Style [ MarginTop 40 ] ] ][
                
                thead [][
                    tr [][
                        th [][ str "Repository" ]
                        th [][ str "Description" ]
                        th [][ str "Language" ]
                        th [][ str "Tags" ]
                        th [][ str "" ]
                    ]
                ]
                
                props.Repositories
                |> List.map (fun repository ->
                    tr [][
                        td [][
                            a [ Href repository.Repository.Url
                                Target "_blank" ][
                                str repository.Repository.Name
                            ]
                        ]
                        td [][ str repository.Repository.Description ]
                        td [][ str (repository.Repository.Language.ToLower ()) ]
                        td [][ str (String.Join (" ", repository.Tags |> Array.map ((+) "#"))) ]
                        td [][
                            a [ OnClick (events.OnEditRepositoryClick repository) ][
                                str "edit"
                            ]
                        ]
                    ]
                )
                |> tbody []
            ]
        ]
    , memoizeWith = equalsButFunctions)

