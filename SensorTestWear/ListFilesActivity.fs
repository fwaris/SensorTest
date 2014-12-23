namespace SensorTestWear

open System
open Extensions
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Gms.Wearable
open Android.Hardware
open Android.Support.Wearable.Views
open Android.Graphics.Drawables
open Android.Hardware
open Android.Support.V7.Widget

type WearableListItemLayout(ctx:Context) =
    inherit FrameLayout(ctx) 

    [<DefaultValue>]
    val mutable circle : ImageView

    [<DefaultValue>]
    val mutable name   : TextView 

    interface WearableListView.IOnCenterProximityListener with
        member x.OnCenterPosition(b) = ()
           //x.circle.Animate().ScaleX(1.f).ScaleY(1.f).Alpha(1.f) |> ignore
            //x.name.Animate().ScaleX(1.f).ScaleY(1.f).Alpha(1.f) |> ignore
        member x.OnNonCenterPosition(b) =
            //x.circle.Animate().ScaleX(0.8f).ScaleY(0.8f).Alpha(0.6f) |> ignore
            x.name.Animate().Alpha(0.6f) |> ignore
    
    static member Create(ctx) =
        let vh = new WearableListItemLayout(ctx)
        let view = View.Inflate(ctx,Resource_Layout.ListItemText,vh)
        vh.circle <- view.FindViewById<ImageView>(Resource_Id.circle)
        vh.name <- view.FindViewById<TextView>(Resource_Id.name)
        vh


type MyAdapter(ctx,data:string array) =
    inherit RecyclerView.Adapter()
    override x.get_ItemCount() = data.Length

    override x.OnBindViewHolder(viewHolder, pos) = 
        let v = viewHolder.ItemView.JavaCast<WearableListItemLayout>()
        let name = data.[pos]
        v.name.Text <- name

    override x.OnCreateViewHolder(parent,viewType) =
        let view = WearableListItemLayout.Create(ctx)
        new WearableListView.ViewHolder(view) :>_ 

[<Activity(Label = "Files", MainLauncher = false)>]
type ListFilesActivity() = 
    inherit Activity()

    override x.OnCreate(bundle) = 
        base.OnCreate(bundle)
        x.SetContentView(Resource_Layout.FileList)
        let l = x.FindViewById<WearableListView>(Resource_Id.times_list_view)
        let data = [| for i in 1..10 -> sprintf "item %d" i |]
        let data = Storage.fileList()
        let adapter = new MyAdapter(x,data)
        l.SetAdapter(adapter)
 
// Create your application here

