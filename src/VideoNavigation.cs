using System;
using System.Linq;
using System.Threading.Tasks;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 int playlistIndex=-1,playlistCount;
 void RememberPosition(){if(prefs.HistoryIndex>=0&&prefs.HistoryIndex<prefs.History.Count&&prefs.Last!=null&&current?.Id==prefs.Last.Id)prefs.History[prefs.HistoryIndex]=prefs.Last;}
 void RememberVideo(Video video){
  RememberPosition();if(video.Id.Length==0)return;int index=prefs.HistoryIndex;
  if(index>=0&&index<prefs.History.Count&&prefs.History[index].Id==video.Id&&prefs.History[index].Playlist==video.Playlist){prefs.History[index]=video;return;}
  if(index+1<prefs.History.Count)prefs.History.RemoveRange(Math.Max(0,index+1),prefs.History.Count-Math.Max(0,index+1));
  prefs.History.Add(video);if(prefs.History.Count>100)prefs.History.RemoveAt(0);prefs.HistoryIndex=prefs.History.Count-1;
 }
 async Task PreviousVideo(){
  if(current?.Playlist.Length>0&&playlistIndex>0){SendPlayer(new{type="previousVideo"});return;}
  if(prefs.HistoryIndex<=0){SetStatus("Não há vídeo anterior nesta sequência.",true);return;}
  RememberPosition();prefs.HistoryIndex--;await Load(prefs.History[prefs.HistoryIndex],true,false);
 }
 async Task NextVideo(){
  if(current?.Playlist.Length>0&&playlistIndex>=0&&playlistIndex+1<playlistCount){SendPlayer(new{type="nextVideo"});return;}
  if(prefs.HistoryIndex+1<prefs.History.Count){RememberPosition();prefs.HistoryIndex++;await Load(prefs.History[prefs.HistoryIndex],true,false);return;}
  if(prefs.Queue.Count>0){await Next();return;}
  var recommended=NextRecommended();if(recommended!=null){await Load(recommended,true);return;}
  SetStatus("Fim das recomendações salvas. Abra o início do YouTube para atualizar.",true);
 }
 Video? NextRecommended(){if(!prefs.RecommendationsEnabled||prefs.Recommended.Count==0)return null;int index=prefs.Recommended.FindIndex(v=>v.Id==current?.Id);return prefs.Recommended.Skip(index+1).FirstOrDefault(v=>v.Id!=current?.Id);}
 void PlaylistProgress(System.Text.Json.JsonElement r,string id){
  if(r.TryGetProperty("playlistIndex",out var index))playlistIndex=index.GetInt32();
  if(r.TryGetProperty("playlistCount",out var count))playlistCount=count.GetInt32();
  if(current?.Playlist.Length>0&&id.Length==11&&current.Id!=id){var next=current with{Id=id,Start=0};RememberVideo(next);current=next;prefs.Last=next;Save();UpdateActions();}
 }
}
