namespace SensorTestWear

open System
open System.Collections.Generic
open System.Linq
open System.Text
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Gms.Wearable
open Android.Support.Wearable.Views

[<Activity(Label = "Confirm")>]
type ConfirmationActivity() = 
    inherit Activity()

    let mutable fExec : unit->unit = fun () -> ()
    let mutable text  = ""
        
   
    interface DelayedConfirmationView.IDelayedConfirmationListener  with
        member x.OnTimerFinished(v) = fExec()
        member x.OnTimerSelected(v) = ()


    override x.OnCreate(bundle) =
        base.OnCreate(bundle)
        x.SetContentView(Resource_Layout.Confirmation)
        let dcv = x.FindViewById<DelayedConfirmationView>(Resource_Id.delayed_confirm)
        let tx  = x.FindViewById<TextView>(Resource_Id.confText)
        tx.Text <- text
        dcv.SetListener(x)
// Create your application here

