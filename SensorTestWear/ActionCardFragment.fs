namespace SensorTestWear

open System
open System.Collections.Generic
open System.Linq
open System.Text


open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Util
open Android.Views
open Android.Widget
open Android.Support.Wearable.Views

type ActionCardFragment() = 
    inherit CardFragment()
    override x.OnCreate(savedInstanceState) = base.OnCreate(savedInstanceState)


    override x.OnCreateContentView(inflater,container,savedInstance) =
        let view = inflater.Inflate(Resource_Layout.CF1,container)
//        let isRunning = isServiceRunning "SensorTestWear.SensorService"
//        let btn = view.FindViewById<ToggleButton>(Resource_Id.actionButton)
//        btn.CheckedChange.Add(fun e -> 
//            if e.IsChecked then 
//                new Intent(x.Activity,typeof<SensorService>) |> x.Activity.StartService |> ignore
//            else
//                new Intent(x.Activity,typeof<SensorService>) |> x.Activity.StopService |> ignore
//                )
        view