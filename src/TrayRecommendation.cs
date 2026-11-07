using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 bool trayRefreshActive;string requestedRefresh="";
 async Task RefreshOnTrayOpen(){
  if(handoffActive)return;
  if(!prefs.RecommendationsEnabled){SetStatus("Ative as recomendações nas configurações.",true);return;}
  trayRefreshActive=true;string token=Guid.NewGuid().ToString("N");requestedRefresh=token;
  try{
   PausePlayer();prefs.PreserveCurrentVideo=false;Save();SetStatus("Atualizando recomendação…",true);
   Directory.CreateDirectory(Preferences.Folder);File.WriteAllText(Recommendations.RefreshPath+".tmp",JsonSerializer.Serialize(new{Token=token,At=DateTimeOffset.UtcNow}));File.Move(Recommendations.RefreshPath+".tmp",Recommendations.RefreshPath,true);
   for(int i=0;i<90;i++){
    await Task.Delay(500);if(requestedRefresh!=token||!IsVisible||exiting||handoffActive)return;if(!ready)continue;
    try{using var d=JsonDocument.Parse(File.ReadAllText(Recommendations.PathName));var r=d.RootElement;
     if(r.TryGetProperty("RefreshToken",out var result)&&result.GetString()==token){recommendationAt=DateTimeOffset.MinValue;await CheckRecommendation(true);SetStatus("");return;}
    }catch(IOException){}catch(JsonException){}
   }
   SetStatus("Não foi possível atualizar. Abra o Chrome e ative as recomendações na extensão.",true);
  }catch(Exception error) when(error is IOException or UnauthorizedAccessException){App.Log(error);SetStatus("Não foi possível atualizar a recomendação.",true);}
  finally{if(requestedRefresh==token){trayRefreshActive=false;requestedRefresh="";}}
 }
}
