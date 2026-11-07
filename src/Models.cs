using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeWidget;
public record Video(string Id, string Playlist = "", int Start = 0) {
 public string Url => Id.Length>0 ? "https://www.youtube.com/watch?v="+Id+(Playlist.Length>0?"&list="+Playlist:"") : "https://www.youtube.com/playlist?list="+Playlist;
 public string Label => Id.Length>0 ? "Vídeo · "+Id : "Playlist · "+Playlist;
 public static Video Parse(string input) {
  input=input.Trim();
  if(Regex.IsMatch(input,"^[A-Za-z0-9_-]{11}$"))return new(input);
  if(!Uri.TryCreate(input,UriKind.Absolute,out var u)||u.Scheme!="https"||u.Port!=443||u.UserInfo.Length>0)throw new FormatException("Cole um link HTTPS do YouTube ou um ID de vídeo.");
  var host=u.IdnHost.ToLowerInvariant();
  if(!new[]{"youtube.com","www.youtube.com","m.youtube.com","music.youtube.com","youtu.be","www.youtube-nocookie.com"}.Contains(host))throw new FormatException("O link precisa ser do YouTube.");
  var q=new Dictionary<string,string>();foreach(var part in u.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries)){var p=part.Split('=',2);q[Uri.UnescapeDataString(p[0])]=p.Length==2?Uri.UnescapeDataString(p[1]):"";}
  string id="";var path=u.AbsolutePath.Split('/',StringSplitOptions.RemoveEmptyEntries);
  if(host=="youtu.be")id=path.FirstOrDefault()??"";
  else if(path.Length>=2&&new[]{"shorts","live","embed"}.Contains(path[0]))id=path[1];
  else if(u.AbsolutePath=="/watch")id=q.GetValueOrDefault("v","");
  var list=q.GetValueOrDefault("list","");
  if(id.Length>0&&!Regex.IsMatch(id,"^[A-Za-z0-9_-]{11}$"))throw new FormatException("ID de vídeo inválido.");
  if(list.Length>0&&!Regex.IsMatch(list,"^[A-Za-z0-9_-]{10,100}$"))throw new FormatException("Playlist inválida.");
  if(id.Length==0&&list.Length==0)throw new FormatException("Use o link de um vídeo ou de uma playlist.");
  int start=0;var stamp=q.GetValueOrDefault("t",q.GetValueOrDefault("start","0"));
  if(!int.TryParse(stamp,out start)){var m=Regex.Match(stamp,"^(?:(\\d+)h)?(?:(\\d+)m)?(?:(\\d+)s)?$");if(m.Success){int n(int i)=>int.TryParse(m.Groups[i].Value,out var v)?Math.Min(v,86400):0;start=n(1)*3600+n(2)*60+n(3);}}
  return new(id,list,Math.Clamp(start,0,604800));
 }
}
public class Preferences {
 public int SchemaVersion{get;set;}=2; public string Theme{get;set;}="System";public int Material{get;set;}=0;public bool Animations{get;set;}=true;public bool AlwaysOnTop{get;set;}=false;public bool ExpandedPlayer{get;set;}=false; public Dictionary<string,string> Titles{get;set;}=new(); public double Width{get;set;}=360;public double Height{get;set;}=600;public double Left{get;set;}=-1;public double Top{get;set;}=-1;
 public bool PreserveCurrentVideo{get;set;}=false;public bool RecommendationsEnabled{get;set;}=true; public bool Pinned{get;set;}=true;public bool Light{get;set;}=false;public bool ContinueQueue{get;set;}=true;
 public List<Video> Recommended{get;set;}=new();public string RecommendationAccount{get;set;}="";
 public List<Video> History{get;set;}=new();public int HistoryIndex{get;set;}=-1;
 public List<Video> Favorites{get;set;}=new();public List<Video> Queue{get;set;}=new();public Video? Last{get;set;}
 public static string? TestFolder; public static string Folder=>TestFolder??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowsYouTubeWidget");
 public static Preferences Load(){try{var raw=File.ReadAllText(Path.Combine(Folder,"settings.json"));var value=JsonSerializer.Deserialize<Preferences>(raw)??new();using var doc=JsonDocument.Parse(raw);if(!doc.RootElement.TryGetProperty("Theme",out _)&&doc.RootElement.TryGetProperty("Light",out var legacy))value.Theme=legacy.GetBoolean()?"Light":"Dark";value.Recommended??=new();value.History??=new();value.HistoryIndex=Math.Clamp(value.HistoryIndex,-1,value.History.Count-1);value.Favorites??=new();value.Queue??=new();value.Titles??=new();return value;}catch{return new();}}
 public void Save(){Directory.CreateDirectory(Folder);var path=Path.Combine(Folder,"settings.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(path+".tmp",path,true);}
}



