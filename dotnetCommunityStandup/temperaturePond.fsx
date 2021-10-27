#r "nuget: Deedle"
#r "nuget: FSharp.Stats"
#r "nuget: Plotly.NET, 2.0.0-beta3"
#r "nuget: Plotly.NET.Interactive, 2.0.0-alpha5"

open System
open Deedle
open FSharp.Stats
open Plotly.NET
open Plotly.NET.StyleParam

///////////////////
/// import data
let pondData = __SOURCE_DIRECTORY__ + @"pondDataKaiserslautern_hourly.tsv"

let df = 
    Frame.ReadCsv(pondData,hasHeaders=true,separators="\t")     
    |> Frame.indexRowsUsing (fun os -> os.GetAs<System.DateTime>"DateTime") 

df.Print()



///////////////////
/// isolate sensor data
/// use the sensor name to isolate a tuple sequence of date and temperature reading
let getSingleSensorData sensorName :(DateTime*float) []=
    df
    |> Frame.indexRowsUsing (fun os -> os.GetAs<System.DateTime>"DateTime") 
    |> Frame.getCol sensorName
    |> Series.observations
    |> Array.ofSeq


// isolate data from sensors
let bankUpData  = getSingleSensorData "bankUp_DegC"
let midDownData = getSingleSensorData "midDown_DegC"

let temperatureChart =
    [
        Chart.Area(bankUpData,"bankUp")
        Chart.Line(midDownData,"midDown")
    ]
    |> Chart.Combine
    |> Chart.withTemplate ChartTemplates.dark
    |> Chart.withY_AxisStyle "Temperature [°C]"
    |> Chart.withSize (1200.,600.)
    |> Chart.withTitle "Water temperature in pond at University of Kaiserslautern (49.42634, 7.7558)"

temperatureChart |> Chart.Show



///////////////////
/// padding data
open FSharp.Stats.Signal
open FSharp.Stats.Signal.Padding
open FSharp.Stats.Signal.Padding.HelperFunctions

// interpolate data point y-values when small gaps are present
let innerPadMethod = InternalPaddingMethod.LinearInterpolation

/// take random data point y-values when huge gaps are between data points
let hugeGapPadMethod = HugeGapPaddingMethod.LinearInterpolation

// pad the start and end of the signal with random data points
let borderPadMethod = BorderPaddingMethod.Random

//let averageSpacing = getAvgSpacing bankUpData Time.getDiffMinutes

// the maximal distance that is allowed between data points is the minimum spacing divided by 2
let minDistance = 60.

// gap size from which on hugeGapPaddingMethod is applied
let maxDistance = 15. * 60.

// since were dealing with DateTime, functions must be defined that calculate an addition an substraction of two time intervals.
let getDiff = Time.getDiffMinutes
let addXValue = Time.addToXValueMinutes

// number of datapoints the dataset gets expanded to the left and to the rigth
let borderpadding = 15000

// pad the data
let paddedData = Padding.pad bankUpData minDistance maxDistance getDiff addXValue borderpadding borderPadMethod innerPadMethod hugeGapPadMethod

let paddedDataChart =
    [
    Chart.Line (paddedData,Name="paddedData")
    Chart.Line (bankUpData,Name = "rawData") |> Chart.withMarkerStyle(4)
    ]
    |> Chart.Combine
    |> Chart.withY_AxisStyle "Temperature [°C]"
    |> Chart.withTemplate ChartTemplates.dark
    |> Chart.withSize(1200.,550.)

paddedDataChart |> Chart.Show


///////////////////
/// Wavelet transform
// Array containing wavelets of all scales that should be investigated. The propagated frequency corresponds to 4 * Ricker.Scale
let rickerArray = 
    [|3.66 .. 0.33 .. 19.|] 
    |> Array.map (fun x -> 
        Wavelet.createRicker (x**4.)
        )

///the data already was padded with 1000 additional datapoints in the beginning and end of the data set (see above). 
///Now it is transformed with the previous defined wavelets.
let transformedData = 
    rickerArray
    |> Array.map (fun wavelet -> 
        ContinuousWavelet.transform paddedData getDiff 15000 wavelet
        )

///combining the raw and transformed data in one chart
let combinedChart =

    let rownames = 
        rickerArray
        |> Array.map (fun x -> sprintf "%.1f days" (x.Scale * 4. / 1440. ))

    // raw data chart
    let rawChart = 
        Chart.Area (bankUpData,Color = "#1f77b4",Name = "rawData")
        |> Chart.withAxisAnchor(X=2)
        |> Chart.withAxisAnchor(Y=2) 

    // CWT-chart
    let heatmap =
        let colNames = 
            transformedData.[0] 
            |> Array.map fst
        transformedData
        |> JaggedArray.map snd
        |> fun x -> Chart.Heatmap(x,ColNames=colNames,RowNames=rownames,Showscale=false,Colorscale=StyleParam.Colorscale.Portland)
        |> Chart.withAxisAnchor(X=1)
        |> Chart.withAxisAnchor(Y=1)

    // combine the charts and add additional styling
    Chart.Combine([heatmap;rawChart])
    |> Chart.withX_AxisStyle("Time",Side=Side.Bottom,Id=2,Showgrid=false)
    |> Chart.withX_AxisStyle("", Side=Side.Top,Showgrid=false, Id=1,Overlaying=AxisAnchorId.X 2)
    |> Chart.withY_AxisStyle("Temperature", MinMax=(-25.,35.), Side=Side.Left,Id=2)
    |> Chart.withY_AxisStyle("frequency", MinMax=(0.,125.),Showgrid=false, Side=Side.Right,Id=1,Overlaying=AxisAnchorId.Y 2)
    |> Chart.withLegend true
    |> Chart.withTemplate ChartTemplates.dark
    |> Chart.withTitle "Temperature in a pond at the University of Kaiserslautern"
    |> Chart.withSize(1200.,900.)
    

combinedChart |> Chart.Show