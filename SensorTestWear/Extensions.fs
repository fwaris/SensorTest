module Extensions
let tag = "SensorTest"
let logI d = Android.Util.Log.Info(tag,d)  |> ignore
let logE d = Android.Util.Log.Error(tag,d) |> ignore


