module Client

open Elmish
open Elmish.React

open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open Fable.Core.JsInterop
open Fulma

open Shared


//
type Model = {
    //contains the plot html as string
    PlotHTML : string
    //is the server processing the overview html?
    Loading : bool
    //clicked on "from-to"
    ClickedOnChunk : bool
    //loading screen for "from-to"
    LoadingChunk : bool
    //clicked on "wavelet"
    ClickedOnWavelet : bool
    //trace selection buttons for "wavelet"
    TraceSelectionMdl : bool
    //loading screen for "wavelet"
    LoadingWavelet : bool
    //contains the string entered in "from-to"
    SearchBarChunkMdl : string
    //contains the string entered in "wavelet"
    SearchBarWaveletMdl : string
    //contains the trace processed by 'wavelet'
    WaveletTrace : string
    }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
//overviewPlot
| GetPlotRequest
//returns the plot html as string
| GetPlotResponse of Result<string, exn>
| PlotLoadingYear

//chunkProcessing
| ChunkClick
| SearchBarChunk
//returns the text entered into the 'get-from' search bar
| UpdateChunkSearchBar of string
| GetPlotChunkRequest
//returns the plot html as string
| GetPlotChunkResponse of Result<string, exn>
| PlotLoadingChunk

//waveletProcessing
| WaveletClick
| SearchBarWavelet
//returns the text entered into the 'wavelet' search bar
| UpdateWaveletSearchBar of string
| GetPlotWaveletRequest
//returns the plot html as string
| GetPlotWaveletResponse of Result<string, exn>
| TraceSelectionWavelet
| PlotLoadingWavelet1
| PlotLoadingWavelet2
| PlotLoadingWavelet3
| PlotLoadingWavelet4
| PlotLoadingWavelet5
| PlotLoadingWavelet6
| PlotLoadingWaveletLight
| PlotLoadingWaveletRain

//rain
//returns unit, because the data is directly written to the database
| FetchRainResponse of Result<unit, exn> 
| FetchRain



module Server =

    open Shared
    open Fable.Remoting.Client
    
    let api : ILoggingAPI =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<ILoggingAPI>


module RequestHelpers =
    //overviewPlot
    //
    let createLoadingCmdPlot =
        Cmd.ofMsg GetPlotRequest

    let createGetPlotCmd =      
        Cmd.OfAsync.either
            Server.api.GetPlot
            ()
            (Ok >> GetPlotResponse)
            (Error >> GetPlotResponse)
    
    //chunkPlot
    //command to open the search bar when clicked on from-to
    let openSearchBarChunkCmd =      
        Cmd.ofMsg ChunkClick
    //when entered the dates, the loading is initialized
    let createLoadingCmdChunk =
        Cmd.ofMsg GetPlotChunkRequest   
    //while loading the plot gets generated with input: the date from/to wich the plot should range ("yyMMdd - yyMMdd")
    let createGetPlotChunkCmd dates=
        Cmd.OfAsync.either
            Server.api.GetPlotChunk 
            dates
            (Ok >> GetPlotChunkResponse)
            (Error >> GetPlotChunkResponse)        

    //wavelet
    //command to open the search bar when clicked on wavelet
    let openSearchBarWaveletCmd =      
        Cmd.ofMsg WaveletClick
        
    //when entered the dates, the trace selection for continuous wavelet transformation is displayed
    let openTraceSelectionCmd =      
        Cmd.ofMsg TraceSelectionWavelet            
    //when the trace is selected, the loading is initialized
    let createLoadingCmdWavelet =
        Cmd.ofMsg GetPlotWaveletRequest        
    //while loading the plot gets generated with input: the date from/to wich the plot should range ("yyMMdd - yyMMdd") and the specified trace as string e.g. "T1"
    let createGetPlotWaveletCmd dates trace=
        Cmd.OfAsync.either
            Server.api.GetPlotWavelet 
            (dates,trace)
            (Ok >> GetPlotWaveletResponse)
            (Error >> GetPlotWaveletResponse)

    //rain
    //when clicked on fetch rain data the rain data is downloaded and integrated into the data base
    let fetchRainCmd =
        Cmd.OfAsync.either
            Server.api.FetchRain
            ()
            (Ok >> FetchRainResponse)
            (Error >> FetchRainResponse)   

                



// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { 
        PlotHTML = ""
        Loading = false 
        ClickedOnChunk = false
        SearchBarChunkMdl = ""
        LoadingChunk = false
        ClickedOnWavelet = false
        SearchBarWaveletMdl = ""
        TraceSelectionMdl = false
        LoadingWavelet = false
        WaveletTrace = ""}
    initialModel, Cmd.none



// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match  msg with
    //overviewPlot
    | PlotLoadingYear -> 
        let nextModel =
            //when clicked on 'last year' the other buttons should be resetted and the over view plot should be loaded
            { currentModel with Loading = true; ClickedOnChunk = false;ClickedOnWavelet = false;PlotHTML=""}
        nextModel,RequestHelpers.createLoadingCmdPlot     
    | GetPlotRequest -> currentModel,RequestHelpers.createGetPlotCmd 
    | GetPlotResponse (Ok res) ->    
        let nextModel = 
            {
                currentModel with PlotHTML = res; Loading=false
            }
        nextModel, Cmd.none    
    | GetPlotResponse (Error ex)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML = ex.Message; Loading=false
                }
        nextModel,Cmd.none


    //Chunk
    | SearchBarChunk ->
        let nextModel = 
            //the current plot is resetted and the click button changes its color. The wavelet click is resetted
            {currentModel with PlotHTML="";ClickedOnChunk = (not currentModel.ClickedOnChunk);ClickedOnWavelet = false}
        nextModel,Cmd.none
    | PlotLoadingChunk -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingChunk = true}
        nextModel,RequestHelpers.createLoadingCmdChunk     
    | GetPlotChunkRequest -> 
        currentModel,RequestHelpers.createGetPlotChunkCmd currentModel.SearchBarChunkMdl
    | GetPlotChunkResponse (Ok res) ->    
        let nextModel = 
            {
                currentModel with PlotHTML = res; LoadingChunk=false
            }
        nextModel, Cmd.none    
    | GetPlotChunkResponse (Error ex)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML = ex.Message; LoadingChunk=false
                }
        nextModel,Cmd.none  

    //FetchRainResponse
    | FetchRainResponse (Ok res) ->    
        currentModel,Cmd.none    
    | FetchRainResponse (Error ex)  -> 
        currentModel,Cmd.none  
    | FetchRain -> 
        currentModel,RequestHelpers.fetchRainCmd


    //Wavelet
    | SearchBarWavelet ->
        let nextModel =
            //when clicked on wavelet, the plotand the chunk button are resetted
            {currentModel with PlotHTML="";ClickedOnWavelet = (not currentModel.ClickedOnWavelet);ClickedOnChunk = false;TraceSelectionMdl = false}
        nextModel,Cmd.none    
    | UpdateChunkSearchBar (input)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML="";SearchBarChunkMdl = input
                }
        nextModel,Cmd.none
    | TraceSelectionWavelet -> 
        let nextModel =
            {
                currentModel with TraceSelectionMdl = true
            }
        nextModel,Cmd.none
    //because the chart needs a trace as input, a string is given, that defines the sensor
    | PlotLoadingWavelet1 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T1"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWavelet2 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T2"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | PlotLoadingWavelet3 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T3"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWavelet4 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T4"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | PlotLoadingWavelet5 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T5"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWavelet6 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T6"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | PlotLoadingWaveletLight -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "Light"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWaveletRain -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "Rain"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | GetPlotWaveletRequest -> 
        currentModel,RequestHelpers.createGetPlotWaveletCmd currentModel.SearchBarWaveletMdl currentModel.WaveletTrace
    | GetPlotWaveletResponse (Ok res) ->    
        let nextModel = 
            {
                currentModel with PlotHTML = res; LoadingWavelet=false
            }
        nextModel, Cmd.none    
    | GetPlotWaveletResponse (Error ex)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML = ex.Message; LoadingWavelet=false
                }
        nextModel,Cmd.none  
    | UpdateWaveletSearchBar (input)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML="";SearchBarWaveletMdl = input
                }
        nextModel,Cmd.none  
    | _ -> currentModel, Cmd.none

//credits for footer
let safeComponents =
    let components =
        span [ ]
           [
             a [ Href "http://suave.io" ] [ str "Suave" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://elmish.github.io/elmish/" ] [ str "Elmish" ]
             str ", "
             a [ Href "https://fulma.github.io/Fulma" ] [ str "Fulma" ]
             str ", "
             a [ Href "https://zaid-ajaj.github.io/Fable.Remoting/" ] [ str "Fable.Remoting" ]
           ]

    span [ ]
        [ str " powered by: "
          components ]

//default button
let button (model:Model) txt onClick =
    Button.button
        [ Button.IsFullWidth
          Button.CustomClass "button"
          Button.OnClick onClick ]
        [ str txt ]

//red button for rain data fetch
let fetchbutton txt onClick =
    Button.button
        [ Button.IsFullWidth
          Button.CustomClass "redbutton"
          Button.OnClick onClick ]
        [ str txt ]

//from-to button changes color when clicked
let fromToButton (model:Model) txt onClick =
    let buttonCol = if model.ClickedOnChunk then (Button.CustomClass "redbutton") else ( Button.CustomClass "button")
    Button.button
        [ Button.IsFullWidth
          buttonCol
          Button.OnClick onClick ]
        [ str txt ]

//wavelet button changes color when clicked
let waveletButton (model:Model) txt onClick =
    let buttonCol = if model.ClickedOnWavelet then (Button.CustomClass "redbutton") else ( Button.CustomClass "button")
    Button.button
        [ Button.IsFullWidth
          buttonCol
          Button.OnClick onClick ]
        [ str txt ]    

//contains the loading gif url and centers it to the screen
let loading = 
    Columns.columns []
        [   
            Column.column [ Column.Width (Screen.Desktop,Column.Is4)] []
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] [
                                                        
                Image.image [Image.Option.CustomClass "centr" ]                                   
                    [
                        img 
                            [   
                                Props.Src "https://media.giphy.com/media/4ZgLPakqTajjVFOVqw/giphy.gif"
                            ]
                    ]                                                         
                ]
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] []
                              
        ]

//because the wavelet transform takes a few seconds, an additional notification is added
let loadingWavelet = 
    Columns.columns []
        [   
            Column.column [ Column.Width (Screen.Desktop,Column.Is4)] []
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] [
                
                Notification.notification [ Notification.Color IsWarning;Notification.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Centered) ] ] [str "This may take some time!"]                                        
                Image.image [Image.Option.CustomClass "centr" ]                                   
                    [
                        img 
                            [   
                                Props.Src "https://media.giphy.com/media/4ZgLPakqTajjVFOVqw/giphy.gif"
                            ]
                    ]                                                         
                ]
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] []
                              
        ]


//main view
let view (model : Model) (dispatch : Msg -> unit) =
    
    div []
        [ 
          Hero.hero [Hero.IsMedium; Hero.CustomClass "csbHero"] [
               Hero.body [] [
                   Container.container [] [
                       Heading.h1 [Heading.CustomClass "csbHead"] [
                           str "AlgaeWatch"
                       ]
                       Heading.h3 [Heading.CustomClass "csbHead";Heading.IsSubtitle] [
                           str "Automated logging and analysis of water temperature"
                       ]
                   ]
               ]
           ]
          
          
          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    []
                Columns.columns []
                    [ //the four buttons are separated into 4 columns
                      Column.column [] [ button model "show last year" (fun _ -> dispatch PlotLoadingYear) ] 
                      Column.column [] [ fromToButton model "show from-to" (fun _ -> dispatch SearchBarChunk) ] 
                      Column.column [] [ waveletButton model "show wavelet from to" (fun _ -> dispatch SearchBarWavelet)] 
                      Column.column [] [ fetchbutton "Fetch rain data" (fun _ -> dispatch FetchRain) ] 
                      ]

                Columns.columns [Columns.CustomClass "outerFrame"; Columns.IsCentered ] [
                    Column.column [Column.Width (Screen.Desktop,Column.Is12)] [
                        //when clicked on "from-to" to get a chunk of the data presented as Plotly chart
                        if model.ClickedOnChunk then
                            yield 
                                    Columns.columns [ ]

                                        [Column.column [] []  
                                         Column.column [ Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                                            [ 
                                            Level.item [ ]
                                                [ Field.div [ Field.HasAddons ]
                                                    [ Control.div [ ]
                                                        [ Input.text [  //the placeholder defines the data format
                                                                        Input.Placeholder ("yyMMdd - yyMMdd" )
                                                                        Input.OnChange (fun e ->    let x = !!e.target?value
                                                                                                    dispatch (UpdateChunkSearchBar x)
                                                                                                    
                                                                                    )
                                                                   ]
                                                                  ]
                                                      Control.div [ ] [
                                                                    //when clicked on from-to dispatch PlotLoadingChunk
                                                                    button model "from-to" (fun _ -> dispatch PlotLoadingChunk)
                                                                  ]
                                                    ]
                                                ]
                                           ]
                                         Column.column [] []
                                         Column.column [] []                                       
                                        ]
                        //happens when it was clicked on 'wavelet'
                        elif model.ClickedOnWavelet then                
                            yield 
                                    Columns.columns [ ]

                                        [Column.column [] []  
                                         Column.column [] []
                                         Column.column [ Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                                            [ 
                                            Level.item [ ]
                                                [ Field.div [ Field.HasAddons ]
                                                    [ Control.div [ ]
                                                        [ Input.text [  Input.Placeholder ("yyMMdd - yyMMdd" )
                                                                        Input.OnChange (fun e ->    let x = !!e.target?value
                                                                                                    dispatch (UpdateWaveletSearchBar x)
                                                                                                    
                                                                                    )
                                                                   ]
                                                                  ]
                                                      Control.div [ ] [
                                                            //when clicked on "from-to" open the trace selection buttons (T1 to Rain)
                                                            button model "from-to" (fun _ -> dispatch TraceSelectionWavelet)
                                                                  ]
                                                    ]
                                                ]
                                            ]
                                         Column.column [] []                                       
                                        ]
                            //open the trace selection buttons (T1 to Rain)
                            if model.TraceSelectionMdl then                  
                                              
                                yield 
                                        Columns.columns [ ]

                                          [
                                            Column.column [] []  
                                            Column.column [] []
                                            Column.column [ Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                                               [ 
                                                Columns.columns [ Columns.IsGap (Screen.All,Columns.ISize.Is8) ]
                                                                [ 
                                                                  Column.column [] [button model "T1" (fun _ -> dispatch    PlotLoadingWavelet1 )]
                                                                  Column.column [] [button model "T2" (fun _ -> dispatch    PlotLoadingWavelet2 )]
                                                                  Column.column [] [button model "T3" (fun _ -> dispatch    PlotLoadingWavelet3 )]
                                                                  Column.column [] [button model "T4" (fun _ -> dispatch    PlotLoadingWavelet4 )]
                                                                  Column.column [] [button model "T5" (fun _ -> dispatch    PlotLoadingWavelet5 )]
                                                                  Column.column [] [button model "T6" (fun _ -> dispatch    PlotLoadingWavelet6 )]
                                                                  Column.column [] [button model "Light" (fun _ -> dispatch PlotLoadingWaveletLight)] 
                                                                  Column.column [] [button model "Rain" (fun _ -> dispatch  PlotLoadingWaveletRain )] 

                                                                ]
                                                
                                               ]
                                            Column.column [] []                                       
                                            ]
                        //loading...                                                                                               
                        if model.Loading then
                            yield 
                                loading
                        elif model.LoadingChunk then
                            yield 
                                loading
                        elif model.LoadingWavelet then
                            yield 
                                loadingWavelet                                                 
                        else
                            //iframe to display the plot
                            yield iframe 
                                    [
                                      SrcDoc model.PlotHTML
                                      Id "frame"
                                    ] []                                          

                    
                ] ]
                Content.content [ Content.Modifiers [ Modifier.IModifier.Display (Screen.All, Display.Block) ] ] 
                            [ ] ]

          Footer.footer [ ]
                [   Columns.columns []
                        [
                            Column.column [] [
                                Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] 
                                    [   span [] [str "created by: "]
                                        a [ Href "https://github.com/bvenn" ] [ str "Benedikt Venn" ] ] ] 
                        
                            Column.column [] [
                            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] 
                                    [   
                                        a [ Href "https://github.com/bvenn/AlgaeWatch" ] [ str "Documentation" ] ] ] 
                        
                            Column.column [] [
                            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [ safeComponents ] ]                                 
                        ]
                ]
            ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run

