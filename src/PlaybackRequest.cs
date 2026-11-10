using System;
using System.Text.Json;
using System.Threading.Tasks;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 long playbackRequestId;
 bool? playbackIntent;
 internal bool PlaybackPending=>playbackIntent.HasValue;
 void CancelPlaybackRequest(){playbackRequestId++;playbackIntent=null;}
 async Task TogglePlayback(){
  Touch();bool wanted=!(playbackIntent??playing);long request=++playbackRequestId;playbackIntent=wanted;
  string expected=current?.Id??"";
  try{
   if(!ready||current==null){SetStatus("Aguarde o vídeo carregar para reproduzir.",true);return;}
   SetStatus(wanted?"Iniciando reprodução…":"Pausando…",true);
   int attempts=0,nextAttempt=0;double? firstTime=null;
   for(int step=0;step<75;step++){
    if(request!=playbackRequestId||exiting||!IsVisible||current?.Id!=expected)return;
    if(playerReady){
     // A native WPF click is asynchronous to the iframe. Use idempotent commands
     // and confirm actual progress; a sent message alone is not playback proof.
     if(step>=nextAttempt&&attempts<3){web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{type=wanted?"play":"pause"}));attempts++;nextAttempt=step+5;}
     var raw=await web.CoreWebView2.ExecuteScriptAsync("JSON.stringify({state:player.getPlayerState(),time:player.getCurrentTime(),id:player.getVideoData().video_id})");
     if(request!=playbackRequestId||exiting||!IsVisible)return;
     string? json=JsonSerializer.Deserialize<string>(raw);
     if(json!=null){using var doc=JsonDocument.Parse(json);var value=doc.RootElement;int state=value.GetProperty("state").GetInt32();double time=value.GetProperty("time").GetDouble();
      if(expected.Length==0||value.GetProperty("id").GetString()==expected){
       if(!wanted&&state is not(1 or 3)){SetStatus("Pausado");return;}
       if(wanted&&state==1){firstTime??=time;if(time>firstTime.Value+.2){SetStatus("Reproduzindo");return;}}
       else firstTime=null;
      }
     }
    }
    await Task.Delay(200);
   }
   SetStatus("Não foi possível iniciar a reprodução. Tente novamente ou abra outro vídeo.",true);
  }catch(Exception error){App.Log(error);if(request==playbackRequestId)SetStatus("Não foi possível controlar o vídeo. Tente novamente.",true);}
  finally{if(request==playbackRequestId)playbackIntent=null;}
 }
}
