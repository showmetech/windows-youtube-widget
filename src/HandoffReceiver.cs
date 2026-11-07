using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 bool checkingHandoff,handoffActive;string handoffToken="";
 async Task CheckHandoff(){
  if(!ready||checkingHandoff||!File.Exists(HandoffTransport.RequestPath))return;
  HandoffRequest? request;try{request=JsonSerializer.Deserialize<HandoffRequest>(File.ReadAllText(HandoffTransport.RequestPath));}catch{return;}
  if(request==null||!Guid.TryParseExact(request.Token,"N",out _))return;
  if(handoffActive&&handoffToken==request.Token&&request.Phase=="prepare"&&request.At<DateTimeOffset.UtcNow.AddSeconds(-60)){PausePlayer();handoffActive=false;HandoffTransport.Write(HandoffTransport.AckPath,new HandoffAck(request.Token,request.Phase,false,"expired","Transferência expirada.",DateTimeOffset.UtcNow));return;}
  var ack=HandoffTransport.Ack();if(ack?.Token==request.Token&&ack.Phase==request.Phase)return;
  checkingHandoff=true;
  void Reply(bool ok,string status,string error="")=>HandoffTransport.Write(HandoffTransport.AckPath,new HandoffAck(request.Token,request.Phase,ok,status,error,DateTimeOffset.UtcNow));
  try{
   if(request.At<DateTimeOffset.UtcNow.AddSeconds(-60)){Reply(false,"expired","Transferência expirada.");handoffActive=false;return;}
   if(request.Phase=="prepare"){
    handoffActive=true;handoffToken=request.Token;recommendationAt=DateTimeOffset.UtcNow;
    prefs.ExpandedPlayer=true;prefs.Titles[request.Id]=request.Title;ShowWidget();await Load(new Video(request.Id,"",(int)request.Seconds));
    for(int i=0;i<150;i++){await Task.Delay(100);if(playerReady&&current?.Id==request.Id)break;}
    if(!playerReady||current?.Id!=request.Id){Reply(false,"failed","O vídeo não pôde ser preparado no widget.");handoffActive=false;return;}
    PausePlayer();Reply(true,"prepared");SetStatus("Pronto para receber o vídeo do Chrome.",true);return;
   }
   if(handoffToken!=request.Token){Reply(false,"failed","O widget não possui esta transferência.");return;}
   if(request.Phase=="abort"){if(current?.Id==request.Id)PausePlayer();handoffActive=false;Reply(true,"aborted");return;}
   if(request.Phase!="commit"||!playerReady||current?.Id!=request.Id){handoffActive=false;Reply(false,"failed","O vídeo preparado não está mais disponível.");return;}
   handoffActive=true;
   web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{type="handoff",seconds=request.Seconds,volume=request.Volume,muted=request.Muted,playing=request.Playing,rate=request.Rate,live=request.Live}));
   bool success=false;
   for(int i=0;i<120;i++){
    await Task.Delay(100);if(!playerReady)break;
    var raw=await web.CoreWebView2.ExecuteScriptAsync("JSON.stringify({id:player.getVideoData().video_id,state:player.getPlayerState(),time:player.getCurrentTime()})");
    string? json=JsonSerializer.Deserialize<string>(raw);if(json==null)continue;using var d=JsonDocument.Parse(json);var r=d.RootElement;int state=r.GetProperty("state").GetInt32();
    bool playback=request.Playing?state==1:state is not(1 or 3);
    if(r.GetProperty("id").GetString()==request.Id&&playback&&(request.Live||Math.Abs(r.GetProperty("time").GetDouble()-request.Seconds)<4)){success=true;break;}
   }
   if(!success){if(current?.Id==request.Id)PausePlayer();handoffActive=false;Reply(false,"failed","O widget não confirmou a reprodução. O vídeo do Chrome pode continuar.");return;}
   prefs.Last=new Video(request.Id,"",(int)request.Seconds);prefs.PreserveCurrentVideo=true;Save();handoffActive=false;Reply(true,request.Playing?"playing":"paused");SetStatus("Vídeo recebido do Chrome.",true);
  }catch(Exception error){if(current?.Id==request.Id)PausePlayer();handoffActive=false;App.Log(error);Reply(false,"failed","Não foi possível receber o vídeo do Chrome.");}
  finally{checkingHandoff=false;}
 }
}
