using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
namespace YouTubeWidget;
// Owns only this widget's process. Explicit exit and Windows logoff stop recovery.
internal sealed class RuntimeSupervisor:IDisposable {
 readonly EventWaitHandle pulse,stop;readonly DispatcherTimer timer;bool disposed;
 RuntimeSupervisor(string token,bool fixture=false){
  pulse=new(false,EventResetMode.AutoReset,"Local\\YouTubeWidget.Pulse."+token);
  stop=new(false,EventResetMode.ManualReset,"Local\\YouTubeWidget.Stop."+token);
  timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(5)};timer.Tick+=(_,_)=>pulse.Set();timer.Start();pulse.Set();
  var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
  foreach(var arg in new[]{"--watchdog",Environment.ProcessId.ToString(),token,Preferences.Folder})start.ArgumentList.Add(arg);
  if(fixture)start.ArgumentList.Add("fixture");
  Process.Start(start)?.Dispose();
 }
 public static RuntimeSupervisor Start()=>new(Guid.NewGuid().ToString("N"));
 internal static void Fixture(string folder,bool graceful){
  Preferences.TestFolder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);
  var marker=Path.Combine(folder,"fixture-started.json");
  if(File.Exists(marker)){File.WriteAllText(Path.Combine(folder,"fixture-recovered.json"),Environment.ProcessId.ToString());return;}
  File.WriteAllText(marker,Environment.ProcessId.ToString());var app=new System.Windows.Application();
  using var supervisor=new RuntimeSupervisor(Guid.NewGuid().ToString("N"),true);
  var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};timer.Tick+=(_,_)=>{timer.Stop();if(graceful){supervisor.Dispose();app.Shutdown();}else Environment.Exit(17);};timer.Start();app.Run();
 }
 public void Dispose(){if(disposed)return;disposed=true;timer.Stop();stop.Set();pulse.Dispose();stop.Dispose();}
 internal static void Watch(string[] args){
  if((args.Length!=4&&!(args.Length==5&&args[4]=="fixture"))||!int.TryParse(args[1],out var pid)||!Guid.TryParseExact(args[2],"N",out _))return;
  try{
   using var parent=Process.GetProcessById(pid);
   if(!string.Equals(parent.MainModule?.FileName,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase))return;
   using var pulse=EventWaitHandle.OpenExisting("Local\\YouTubeWidget.Pulse."+args[2]);
   using var stop=EventWaitHandle.OpenExisting("Local\\YouTubeWidget.Stop."+args[2]);
   long lastPulse=Environment.TickCount64,lastLoop=lastPulse;
   while(true){
    if(stop.WaitOne(1000))return;
    long now=Environment.TickCount64;
    // A suspended computer gets a fresh grace period after wake.
    if(now-lastLoop>15000)lastPulse=now;lastLoop=now;
    if(pulse.WaitOne(0))lastPulse=now;
    if(parent.HasExited)break;
    if(now-lastPulse<90000)continue;
    if(stop.WaitOne(0))return;
    parent.Kill();if(!parent.WaitForExit(10000))return;break;
   }
   if(stop.WaitOne(1500))return;
   string folder=Path.GetFullPath(args[3]);Directory.CreateDirectory(folder);
   var path=Path.Combine(folder,"recovery.log");
   var recent=File.Exists(path)?File.ReadAllLines(path).Where(line=>DateTimeOffset.TryParse(line.Split('|')[0],out var at)&&at>DateTimeOffset.UtcNow.AddMinutes(-10)).ToArray():Array.Empty<string>();
   if(recent.Length>=3)return;
   File.AppendAllText(path,DateTimeOffset.UtcNow.ToString("O")+"|Recovered widget process "+pid+Environment.NewLine);
   var restart=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
   if(args.Length==5){restart.ArgumentList.Add("--supervisor-fixture");restart.ArgumentList.Add(folder);}else restart.ArgumentList.Add("--startup");
   Process.Start(restart)?.Dispose();
  }catch(Exception error){App.Log(error);}
 }
}
