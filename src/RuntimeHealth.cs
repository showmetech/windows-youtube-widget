using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 long lastHealth;bool recoveringPlayer;readonly Queue<DateTimeOffset> playerFailures=new();
 void WriteHealth(){
  if(Environment.TickCount64-lastHealth<5000)return;lastHealth=Environment.TickCount64;
  try{HandoffTransport.Write(Path.Combine(Preferences.Folder,"runtime-health.json"),new{At=DateTimeOffset.UtcNow,Pid=Environment.ProcessId,Version=typeof(App).Assembly.GetName().Version?.ToString(3),Responsive=true,Visible=IsVisible,TrayRegistered=tray?.TryBounds(out _)==true,PlayerReady=playerReady,Recovering=recoveringPlayer,TaskbarExcluded=NativePosition.TaskbarExcluded(this)});}catch(Exception error){App.Log(error);}
 }
 async Task RecoverPlayer(CoreWebView2ProcessFailedKind failure){
  if(exiting||recoveringPlayer)return;
  if(failure is not(CoreWebView2ProcessFailedKind.RenderProcessExited or CoreWebView2ProcessFailedKind.FrameRenderProcessExited or CoreWebView2ProcessFailedKind.BrowserProcessExited or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive))return;
  var now=DateTimeOffset.UtcNow;while(playerFailures.TryPeek(out var old)&&old<now.AddMinutes(-2))playerFailures.Dequeue();
  if(playerFailures.Count>=3){SetStatus("Não foi possível recuperar o player. Feche e abra o widget novamente.",true);return;}
  // An unresponsive notification can be transient under load. Wait before reloading.
  recoveringPlayer=true;
  try{
   if(failure==CoreWebView2ProcessFailedKind.RenderProcessUnresponsive){
    var probe=web.CoreWebView2.ExecuteScriptAsync("document.readyState");
    if(await Task.WhenAny(probe,Task.Delay(5000))==probe){await probe;return;}
   }
   playerFailures.Enqueue(now);Save();playing=false;ready=false;playerReady=false;EnableTransport();
   prefs.PreserveCurrentVideo=true;SetStatus("Recuperando o player…",true);
   if(failure==CoreWebView2ProcessFailedKind.BrowserProcessExited){web.Reset();await Task.Delay(1000);await InitPlayer();}
   else web.CoreWebView2.Reload();
   LogPlayer("recovery-"+failure);
  }catch(Exception error){App.Log(error);SetStatus("Não foi possível recuperar o player. Abra o widget novamente.",true);}
  finally{recoveringPlayer=false;}
 }
 internal async Task VerifyPlayerRecovery(){
  if(!App.Testing)return;
  for(int i=0;i<100&&!playerReady;i++)await Task.Delay(100);
  var id=current?.Id;var oldPid=web.CoreWebView2.BrowserProcessId;
  // Terminate only the browser process belonging to this isolated test WebView.
  using(var browser=System.Diagnostics.Process.GetProcessById((int)oldPid))browser.Kill();
  bool restored=false;for(int i=0;i<200;i++){await Task.Delay(100);if(playerReady&&current?.Id==id){restored=true;break;}}
  File.WriteAllText(Path.Combine(Preferences.Folder,"recovery-verification.json"),JsonSerializer.Serialize(new{Passed=restored,SameVideo=current?.Id==id,Paused=!playing,TaskbarExcluded=NativePosition.TaskbarExcluded(this),NewBrowser=restored&&web.CoreWebView2.BrowserProcessId!=oldPid}));
  ExitApp();
 }
}
