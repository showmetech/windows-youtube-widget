using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 bool autoplayBlocked;
 readonly List<string> playbackPermissions=new();
 internal async Task VerifyAudiblePlayback(){
  if(!App.Testing)throw new InvalidOperationException("Playback verification requires an isolated profile.");
  var checks=new Dictionary<string,bool>();string error="";
  async Task<bool> Wait(Func<Task<bool>> condition,int attempts=100){for(int i=0;i<attempts;i++){await Task.Delay(100);if(await condition())return true;}return false;}
  async Task<int> State()=>JsonSerializer.Deserialize<int>(await web.CoreWebView2.ExecuteScriptAsync("player.getPlayerState()"));
  async Task<double> Time()=>JsonSerializer.Deserialize<double>(await web.CoreWebView2.ExecuteScriptAsync("player.getCurrentTime()"));
  try{
   if(!await Wait(()=>Task.FromResult(playerReady),300))throw new TimeoutException("YouTube player did not become ready.");
   int videoArg=Array.IndexOf(args,"--playback-video");
   if(videoArg>=0&&videoArg+1<args.Length&&current?.Id!=args[videoArg+1]){await Load(Video.Parse(args[videoArg+1]));if(!await Wait(()=>Task.FromResult(playerReady),300))throw new TimeoutException("Test video did not become ready.");}
   var point=playButton.TranslatePoint(new Point(playButton.ActualWidth/2,playButton.ActualHeight/2),this);
   var hit=InputHitTest(point) as DependencyObject;
   checks["PlayHitTargetIsButton"]=hit==playButton||(hit!=null&&playButton.IsAncestorOf(hit));
   checks["StartsPausedWithSoundEnabled"]=await State() is not(1 or 3)&&!muted;
   if(args.Contains("--drop-first-play"))await web.CoreWebView2.ExecuteScriptAsync("window.probeOriginalPlay=player.playVideo.bind(player);window.probePlayCalls=0;player.playVideo=function(){if(++window.probePlayCalls>1)window.probeOriginalPlay();};");
   autoplayBlocked=false;playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   bool started=await Wait(async()=>autoplayBlocked||await State()==1,150);
   checks["FirstNativePlayStartsUnmuted"]=started&&!autoplayBlocked&&await State()==1&&!JsonSerializer.Deserialize<bool>(await web.CoreWebView2.ExecuteScriptAsync("player.isMuted()"));
   if(checks["FirstNativePlayStartsUnmuted"]){
    double before=await Time();await Task.Delay(1500);checks["PlaybackAdvances"]=await Time()>before+.5;
    playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["NativePauseStopsPlayback"]=await Wait(async()=>await State()==2);
    playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["NativeResumeStartsPlayback"]=await Wait(async()=>await State()==1)&&!muted;
    HideWidget();await Task.Delay(700);checks["HidePausesPlayer"]=await State()==2;
    ShowWidget();await Task.Delay(500);checks["ReopenStaysPaused"]=await State()==2;
    await web.CoreWebView2.ExecuteScriptAsync("window.probeCancelPlay=player.playVideo.bind(player);window.probeCancelCalls=0;player.playVideo=function(){if(++window.probeCancelCalls>1)window.probeCancelPlay();};");
    playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);HideWidget();await Task.Delay(1600);
    checks["HideCancelsPendingPlay"]=!PlaybackPending&&await State() is not(1 or 3);
    ShowWidget();await Task.Delay(500);checks["CancelledPlayDoesNotRestartOnReopen"]=await State() is not(1 or 3);
   }
  }catch(Exception e){error=e.ToString();checks["UnexpectedError"]=false;}
  finally{
   PausePlayer();Directory.CreateDirectory(Preferences.Folder);
   File.WriteAllText(Path.Combine(Preferences.Folder,"audible-playback-verification.json"),JsonSerializer.Serialize(new{At=DateTimeOffset.Now,Video=current?.Id,DroppedFirstPlay=args.Contains("--drop-first-play"),Checks=checks,AutoplayBlocked=autoplayBlocked,Permissions=playbackPermissions,Error=error,Passed=checks.Count>=7&&System.Linq.Enumerable.All(checks.Values,x=>x)},new JsonSerializerOptions{WriteIndented=true}));
   ExitApp();
  }
 }
}
