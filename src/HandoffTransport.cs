using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
namespace YouTubeWidget;
internal record HandoffRequest(string Token,string Phase,string Id,string Title,double Seconds,double Volume,bool Muted,bool Playing,double Rate,bool Live,DateTimeOffset At);
internal record HandoffAck(string Token,string Phase,bool Ok,string Status,string Error,DateTimeOffset At);
internal static class HandoffTransport {
 internal static string RequestPath=>Path.Combine(Preferences.Folder,"handoff-request.json");
 internal static string AckPath=>Path.Combine(Preferences.Folder,"handoff-ack.json");
 internal static void Write<T>(string path,T value){Directory.CreateDirectory(Preferences.Folder);var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(value));File.Move(temp,path,true);}
 internal static HandoffAck? Ack(){try{return JsonSerializer.Deserialize<HandoffAck>(File.ReadAllText(AckPath));}catch{return null;}}
 internal static object Handle(JsonElement root,bool testing){
  string phase=root.GetProperty("phase").GetString()??"";
  if(phase is not("prepare" or "commit" or "abort"))return new{ok=false,error="Etapa de transferência inválida."};
  string token=phase=="prepare"?Guid.NewGuid().ToString("N"):root.GetProperty("transferId").GetString()??"";
  if(!Guid.TryParseExact(token,"N",out _))return new{ok=false,error="Transferência inválida."};
  using var gate=new Mutex(false,testing?"Local\\YouTubeWidget.Handoff.Test":"Local\\YouTubeWidget.Handoff.Gate");
  if(!gate.WaitOne(1500))return new{ok=false,error="Outra transferência está em andamento."};
  try{
   HandoffRequest? previous=null;try{previous=JsonSerializer.Deserialize<HandoffRequest>(File.ReadAllText(RequestPath));}catch{}
   var priorAck=Ack();
   if(phase=="prepare"&&previous!=null&&previous.At>DateTimeOffset.UtcNow.AddSeconds(-60)&&previous.Phase=="prepare"&&!(priorAck?.Token==previous.Token&&!priorAck.Ok))return new{ok=false,error="Outra transferência está em andamento. Aguarde."};
   if(phase!="prepare"&&(previous==null||previous.Token!=token))return new{ok=false,error="A transferência não está mais ativa."};
   if(phase=="commit"&&priorAck?.Token==token&&priorAck.Status=="expired")return new{ok=false,transferId=token,error="A transferência expirou. Tente novamente."};
   if(phase!="prepare"&&priorAck?.Token==token&&priorAck.Phase==phase)return new{ok=priorAck.Ok,transferId=token,phase=priorAck.Status,error=priorAck.Error};
   HandoffRequest request;
   if(phase=="abort")request=previous! with{Phase=phase,At=DateTimeOffset.UtcNow};
   else {
    string id=root.GetProperty("id").GetString()??"";var video=Video.Parse(id);if(video.Id!=id)throw new FormatException();
    double seconds=root.GetProperty("seconds").GetDouble(),volume=root.GetProperty("volume").GetDouble(),rate=root.GetProperty("rate").GetDouble();
    if(!double.IsFinite(seconds)||seconds<0||seconds>604800||!double.IsFinite(volume)||volume<0||volume>100||!double.IsFinite(rate)||rate<.25||rate>4)throw new FormatException();
    if(previous!=null&&phase=="commit"&&previous.Id!=id)throw new FormatException();
    string title=root.TryGetProperty("title",out var name)?name.GetString()??"Vídeo do Chrome":"Vídeo do Chrome";
    request=new(token,phase,id,title[..Math.Min(title.Length,300)],seconds,volume,root.GetProperty("muted").GetBoolean(),root.GetProperty("playing").GetBoolean(),rate,root.TryGetProperty("live",out var live)&&live.GetBoolean(),DateTimeOffset.UtcNow);
   }
   Write(RequestPath,request);
   if(!testing)WakeWidget();
  }finally{gate.ReleaseMutex();}
  var watch=Stopwatch.StartNew();while(watch.ElapsedMilliseconds<22000){var ack=Ack();if(ack?.Token==token&&ack.Phase==phase)return new{ok=ack.Ok,transferId=token,phase=ack.Status,error=ack.Error};Thread.Sleep(100);}
  return new{ok=false,transferId=token,error="O widget não confirmou a transferência a tempo."};
 }
 static void WakeWidget(){if(App.TrySignal("Show"))return;Process.Start(new ProcessStartInfo(Environment.ProcessPath!,"--handoff-start"){UseShellExecute=false,CreateNoWindow=true});}
}
