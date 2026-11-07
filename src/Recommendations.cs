using System;using System.IO;using System.Text;using System.Text.Json;using System.Collections.Generic;
namespace YouTubeWidget;
static class Recommendations{
 public const string Host="com.bruno.youtube_widget";
 public static string PathName=>Path.Combine(Preferences.Folder,"recommendation.json");
 public static string RefreshPath=>Path.Combine(Preferences.Folder,"recommendation-refresh.json");
 public static void NativeHost(bool testing=false){
  var input=Console.OpenStandardInput();var output=Console.OpenStandardOutput();
  try{var len=new byte[4];input.ReadExactly(len);int size=BitConverter.ToInt32(len);if(size<2||size>65536)return;var payload=new byte[size];input.ReadExactly(payload);using var doc=JsonDocument.Parse(payload);var root=doc.RootElement;
   try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"bridge-health.json"),JsonSerializer.Serialize(new{At=DateTimeOffset.UtcNow,Folder=Preferences.Folder,Process=Environment.ProcessId,Operation=root.GetProperty("type").GetString()}));}catch(IOException){}catch(UnauthorizedAccessException){}
   if(root.GetProperty("type").GetString()=="subscribeRecommendations"){
    string previous="";int heartbeat=0;
    while(true){string token="";
     if(!testing){using var reply=JsonDocument.Parse(RecommendationBroker.Call("{\"type\":\"refreshPoll\"}"));token=reply.RootElement.GetProperty("token").GetString()??"";}
     else try{using var request=JsonDocument.Parse(File.ReadAllText(RefreshPath));if(request.RootElement.GetProperty("At").GetDateTimeOffset()>DateTimeOffset.UtcNow.AddSeconds(-45))token=request.RootElement.GetProperty("Token").GetString()??"";}catch(IOException){}catch(JsonException){}
     if((token.Length>0&&token!=previous)||heartbeat++%20==0){var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{type=token.Length>0?"refreshRecommendations":"heartbeat",token}));output.Write(BitConverter.GetBytes(bytes.Length));output.Write(bytes);output.Flush();previous=token;}
     System.Threading.Thread.Sleep(1000);
    }
   }
   if(root.GetProperty("type").GetString()=="handoff"){var reply=testing?JsonSerializer.Serialize(HandoffTransport.Handle(root,true)):RecommendationBroker.Call(root.GetRawText());var result=Encoding.UTF8.GetBytes(reply);output.Write(BitConverter.GetBytes(result.Length));output.Write(result);output.Flush();return;}
   if(root.GetProperty("type").GetString()!="recommendation")return;
   string accepted=testing?SaveRecommendation(root):RecommendationBroker.Call(root.GetRawText());var acceptedBytes=Encoding.UTF8.GetBytes(accepted);output.Write(BitConverter.GetBytes(acceptedBytes.Length));output.Write(acceptedBytes);output.Flush();
  }catch(Exception error){try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"bridge-error.json"),JsonSerializer.Serialize(new{At=DateTimeOffset.UtcNow,Error=error.GetType().Name,Message=error.Message,Folder=Preferences.Folder}));}catch{}var response=Encoding.UTF8.GetBytes("{\"ok\":false}");try{output.Write(BitConverter.GetBytes(response.Length));output.Write(response);output.Flush();}catch{}}
 }
 internal static string SaveRecommendation(JsonElement root){
   string id=root.GetProperty("id").GetString()??"";var v=Video.Parse(id);var title=root.GetProperty("title").GetString()??"Vídeo recomendado";
   string account=root.TryGetProperty("account",out var owner)&&owner.ValueKind==JsonValueKind.String?owner.GetString()??"":"";
   var videos=new List<RecommendedEntry>{new(v.Id,title[..Math.Min(title.Length,300)])};
   if(root.TryGetProperty("videos",out var entries)&&entries.ValueKind==JsonValueKind.Array)foreach(var entry in entries.EnumerateArray()){
    if(videos.Count>=30)break;try{var candidate=Video.Parse(entry.GetProperty("id").GetString()??"");if(!videos.Exists(x=>x.Id==candidate.Id)){var name=entry.GetProperty("title").GetString()??candidate.Label;videos.Add(new(candidate.Id,name[..Math.Min(name.Length,300)]));}}catch(FormatException){}
   }
   string refreshToken=root.TryGetProperty("refreshToken",out var refresh)?refresh.GetString()??"":"";
   Directory.CreateDirectory(Preferences.Folder);var data=JsonSerializer.Serialize(new{Id=v.Id,Title=title[..Math.Min(title.Length,300)],Account=account[..Math.Min(account.Length,100)],Videos=videos,RefreshToken=refreshToken,At=DateTimeOffset.UtcNow});
   string temporary=PathName+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temporary,data);File.Move(temporary,PathName,true);
   try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"bridge-saved.json"),JsonSerializer.Serialize(new{At=DateTimeOffset.UtcNow,PathName,Id=v.Id,Length=new FileInfo(PathName).Length,RefreshToken=refreshToken}));}catch{}
   return "{\"ok\":true}";
 }
 record RecommendedEntry(string Id,string Title);
}
