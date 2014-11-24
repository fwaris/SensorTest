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
open Android.Content.Res
open Android.Support.Wearable.Views
open Android.Gms.Wearable
open Android.Hardware

type Snsr = {Name: string; Desc:string}

type SimpleGridPagerAdapter(ctx:Context, fm:FragmentManager, data:Snsr array) =
    inherit FragmentGridPagerAdapter(fm)

    override x.GetFragment(row,col) =
        let fgmt = CardFragment.Create(data.[row].Name,data.[row].Desc)
        fgmt :> _

    override x.RowCount = data.Length
    override x.GetColumnCount(row) = 1

[<Activity(Label = "PageViewActivity", MainLauncher=true)>]
type PageViewActivity() = 
    inherit Activity()
    let mutable pager : GridViewPager = null
    let mutable res : Resources = null


    interface View.IOnApplyWindowInsetsListener with
        member x.OnApplyWindowInsets(view, insets) =
            // Adjust page margins
            // A little extra horizontal spacing between pages looks a but less crouded on a round display
            let round = insets.IsRound;
            let rowMargin = res.GetDimensionPixelOffset (Resource_Dimension.page_row_margin);
            let colMargin = res.GetDimensionPixelOffset (
                                if round then Resource_Dimension.page_column_margin_round else Resource_Dimension.page_column_margin);
            pager.SetPageMargins (rowMargin, colMargin);
            insets


    override x.OnCreate(bundle) = 
        base.OnCreate(bundle)
        x.SetContentView(Resource_Layout.Pager)
        let smgr = x.GetSystemService(Service.SensorService) :?> SensorManager
        let l = smgr.GetSensorList(SensorType.All) |> Seq.map (fun x-> {Name=x.Name; Desc=x.ToString()}) |> Seq.toArray
        res <- x.Resources
        pager <- x.FindViewById<GridViewPager>(Resource_Id.pager)
        pager.SetOnApplyWindowInsetsListener(x)
        pager.Adapter <- new SimpleGridPagerAdapter(x, x.FragmentManager, l)
        new Intent(x,typeof<HeartRateService>) |> x.StartService |> ignore
