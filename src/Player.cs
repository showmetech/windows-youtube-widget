using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 void PlayerCommand(string command){if(command=="toggle"){_ = TogglePlayback();return;}if(playerReady)web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{type=command}));Touch();}
 internal async Task InitPlayer() {
  try {
   await StoreIntegration.PrepareRuntime();
   var env=await CoreWebView2Environment.CreateAsync(File.Exists(Path.Combine(AppContext.BaseDirectory,"WebView2Runtime","msedgewebview2.exe"))?Path.Combine(AppContext.BaseDirectory,"WebView2Runtime"):null,Path.Combine(Preferences.Folder,"Browser"));await web.EnsureCoreWebView2Async(env);
   var core=web.CoreWebView2;core.ProcessFailed+=async(_,e)=>await RecoverPlayer(e.ProcessFailedKind);core.Settings.AreDevToolsEnabled=App.Testing;core.Settings.IsStatusBarEnabled=false;core.Settings.AreHostObjectsAllowed=false;
   core.PermissionRequested+=(_,e)=>{e.SavesInProfile=false;e.State=PlaybackPermissions.Decide(e.PermissionKind,e.Uri);if(App.Testing)playbackPermissions.Add(e.PermissionKind+":"+e.Uri+":"+e.State);};core.DownloadStarting+=(_,e)=>e.Cancel=true;
   core.SetVirtualHostNameToFolderMapping("com.bruno.youtube-widget",Path.Combine(AppContext.BaseDirectory,"Web"),CoreWebView2HostResourceAccessKind.DenyCors);
   core.NewWindowRequested+=(_,e)=>{e.Handled=true;External(e.Uri);};
   core.NavigationStarting+=(_,e)=>{if(!e.Uri.StartsWith("https://com.bruno.youtube-widget/",StringComparison.Ordinal)){e.Cancel=true;SetStatus("Use Abrir vídeo no navegador para acessar o YouTube.");}};
   core.WebMessageReceived+=async(_,e)=>{
    if(!e.Source.StartsWith("https://com.bruno.youtube-widget/",StringComparison.Ordinal))return;
    try {
     using var doc=JsonDocument.Parse(e.WebMessageAsJson);var r=doc.RootElement;var type=r.GetProperty("type").GetString();
     if(type=="ready"){playerReady=true;EnableTransport();SetStatus("Pronto para assistir · pressione ▶ no vídeo.");LogPlayer("player-ready");if(App.Testing&&args.Contains("--play-test"))await core.ExecuteScriptAsync("player.mute();player.playVideo();");}
     else if(type=="state"){int state=r.GetProperty("state").GetInt32();playing=state is 1 or 3;if(playButton.Content is System.Windows.Controls.TextBlock glyph)glyph.Text=playing?"\uE769":"\uE768";playButton.ToolTip=playing?"Pausar vídeo":"Reproduzir vídeo";System.Windows.Automation.AutomationProperties.SetName(playButton,(string)playButton.ToolTip);if(playing){SetStatus("Reproduzindo");LogPlayer("playing");}else if(state==2){SetStatus("Pausado");LogPlayer("paused");}if(state==0&&prefs.ContinueQueue&&current?.Playlist.Length==0)await NextVideo();}
     else if(type=="audio"){muted=r.GetProperty("muted").GetBoolean();if(muteButton.Content is System.Windows.Controls.TextBlock glyph)glyph.Text=muted?"\uE74F":"\uE767";muteButton.ToolTip=muted?"Ativar som":"Desativar som";System.Windows.Automation.AutomationProperties.SetName(muteButton,(string)muteButton.ToolTip);}
     else if(type=="error"){CancelPlaybackRequest();playerReady=false;EnableTransport();int code=r.GetProperty("code").GetInt32();SetStatus(code switch{101 or 150=>"O autor não permite reprodução aqui. Abra no navegador.",100=>"Vídeo indisponível. Abra no navegador.",153=>"YouTube recusou o player. Abra no navegador.",_=>"Não foi possível reproduzir ("+code+"). Abra no navegador."});LogPlayer("error-"+code);}
     else if(type=="autoplayBlocked"){autoplayBlocked=true;CancelPlaybackRequest();SetStatus("O player bloqueou a reprodução. Tente o botão de reprodução no vídeo.",true);}
     else if(type=="network")SetStatus("Sem conexão com o YouTube. Tente abrir novamente.");
     else if(type=="progress") {
      ReceiveProgress(r);
      var title=r.GetProperty("title").GetString()??"";var id=r.GetProperty("id").GetString()??"";PlaylistProgress(r,id);
      if(title.Length>0){videoTitle.Text=title;videoTitle.ToolTip=title;if(id.Length>0&&prefs.Titles.GetValueOrDefault(id)!=title){prefs.Titles[id]=title;RefreshLibrary();}}
      if(current!=null&&current.Id==id){prefs.Last=current with{Start=Math.Clamp((int)r.GetProperty("seconds").GetDouble(),0,604800)};if(Environment.TickCount64-lastProgressSave>=5000){lastProgressSave=Environment.TickCount64;Save();}}
     }
    } catch(Exception error){App.Log(error);}
   };
   core.NavigationCompleted+=async(_,e)=>{
    if(!e.IsSuccess){SetStatus("Não foi possível iniciar o player.");return;}ready=true;LogPlayer("page-loaded");await CheckHandoff();if(handoffToken.Length>0)return;
    if(args.Contains("--probe")){int testVideo=Array.IndexOf(args,"--playback-video");await Load(testVideo>=0&&testVideo+1<args.Length?Video.Parse(args[testVideo+1]):new Video("M7lc1UVf-VE"));}
    else if(prefs.Last!=null)await Load(prefs.Last,preserveSource:true);
    await CheckRecommendation();
   };
   core.Navigate("https://com.bruno.youtube-widget/index.html");
  } catch(Exception e){SetStatus("Instale o Microsoft Edge WebView2 Runtime para assistir.");App.Log(e);}
 }
 internal Task Load(Video video,bool autoplay=false,bool recordHistory=true,bool preserveSource=false) {
  if(!ready){SetStatus("Aguarde o player iniciar.");return Task.CompletedTask;}
  CancelPlaybackRequest();if(!preserveSource)prefs.PreserveCurrentVideo=false;if(recordHistory)RememberVideo(video);playlistIndex=-1;playlistCount=0;
  contextLabel.Text="VÍDEO";current=video;prefs.Last=video;input.Text=video.Url;playing=false;playerReady=false;seek.IsEnabled=false;EnableTransport();elapsed.Text=duration.Text="0:00";Save();UpdateActions();
  videoTitle.Text=prefs.Titles.GetValueOrDefault(video.Id,"Carregando vídeo…");
  web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{type="load",video,autoplay}));SetStatus("Carregando vídeo…");return Task.CompletedTask;
 }
 void PausePlayer(){CancelPlaybackRequest();if(ready)web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"pause\"}");playing=false;}
 internal async Task CheckRecommendation(bool fromTray=false) {
  if(trayRefreshActive&&!fromTray)return;
  if(!ready||handoffActive||!prefs.RecommendationsEnabled||!File.Exists(Recommendations.PathName))return;
  try {
   using var d=JsonDocument.Parse(File.ReadAllText(Recommendations.PathName));var r=d.RootElement;var at=r.GetProperty("At").GetDateTimeOffset();
   if(trayRefreshActive&&(!r.TryGetProperty("RefreshToken",out var pending)||pending.GetString()!=requestedRefresh))return;
   if(at<=recommendationAt||at<DateTimeOffset.UtcNow.AddDays(-1))return;
   var v=Video.Parse(r.GetProperty("Id").GetString()??"");var title=r.GetProperty("Title").GetString()??"Recomendado para você";
   string account=r.TryGetProperty("Account",out var owner)?owner.GetString()??"":"";
   var batch=new System.Collections.Generic.List<Video>();
   if(r.TryGetProperty("Videos",out var entries)&&entries.ValueKind==JsonValueKind.Array)foreach(var entry in entries.EnumerateArray()){
    if(batch.Count>=30)break;try{var candidate=Video.Parse(entry.GetProperty("Id").GetString()??"");if(!batch.Exists(x=>x.Id==candidate.Id)){batch.Add(candidate);prefs.Titles[candidate.Id]=entry.GetProperty("Title").GetString()??candidate.Label;}}catch(FormatException){}
   }
   if(batch.Count==0)batch.Add(v);
   if(batch.Count==1&&prefs.Recommended.Count>1&&account.Length>0&&account==prefs.RecommendationAccount){prefs.Recommended.RemoveAll(x=>x.Id==v.Id);prefs.Recommended.Insert(0,v);if(prefs.Recommended.Count>30)prefs.Recommended.RemoveRange(30,prefs.Recommended.Count-30);}else prefs.Recommended=batch;
   prefs.RecommendationAccount=account;recommendationAt=at;prefs.Titles[v.Id]=title;Save();EnableTransport();
   if(trayRefreshActive&&current?.Id==v.Id){var alternative=batch.Find(candidate=>candidate.Id!=current.Id);if(alternative!=null){v=alternative;title=prefs.Titles.GetValueOrDefault(v.Id,v.Label);}}
   if(playing||prefs.PreserveCurrentVideo)return;
   if(current?.Id!=v.Id||current.Playlist!=v.Playlist)await Load(v);contextLabel.Text=account.Length>0?"PARA VOCÊ · "+account:"PARA VOCÊ";videoTitle.Text=title;SetStatus("Recomendações carregadas · use anterior e próximo.");

  } catch(Exception e) when(e is IOException or JsonException or FormatException or UnauthorizedAccessException){App.Log(e);}
 }
 void LogPlayer(string text){try{Directory.CreateDirectory(Preferences.Folder);File.WriteAllText(Path.Combine(Preferences.Folder,"player-state.json"),JsonSerializer.Serialize(new{At=DateTimeOffset.Now,Event=text,Video=current?.Id,Mode=web.Mode}));if(App.Testing)File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"player-suite.log"),DateTimeOffset.Now.ToString("O")+" "+text+Environment.NewLine);}catch(Exception e){App.Log(e);}}
}


