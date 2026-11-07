using System.Diagnostics;
var app=Path.Combine(AppContext.BaseDirectory,"WindowsYouTubeWidget.exe");
if(File.Exists(app))Process.Start(new ProcessStartInfo(app,"--startup"){UseShellExecute=false,CreateNoWindow=true})?.Dispose();
