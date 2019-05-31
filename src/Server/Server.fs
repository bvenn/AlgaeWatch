open System.IO
open System.Net

open Shared

open Suave
open Suave.Files
open Suave.Successful
open Suave.Filters
open Suave.Operators
open FSharp.Plotly

open Fable.Remoting.Server
open Fable.Remoting.Suave
open FSharp.Plotly.GenericChart
open System.Data.SQLite
open Suave.Logging
open System
open Suave.Logging
open System.Data.SQLite

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x
let publicPath = Path.GetFullPath "../Client/public"
let oldPlotPath = __SOURCE_DIRECTORY__ + @"\content"//Path.GetFullPath "/content"
let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us



module Database =
    ///define the path of the SQLite database
    let dbPath = __SOURCE_DIRECTORY__ + @"\db\TemperatureData.db"

    ///define how to get the connection string for the data base
    let getCn() = 
        let connectionString = sprintf "Data Source=%s;Version=3" dbPath
        new SQLiteConnection(connectionString)


    ///record type containing all fields stored in the data base
    type DBItem = {
        TimeStamp    : int64        
        DateTime     : int64
        Temperature1 : float
        Temperature2 : float
        Temperature3 : float
        Temperature4 : float
        Temperature5 : float
        Temperature6 : float
        Lightsensor  : float
        RainData     : float
        }

    ///creates the DBItem record type
    let createDbItem ts dt t1 t2 t3 t4 t5 t6 light rain = 
        {
        TimeStamp    = ts          
        DateTime     = dt
        Temperature1 = t1  
        Temperature2 = t2
        Temperature3 = t3
        Temperature4 = t4
        Temperature5 = t5
        Temperature6 = t6
        Lightsensor  = light
        RainData     = rain
        }

    ///creates a DBItem from only strings and the data base time stamp
    let createDbItemFromString ts (dt:string) (t1:string) (t2:string) (t3:string) (t4:string) (t5:string) (t6:string) (light:string) (rain:string) = 
        createDbItem 
             ts
            (dt    |> int64)
            (t1    |> float) 
            (t2    |> float)
            (t3    |> float)
            (t4    |> float)
            (t5    |> float)
            (t6    |> float)
            (light |> float)
            (rain  |> float)

    //creates a DataBase with two tables:
    //1. TemperatureData with Items consisting of: TimeStamp(from Server), DateTime(from Sensor), temperature values 1 - 6, light intensity and rain amount
    //2. RainData with one single Item consisting of: LastFetch(last date rain data was fetched) and ID
    let initDB fileName =
        let intitTemperatureData (cn:SQLiteConnection) = 
            let querystring =         
                "CREATE TABLE TemperatureData (
                    TimeStamp INTEGER NOT NULL,
                    DateTime INTEGER NOT NULL UNIQUE,
                    Temperature1 REAL,
                    Temperature2 REAL,
                    Temperature3 REAL,
                    Temperature4 REAL,
                    Temperature5 REAL,
                    Temperature6 REAL,
                    LightSensor REAL,
                    RainData REAL,
                    PRIMARY KEY(DateTime)
                    )"
            let cmd  = new SQLiteCommand(querystring, cn)
            cmd.ExecuteNonQuery()

        let intitRainData (cn:SQLiteConnection) = 
            let querystring =         
                "CREATE TABLE RainData (
                    LastFetch INTEGER NOT NULL UNIQUE,
                    ID Integer NOT NULL UNIQUE
                    PRIMARY KEY(ID)
                    )"
            let cmd  = new SQLiteCommand(querystring, cn)
            cmd.ExecuteNonQuery()        

        let connectionString = sprintf "Data Source=%s;Version=3" fileName
        use cn = new SQLiteConnection(connectionString)

        cn.Open()
        intitTemperatureData cn |> ignore
        intitRainData cn |> ignore
        cn.Close()

    ///because the RainData table is updated ever when new rain data is fetched, there has to be an initial entry
    let insertInitialRainFetch() =
        let initialFetchDate = 101010101010L
        use cn = getCn()
        cn.Open()  
        let insertString = 
            "INSERT INTO RainData (
                LastFetch,
                ID) 
                    VALUES (
                        @lastfetch,
                        @id)"

        use cmd  = new SQLiteCommand(insertString, cn)
        cmd.Parameters.Add("@lastfetch" ,System.Data.DbType.Int64) |> ignore
        cmd.Parameters.Add("@id"        ,System.Data.DbType.Int16) |> ignore
        cmd.Parameters.["@lastfetch"].Value <- initialFetchDate
        cmd.Parameters.["@id"].Value        <- 1
        cmd.ExecuteNonQuery() |> ignore
        cn.Close() 

    ///Inserts a single TemperatureEvent into the TemperatureData table.
    ///Because rain data is fetched afterwards it is set to 0.
    ///The format is short to reduce the traffic of the arduinos GSM module:
    let insertDbItemWithRain (data: Shared.TransmitJSON) (rain : float) (cn:SQLiteConnection)=
        try //create the item, that should be inserted to the data base
            let tmpItem =
                //convert the server time stamp to string
                let timestampAsInt = System.DateTime.Now.ToString("yyMMddHHmmss") |> int64
                //convert the data logger time stamp to string
                let dateInt = 
                    let tmp = data.D
                    tmp.Replace(":","").Replace(".","").Replace(" ","")     
                    |> fun x -> x.[6..7] + x.[2..3] + x.[0..1] + x.[8..9] + x.[10..11] + x.[12..13]  
                    |> int64
                //"18.05.2019 16:49:10_12_4._-2._5_123.123_41"
                createDbItem timestampAsInt dateInt data.T1 data.T2 data.T3 data.T4 data.T5 data.T6 data.L rain
            let insertString = 
                "INSERT INTO TemperatureData (
                    TimeStamp,
                    DateTime,
                    Temperature1,
                    Temperature2,
                    Temperature3,
                    Temperature4,
                    Temperature5,
                    Temperature6,
                    LightSensor,
                    RainData) 
                        VALUES (
                            @timestamp,
                            @datetime,
                            @t1,
                            @t2,
                            @t3,
                            @t4,
                            @t5,
                            @t6,
                            @light,
                            @rain)"
  
            use cmd  = new SQLiteCommand(insertString, cn)

            cmd.Parameters.Add("@timestamp" ,System.Data.DbType.Int64) |> ignore
            cmd.Parameters.Add("@datetime"  ,System.Data.DbType.Int64) |> ignore
            cmd.Parameters.Add("@t1"        ,System.Data.DbType.Double) |> ignore
            cmd.Parameters.Add("@t2"        ,System.Data.DbType.Double) |> ignore
            cmd.Parameters.Add("@t3"        ,System.Data.DbType.Double) |> ignore
            cmd.Parameters.Add("@t4"        ,System.Data.DbType.Double) |> ignore        
            cmd.Parameters.Add("@t5"        ,System.Data.DbType.Double) |> ignore     
            cmd.Parameters.Add("@t6"        ,System.Data.DbType.Double) |> ignore
            cmd.Parameters.Add("@light"     ,System.Data.DbType.Double) |> ignore
            cmd.Parameters.Add("@rain"      ,System.Data.DbType.Double) |> ignore
            //filling of the variables
            cmd.Parameters.["@timestamp"].Value <- tmpItem.TimeStamp
            cmd.Parameters.["@datetime"].Value  <- tmpItem.DateTime
            cmd.Parameters.["@t1"].Value        <- tmpItem.Temperature1
            cmd.Parameters.["@t2"].Value        <- tmpItem.Temperature2
            cmd.Parameters.["@t3"].Value        <- tmpItem.Temperature3
            cmd.Parameters.["@t4"].Value        <- tmpItem.Temperature4
            cmd.Parameters.["@t5"].Value        <- tmpItem.Temperature5
            cmd.Parameters.["@t6"].Value        <- tmpItem.Temperature6
            cmd.Parameters.["@light"].Value     <- tmpItem.Lightsensor
            cmd.Parameters.["@rain"].Value      <- tmpItem.RainData

            cmd.ExecuteNonQuery() |> ignore

        with e as exn -> printfn "%s" exn.Message

    ///insert an item to the data base from a transmission JSON string and rain = 0.
    let insertDbItem (transmitJSON : Shared.TransmitJSON)= 
        use cn = getCn()
        cn.Open()
        insertDbItemWithRain transmitJSON 0. cn
        cn.Close()

    ///get all data base entries between date begin and date end (format: "yyMMddHHmmss"
    let getDbItem (dateBegin: int64) (dateEnd: int64) (cn: SQLiteConnection) =  

        let querystring = 
            "SELECT * FROM TemperatureData WHERE DateTime BETWEEN @begin AND @end"
        
        let cmd = new SQLiteCommand(querystring, cn)

        cmd.Parameters.Add("@begin", System.Data.DbType.Int64) |> ignore
        cmd.Parameters.Add("@end", System.Data.DbType.Int64) |> ignore
        cmd.Parameters.["@begin"].Value <- dateBegin
        cmd.Parameters.["@end"].Value   <- dateEnd
        use reader = cmd.ExecuteReader()
       
        let rec readerloop (reader:SQLiteDataReader) (acc) =
            match reader.Read() with
                       //read out the single parameters with indices
            | true  -> readerloop reader ((reader.GetInt64(0),reader.GetInt64(1),reader.GetDouble(2),reader.GetDouble(3),reader.GetDouble(4),reader.GetDouble(5),reader.GetDouble(6),reader.GetDouble(7),reader.GetDouble(8),reader.GetDouble(9)):: acc)
            | false -> acc 
        readerloop reader []
        |> List.map (fun (ts,dt,t1,t2,t3,t4,t5,t6,light,rain) -> createDbItem ts dt t1 t2 t3 t4 t5 t6 light rain)


    //contains the (data*value) pairs of every sensor
    type LoggChartingData = {
            DataT1    : (System.DateTime * float) list 
            DataT2    : (System.DateTime * float) list 
            DataT3    : (System.DateTime * float) list 
            DataT4    : (System.DateTime * float) list 
            DataT5    : (System.DateTime * float) list 
            DataT6    : (System.DateTime * float) list 
            DataLight : (System.DateTime * float) list 
            DataRain  : (System.DateTime * float) list 
        }

    /// get LoggChartingData with dates specifying the date range
    let getDbData fromDate toDate =
        let cn = getCn() 
        cn.Open()
        let rainEvents  = getDbItem fromDate toDate cn |> Array.ofList
        cn.Close()
        let length = rainEvents.Length
        let getDate str = System.DateTime.ParseExact(str,"yyMMddHHmmss",null)

        let rec loop i accT1 accT2 accT3 accT4 accT5 accT6 accLight accRain =
            if i < length then
                let date = getDate (string rainEvents.[i].DateTime)
                let currentTempEvent = rainEvents.[i]
                loop (i+1) 
                            ((date,currentTempEvent.Temperature1)::accT1)
                            ((date,currentTempEvent.Temperature2)::accT2)
                            ((date,currentTempEvent.Temperature3)::accT3)
                            ((date,currentTempEvent.Temperature4)::accT4)
                            ((date,currentTempEvent.Temperature5)::accT5)
                            ((date,currentTempEvent.Temperature6)::accT6)
                            ((date,currentTempEvent.Lightsensor)::accLight)
                            ((date,currentTempEvent.RainData)::accRain)
            else 
                {
                DataT1    = accT1    |> List.rev
                DataT2    = accT2    |> List.rev
                DataT3    = accT3    |> List.rev
                DataT4    = accT4    |> List.rev
                DataT5    = accT5    |> List.rev
                DataT6    = accT6    |> List.rev
                DataLight = accLight |> List.rev
                DataRain  = accRain  |> List.rev
                }    

        let processedData = loop 0 [] [] [] [] [] [] [] []
        processedData
    



///module for data processing
module Processing =
    
    ///Ricker, or Mexican hat wavelet
    type Ricker = {
        //the scale of the wavelet
        Scale       : float
        //half of the width of the wavelet
        PaddingArea : float
        //x_value of minimum y_value
        MinimumPosX : float
        //function that takes a x_value and gives the corresponding y_value
        RickerFun   : (float -> float)
        }

    //creation function for Ricker
    let createRicker scale =  
        let rickerFun x = 
            let xx = pown x 2
            let ss = pown scale 2
            let fakA = 2./(sqrt(3.*scale)*1.331335364)//(Math.PI**0.25)) calculated for efficiency
            let fakB = 1.-(xx/ss)
            let fakC = Math.E**(-xx/(2.*ss))
            fakA * fakB * fakC
        let padArea =   (7. * scale) |> ceil
        let minimumPosX = 1.73205 * scale
        {
        Scale       = scale
        PaddingArea = padArea
        MinimumPosX = minimumPosX
        RickerFun   = rickerFun
        }

    ///padds data points to the beginning, the end and on internal intervals of data
    module Padding = 
  
        type InternalPaddingMethod =
            //inserts random data points taken from the original data set in a huge data gap
            | Random 
            //inserts nan values in a huge data gap
            | NaN
            //does not insert any point internally
            | Delete
            //inserts points lying on the linear interpolation of the two adjacent knots
            | LinearInterpolation

        ///Adds additional data points to the beginning and end of data (number: borderpadding; x_Value distance: minDistance; y_Value: random).
        ///If huge data chunks are missing (missing gap < maxDistance), data points are added with y_Value nan or rnd, or no points are added.
        ///set maxDistance high to avoid this extra condition
        ///Between every pair of data point where the difference in x_Values is greater than minDistance, additional datapoints are generated (linear interpolation of adjacent points)
        ///getDiff: get the difference in x_Values as float representation (if 'a is float then (-))
        ///addToXValue: function that adds a float to the x_Value (if 'a is float then (+))
        let padding (data : ('a * float) []) (minDistance: float) (maxDistance : float) (getDiff: 'a -> 'a -> float) (addToXValue : 'a -> float -> 'a) (borderpadding : int) (internalPaddingMethod: InternalPaddingMethod) =
            let rnd = System.Random()
            let n = data.Length
            ///minimal x_Value
            let minX = data |> Array.head |> fst
            ///minimal y_Value
            let maxX = data |> Array.last |> fst
            ///average spacing of the data points
            let avgSpacing = (getDiff maxX minX) / (float n)
            ///adds 'borderpadding' number of random data points to the left
            let leftPadding     = 
                Array.init borderpadding (fun i -> 
                    let paddX = addToXValue minX (- (float i + 1.) * minDistance)
                    let paddY = snd data.[rnd.Next(0,n)] //n+1
                    paddX,paddY)
                    |> Array.rev
            ///adds 'borderpadding' number of random data points to the rigth
            let rightPadding    = 
                Array.init borderpadding (fun i -> 
                    let paddX = addToXValue maxX ((float i + 1.) * minDistance)
                    let paddY = snd data.[rnd.Next(0,n)] //n+1
                    paddX,paddY
                    )

            let fillSpaceInBetween = 
                //interpolate the space between the two adjacent knots and add aditional points (number = (getDiff p1 p2 / minDistance) - 1)
                let linearInterpol current next numberOfPointsToAdd xSpacing =
                    let m =
                        let deltaX = getDiff (fst next) (fst current)
                        let deltaY = snd next - snd current
                        deltaY/deltaX
                    //number of points to add between the adjacent points, that the distance is < minDistance
                    let pointsToAdd =
                        [1 .. int numberOfPointsToAdd]
                        |> List.map (fun interval -> 
                            let x = addToXValue (fst current) (float interval * xSpacing)
                            let y = snd current + (m * float interval * xSpacing)
                            x,y
                            )
                    pointsToAdd
                let rec loop i acc = 
                    if i = n-1 then
                        acc 
                        |> List.rev
                        |> List.concat
                    else
                        let current = data.[i]
                        let next = data.[i+1]
                        let diff = Math.Abs (getDiff (fst current) (fst next))
                        if diff > minDistance then
                            let numberOfPointsToAdd = (diff / minDistance) - 1. |> floor
                            let xSpacing = diff / (numberOfPointsToAdd + 1.)
                            //if there is a huge gap, then do not use linear interpolation, but enter random values, nan or add no points
                            if diff > maxDistance then
                                match internalPaddingMethod with
                                | Random -> 
                                    let pointsToAdd =
                                        [1 .. int numberOfPointsToAdd]
                                        |> List.map (fun interval -> 
                                            let x = addToXValue (fst current) (float interval * xSpacing)
                                            let y = data.[rnd.Next(0,n)] |> snd
                                            x,y
                                            )
                                    loop (i+1) (pointsToAdd::[current]::acc) //add random             
                                | NaN -> 
                                    let pointsToAdd =
                                        [1 .. int numberOfPointsToAdd]
                                        |> List.map (fun interval -> 
                                            let x = addToXValue (fst current) (float interval * xSpacing)
                                            let y = nan
                                            x,y
                                            )
                                    loop (i+1) (pointsToAdd::[current]::acc) //add nan
                                | Delete -> 
                                    loop (i+1) ([current]::acc)              //delete values
                                | _ ->
                                    let pointsToAdd = linearInterpol current next numberOfPointsToAdd xSpacing                              
                                    loop (i+1) (pointsToAdd::[current]::acc) 

                            else
                                //if there is no big gap, interpolate the gap and add interpolating points
                                let pointsToAdd = linearInterpol current next numberOfPointsToAdd xSpacing
                                loop (i+1) (pointsToAdd::[current]::acc) 

                        else
                            loop (i+1) ([current]::acc)
                loop 0 []
                |> Array.ofSeq
            [leftPadding;fillSpaceInBetween;rightPadding] |> Array.concat


    ///Continuous wavelet transformation on non discrete data
    module CWT = 

        ///getDiff: calculates the time span between the two events as total minutes (float)
        let private calcTimeSpan (a: DateTime) (b: DateTime) =
            a - b
            |> fun x -> x.TotalMinutes 

        ///addToXValue: adds minutes to the date
        let private addMinToDateTime (dt: DateTime) (minutes: float) =
            dt.AddMinutes(minutes)

        ///addToXValue: adds minutes to the date
        let private addHourToDateTime (dt: DateTime) (hours: float) =
            dt.AddHours(hours)        

        ///calculates the continuous wavelet transform: 
        ///data: data to transform (x_Value,y_Value) [];
        ///getDiff: get the difference in x_Values as float representation (if 'a is float then (-))
        ///borderpadding: define the number of points padded to the beginning and end of the data (has to be the same as used in padding)
        ///ricker: wavelet
        let cwt (data : ('a * float) []) (getDiff: 'a -> 'a -> float) (borderpadding : int) (ricker: Ricker) =
            let n = data.Length
            let rickerPadd = ricker.PaddingArea

            //for every point in the range of the original data perform a convolution with a wavelet and calculate
            //the correlation value at that particular time point
            [|borderpadding .. (n-borderpadding-1)|]
            |> Array.map (fun i -> 
                let (currentX,currentY) = data.[i]
                //calculates the product at x = 0, so the current data point
                let transformAtX = ricker.RickerFun 0. * currentY
                //calculates sum of products on the right side of the current data point
                let rec rightSide iR acc =
                    let (nextRightX,nextRightY) = data.[i+iR]
                    let diff = getDiff nextRightX currentX 
                    if diff > rickerPadd then
                        acc
                    else    
                        rightSide (iR + 1) (acc + ((ricker.RickerFun diff) * nextRightY))
                //calculates sum of products on the left side of the current data point
                let rec leftSide iL acc = 
                    let (nextLeftX,nextLeftY) = data.[i+iL]
                    let diff = getDiff currentX nextLeftX
                    if diff > rickerPadd then 
                        acc
                    else 
                        leftSide (iL - 1) (acc + ((ricker.RickerFun (- diff)) * nextLeftY))
                
                let correlationValue = 
                    (rightSide 1 0.) + (leftSide -1 0.) + transformAtX
                currentX,correlationValue / (Math.Sqrt (Math.Abs(ricker.Scale)))
                )

        ///all existing sensors
        type Trace =
            | T1
            | T2
            | T3
            | T4
            | T5
            | T6
            | Light
            | Rain

        ///takes dates in the form "yyMMddHHmmss" and a trace to generate traces of the rawdata, and the wavelet transformed data
        let transformTemperatureData fromDate toDate (trace: Trace) =
            let data = Database.getDbData fromDate toDate
            let (traceName,singleTrace) = 
                match trace with
                | T1 ->     data.DataT1    |> Array.ofList |> fun x -> "T1",x
                | T2 ->     data.DataT2    |> Array.ofList |> fun x -> "T2",x
                | T3 ->     data.DataT3    |> Array.ofList |> fun x -> "T3",x
                | T4 ->     data.DataT4    |> Array.ofList |> fun x -> "T4",x
                | T5 ->     data.DataT5    |> Array.ofList |> fun x -> "T5",x
                | T6 ->     data.DataT6    |> Array.ofList |> fun x -> "T6",x
                | Light ->  data.DataLight |> Array.ofList |> fun x -> "Light",x  
                | Rain ->   data.DataRain  |> Array.ofList |> fun x -> "Rain",x
            ///contains the padded data
            let paddedData =
                //to decrease computation time, all data points are rounded to half hours.
                //if multiple values exist, calculate the arithmetic mean of all.
                let roundToHalfHours (arr : (System.DateTime * float)[]) =
                    arr
                    |> Array.map (fun (d,x) ->
                        let seconds = float d.Second
                        let roundTo30min = 
                            let diffToNext = (d.Minute%30)
                            if diffToNext < 15 then - diffToNext else 30 - diffToNext
                        d.AddMinutes(float roundTo30min).AddSeconds(- seconds),x)
                    |> Array.groupBy (fun (d,x) -> d)
                    |> Array.map (fun (groupindex,group) -> groupindex,group |> Array.averageBy (fun (date,x) -> x))
                    |> Array.sortBy fst 

                //padd the reduced data with minimal datadistance of 30 min, maxGap of 1 day and borderpadding sufficient for a one-year wavelet (minutes of a year / 4.)
                singleTrace
                |> roundToHalfHours
                |> fun x -> 
                    Padding.padding x 30. 1440. calcTimeSpan addMinToDateTime 1008000 Padding.InternalPaddingMethod.LinearInterpolation //maxGap = 1 Day; borderpadding sufficient for Ricker 144000. (quarter year)
            //get the correlation values of the specified trace with a defined set of wavelet scales
            let waveletTransformedData =
                let rickerArr =    
                    //corresponds to following frequencies [days]: 0.1, 0.1, 0.2, 0.4, 0.4, 0.5, 0.5, 0.6, 0.7, 1.0, 1.2, 1.5, 1.9, 2.4, 3.0, 3.8, 4.6, 5.8, 7.0, 8.3, 10.4, 12.5, 15.4, 20.8, 29.2, 41.7
                    [|2.;3.;4.;6.;8.;11.;13.;16.;19.;24.;29.;36.;45.;58.;72.;90.;110.;140.;170.;200.;250.;300.;370.;500.;700.;1000.|]//;1500.;2500.;3800.;5000.;7000.;9000.|]
                    |> Array.map (fun x -> createRicker(x * 60. / 4.))
                    |> Array.rev
                rickerArr
                |> Array.map (cwt paddedData calcTimeSpan 1008000)

            ((traceName,singleTrace),waveletTransformedData)



module Charting =
   
    ///generates a 'from-to' chart (dataformat: "yyMMddHHmmss")
    let chart fromDate toDate =
        //define a nice looking axis 
        let myAxis() =
            Axis.LinearAxis.init(Mirror=StyleParam.Mirror.All,Ticks=StyleParam.TickOptions.Inside,Showgrid=true,Showline=true)
        //get the data that should be plotted
        let chartData = Database.getDbData fromDate toDate

        let finalChart =
            let createTemperatureChart name list = 
                list 
                |> List.rev 
                |> fun d -> Chart.Line(d,Name = name)
                |> Chart.withAxisAnchor(Y=1)
            let chartT1    = chartData.DataT1    |> createTemperatureChart "T1" 
            let chartT2    = chartData.DataT2    |> createTemperatureChart "T2" 
            let chartT3    = chartData.DataT3    |> createTemperatureChart "T3" 
            let chartT4    = chartData.DataT4    |> createTemperatureChart "T4" 
            let chartT5    = chartData.DataT5    |> createTemperatureChart "T5" 
            let chartT6    = chartData.DataT6    |> createTemperatureChart "T6" 
            let chartLight = chartData.DataLight |> List.rev |> fun d -> Chart.Line(d,Name = "Light"        ) |> Chart.withAxisAnchor(Y=2)
            let chartRain  = chartData.DataRain  |> List.rev |> fun d -> Chart.Line(d,Name = "Precipitation") |> Chart.withAxisAnchor(Y=3)

            //combine all charts and add styling
            Chart.Combine[chartT1;chartT2;chartT3;chartT4;
                            chartT5;chartT6;chartLight;chartRain]
            //transform Axis
            |> Chart.withX_Axis(myAxis())  
            |> Chart.withY_Axis(myAxis())
            //define the date axis
            |> Chart.withX_AxisStyle("Date",Domain=(0., 0.85),Showgrid=false)
            //define the temperature axis in a range from -20C to 40C
            |> Chart.withY_AxisStyle("Temperature [°C]",            MinMax=(-20.,40.),    Side=StyleParam.Side.Left,Id=1)
            //define the light intensity axis in a range from -750 to 5000
            |> Chart.withY_AxisStyle("Light intensity",             MinMax=(-750.,5000.),Showgrid=false,  Side=StyleParam.Side.Right,Id=2,Overlaying=StyleParam.AxisAnchorId.Y 1)
            //define the rain axis in a range from 0 to 35 mm/m^2/10min
            |> Chart.withY_AxisStyle("Precipitation [mm/m^2/10min]",MinMax=(0.,35.),Showgrid=false,    Side=StyleParam.Side.Right,Id=3,Overlaying=StyleParam.AxisAnchorId.Y 1,Position=0.9)
            //define the title from the date range
            |> Chart.withTitle (sprintf "data from %s to %s" ((string fromDate).[0..5]) ((string toDate).[0..5]))
            |> Chart.withSize(1200.,700.)
            |> GenericChart.toEmbeddedHTML
            //to write the chart as html
            //|> fun x -> System.IO.File.WriteAllLines (@"C:\Users\bvenn\Documents\Projects\SFB\AlgaeWatch\FSharpChallenge\oldPlotSmall.html",[|x|])
        finalChart        

    //get the wavelet transformed data and plot them together with the original raw trace
    let waveletChart fromDate toDate (trace: Processing.CWT.Trace) yAxisName =
        let yAxis() =
            Axis.LinearAxis.init(Mirror=StyleParam.Mirror.All,Ticks=StyleParam.TickOptions.Inside,Showgrid=true,Showline=true)
        
        let rownames = 
            [|"0.08";"0.13";"0.17";"0.25";"0.33";"0.46";"0.54";"0.67";"0.79";"1.00";"1.21";"1.50";"1.88";"2.42";"3.00";"3.75";
             "4.58";"5.83";"7.08";"8.33";"10.42";"12.50";"15.42";"20.83";"29.17";"41.67";|]
            |> Array.map (fun x -> x + " days")
            |> Array.rev

        let ((tracename,traceRaw),traceProcessed) = Processing.CWT.transformTemperatureData fromDate toDate trace

        //CWT-chart
        let heatmap = 
            let lables = 
                traceProcessed.[0] |> Array.map fst
            traceProcessed
            |> Array.map (fun ia -> ia |> Array.map snd)
            |> fun x ->  Chart.Heatmap(x,Colorscale=StyleParam.Colorscale.Portland,ColNames=lables,RowNames=rownames,Showscale=false)
            |> Chart.withAxisAnchor(X=1)
            |> Chart.withAxisAnchor(Y=1)

        //Rawchart
        let rawChart = 
            Chart.Line (traceRaw,Color = "#1f77b4",Name = "raw")
            |> Chart.withAxisAnchor(X=2)
            |> Chart.withAxisAnchor(Y=2) 
        //combine the charts and add additional styling (see above)
        Chart.Combine([heatmap;rawChart])
        |> Chart.withX_AxisStyle("Date",Side=StyleParam.Side.Bottom,Id=2,Showgrid=false)
        |> Chart.withX_AxisStyle("", Side=StyleParam.Side.Top, Showline=false,Showgrid=false, Id=1,Overlaying=StyleParam.AxisAnchorId.X 2)
        |> Chart.withY_AxisStyle(yAxisName, MinMax=(-25.,40.), Side=StyleParam.Side.Left,Id=2)
        |> Chart.withY_AxisStyle("Correlation", MinMax=(25.,-50.),Showline=false,Showgrid=false, Side=StyleParam.Side.Right,Id=1,Overlaying=StyleParam.AxisAnchorId.Y 2)
        |> Chart.withSize(1200.,700.)
        |> Chart.withLegend true
        |> Chart.withTitle (sprintf "wavelet transformation for %s from %s to %s" tracename ((string fromDate).[0..5]) ((string toDate).[0..5]))
        |> Chart.withX_Axis (yAxis())
        |> Chart.withY_Axis (yAxis())
        |> GenericChart.toEmbeddedHTML



module RainData =
    ///stores information about the rain data from the ftp server
    type Rain = {
        Year      : int
        Month     : int
        Day       : int
        Hour      : int
        Minute    : int
        Hhmm      : string
        Summertime: bool
        Yyyymmdd  : string
        Quality   : int
        Amount    : float
        Date      : DateTime
        }

    ///create the Rain type
    let createRain year month day hour minute hhmm summertime yyyymmdd quality amount date= 
        {
        Year      = year
        Month     = month
        Day       = day
        Hour      = hour
        Minute    = minute
        Hhmm      = hhmm
        Summertime= summertime
        Yyyymmdd  = yyyymmdd
        Quality   = quality
        Amount    = amount
        Date      = date
        }
    
    ///downloads the latest rain data from the "Deutscher Wetterdienst". The sensor station is located in direct neighbourhood to the pond where the temperaturedata is measured
    let downloadRainData() =
        //define source directory for the raindata
        let src = __SOURCE_DIRECTORY__ + @"\content\raindata\"
        //define destination directory
        let destination = (src + @"ftpRainData.zip")
        //list all files in raindata directory
        let filelist = System.IO.Directory.GetFiles(src + @"ftpRainData", "*.txt")
        //delete all present files (files are processed and there is no need to keep them)
        filelist |> Array.map IO.File.Delete |> ignore
        //adress from Deutscher Wetterdienst (DWD)
        //let ftpAdress = @"ftp://ftp-cdc.dwd.de/pub/CDC/observations_germany/climate/10_minutes/precipitation/recent/10minutenwerte_nieder_02486_akt.zip"
        let ftpAdress = @"ftp://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/precipitation/recent/10minutenwerte_nieder_02486_akt.zip"
        //download txt.zip
        let req = FtpWebRequest.Create(ftpAdress) :?> FtpWebRequest
        req.Method <- WebRequestMethods.Ftp.DownloadFile
        let res = req.GetResponse()  :?> FtpWebResponse
        let stream = res.GetResponseStream()
        let buffer : byte [] = Array.zeroCreate 1024
        let fileStream = new FileStream(destination,FileMode.Create,FileAccess.Write)
        let rec loop amountRead =
            if amountRead > 0 then
                fileStream.Write (buffer, 0, amountRead)
                loop (stream.Read(buffer,0,buffer.Length))
            else
                fileStream.Dispose()
                stream.Close()
                res.Close()
        loop (stream.Read(buffer,0,buffer.Length))      
        //extract the .zip folder to the destination directory
        Compression.ZipFile.ExtractToDirectory(destination,destination.Substring(0,destination.Length-4))

    ///downloads the latest rain data and extracts all rain events since fromDate      
    let readRainData fromDate = 

        //downloads the latest rain data
        downloadRainData()
        //German summertime is set as default time
        let summertime = true
        let rainDataFilePath = 
            let src = __SOURCE_DIRECTORY__ + @"\content\raindata\"
            System.IO.Directory.GetFiles(src + @"ftpRainData", "*.txt") 
            |> Array.head    
        let rainData =
            System.IO.File.ReadAllLines(rainDataFilePath)
            |> Seq.map (fun x -> 
                x.Split([|';'|])
                |> Array.collect (fun k -> 
                    k.Split([|' '|]) 
                    |> Array.filter (fun x -> x <> "")))
            |> Seq.tail
            |> Array.ofSeq
            |> fun allRainData -> 
                    //finds index of the first occurence of the specified date
                    let index =  Seq.tryFindIndex (fun (l:string[]) -> l.[1].[0..7] = fromDate) allRainData
                    match index with
                    | Some i -> 
                        allRainData.[i..]
                    | None -> allRainData.[(allRainData.Length - 1000) .. ]                    
            //filter data where there was no rain or where the quality was not 3 (highest precision)            
            |> Array.filter (fun x -> 
                x.[4] <> "0.00" || x.[2] <> "3")
        //convert data to Rain record type        
        rainData
        |> Array.map (fun x -> 
            let datestring = x.[1] 
            //the data given by the DWD specifies the rain fallen during the last 10 minutes, so 10 minutes are subtracted            
            let date =      
                //convert UTC to German time
                let utcTime = System.DateTime.ParseExact(datestring,"yyyyMMddHHmm",null).AddMinutes(-10.)
                if summertime then 
                    utcTime.AddHours(2.)
                else utcTime.AddHours(1.)                  
            let yyyyMMdd =  datestring.[0..7] 
            let hhmm =      datestring.[8..11]
            let quality =   int x.[2]
            let amount =    float x.[4]
            let rt = createRain date.Year date.Month date.Day date.Hour date.Minute hhmm summertime yyyyMMdd quality amount date
            rt)


    //automatically fetches the latest rain data since the last time fetched and updates the temperature measurement items int the data base
    let updateTemperatureDataWithRain() =
        //updates the rainData database table with the date of the last fetched ftp-Data (today - 15 days to have a overhead)
        let updateRainFetchDate() =
            let date = System.DateTime.Now.AddDays(-15.).ToString("yyMMddHHmmss") |> int64
            use cn = Database.getCn()
            cn.Open()
            let updateString = 
                "UPDATE RainData SET LastFetch = @fetchdate WHERE ID = '1'"
            use cmd  = new SQLiteCommand(updateString, cn)
            cmd.Parameters.Add("@fetchdate", System.Data.DbType.Int64) |> ignore
            cmd.Parameters.["@fetchdate"].Value <- date
            cmd.ExecuteNonQuery() |> ignore 
            cn.Close()    

        //updates a single temperature measurement event with the given rain amount
        let updateTempEventWithRain (date: int64) (rain:float) (cn: SQLiteConnection)=
            try
                let updateString = 
                    "UPDATE TemperatureData SET RainData = @rainAmount WHERE DateTime = @date"

                let cmd  = new SQLiteCommand(updateString, cn)
                cmd.Parameters.Add("@rainAmount", System.Data.DbType.Double) |> ignore
                cmd.Parameters.Add("@date", System.Data.DbType.Int64) |> ignore

                cmd.Parameters.["@rainAmount"].Value <- rain
                cmd.Parameters.["@date"].Value   <- date

                cmd.ExecuteNonQuery()   |> ignore   
            with e as exn -> printfn "%s" exn.Message 

        use cn = Database.getCn()
        cn.Open()

        let dateOfLastRainFetch = 
            let querystring = 
                "SELECT LastFetch FROM RainData WHERE ID = '1'"
            let cmd = new SQLiteCommand(querystring, cn)
            use reader = cmd.ExecuteReader()
            reader.Read() |> ignore
            reader.GetInt64(0)
        
        //get rainData from weather station in direct neighbourhood of the sensor
        let rainData = 
            //add a buffer of 30 days since the last RainFetch date to ensure that no rain is missed.
            //The quality of the rain data is checked by the Deutscher Wetterdienst ~4 days after data collection, so thereis a delay.
            //Should be of the form: yyyyMMddHHmm
            let date =
                "20" + (dateOfLastRainFetch |> string).[0..5]
            updateRainFetchDate()
            readRainData date


        //collect every entry since the last RainData update
        let temperatureItems =
            Database.getDbItem dateOfLastRainFetch (System.DateTime.Now.ToString("yyMMddHHmmss") |> int64) cn

        //searches for rain that has fallen during the specified temperature measurement
        let tempEventsWithRain =
           
            temperatureItems
            |> List.map (fun temperatureEvent -> 
                let dateOfTemperatureMeasurement = System.DateTime.ParseExact((string temperatureEvent.DateTime),"yyMMddHHmmss",null)
                printfn "tempItems: %s" (dateOfTemperatureMeasurement.ToShortDateString())    
                let rainDataOfMeasurement =
                    rainData
                    |> Array.tryFind (fun rainEvent -> 
                        let timeDifference = (dateOfTemperatureMeasurement - rainEvent.Date).TotalMinutes
                        timeDifference < 10. && timeDifference >= 0.
                        )
                match rainDataOfMeasurement with
                | Some rain -> temperatureEvent.DateTime,rain.Amount
                | None -> temperatureEvent.DateTime,0.)       
            //filter every temperature event where the rainamount was > 0.
            |> List.filter (fun (_,rainAmount) -> rainAmount <> 0.)  

        tempEventsWithRain
        |> List.iter (fun (tempEvent,rainAmount) -> 
            updateTempEventWithRain tempEvent rainAmount cn)
        cn.Close()
            
    

module Care =
    //for initializing the data base
    let insertTemperatureEventsFromFile filepath = 
        use cn = Database.getCn()
        cn.Open() 
        System.IO.File.ReadAllLines(filepath)
        |> Array.tail
        |> Array.indexed
        |> Array.filter (fun (i,x) -> i%2 = 0)
        |> Array.map snd
        //|> fun x -> x.[0..10]
        |> Array.mapi (fun i x ->   
            printfn "%i" i
            let tmp = x.Split([|'\t'|])
            let date = tmp.[0].Replace("/",".")
            let jsonFormat = {
                D   = date
                T1  = tmp.[1] |> float
                T2  = tmp.[2] |> float
                T3  = tmp.[3] |> float
                T4  = tmp.[4] |> float
                T5  = tmp.[5] |> float
                T6  = tmp.[6] |> float
                L   = tmp.[7] |> float
                }        
            Database.insertDbItemWithRain jsonFormat (tmp.[8] |> float) cn) |> ignore
        cn.Close()         


let config =
    { defaultConfig with
          homeFolder = Some publicPath
          bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") port ] }


let loggingAPI : ILoggingAPI= 
    {
    //get overview plot
    GetPlot = fun () -> 
        async {return (System.IO.File.ReadAllLines(oldPlotPath + @"/lastYearSmall.txt") |> String.concat "\n")}
    //get 'from-to' plot
    GetPlotChunk = fun date -> 
        let dateFrom = 
            date.Split([|'-'|])
            |> fun x -> x.[0].Replace(" ","") + "000000" |> int64
        let dateTo = 
            date.Split([|'-'|])
            |> fun x -> x.[1].Replace(" ","") + "000000" |> int64        
        async {return (Charting.chart dateFrom dateTo)}
    //get wavelet plot
    GetPlotWavelet = fun (date,trace)-> 
        let dateFrom = 
            date.Split([|'-'|])
            |> fun x -> x.[0].Replace(" ","") + "000000" |> int64
        let dateTo = 
            date.Split([|'-'|])
            |> fun x -> x.[1].Replace(" ","") + "000000" |> int64        
        match trace with
        | "T1" -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.T1 "Temperature [Celsius]")} 
        | "T2" -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.T2 "Temperature [Celsius]")} 
        | "T3" -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.T3 "Temperature [Celsius]")} 
        | "T4" -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.T4 "Temperature [Celsius]")} 
        | "T5" -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.T5 "Temperature [Celsius]")} 
        | "T6" -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.T6 "Temperature [Celsius]")} 
        | "Light" -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.Light "Light [AU]")} 
        | _ -> async {return (Charting.waveletChart dateFrom dateTo Processing.CWT.Rain "Rain l/m^2/10min")} 
    //get JSON string transmitted from arduino and insert it into the database        
    ArduinoPost = fun transmit -> 
        async {return (Database.insertDbItem transmit)}
    //fetch the latest rain data
    FetchRain = fun () ->
        async {return (RainData.updateTemperatureDataWithRain())}    
}
 


let webApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.withDiagnosticsLogger(fun x -> if x.Length < 10000 then printfn "%s" x else (printfn "too many lines"))
    |> Remoting.fromValue loggingAPI
    |> Remoting.buildWebPart

let webApp =
    choose [
        webApi
        path "/" >=> browseFileHome "index.html"
        browseHome
        RequestErrors.NOT_FOUND "Not found!"
    ]


startWebServer config webApp