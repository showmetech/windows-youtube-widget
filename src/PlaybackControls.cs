using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 readonly StackPanel playerFooter=new();
 readonly Grid timeline=new(){Height=22,Margin=new(12,0,12,0)},transport=new(){Height=48,Margin=new(12,0,12,0)};
 readonly Slider seek=new(){Minimum=0,Maximum=1,IsEnabled=false,Height=22},volume=new(){Minimum=0,Maximum=100,Value=100,Width=50,Height=32};
 readonly TextBlock elapsed=Text("0:00",10,false,"SecondaryInk"),duration=Text("0:00",10,false,"SecondaryInk");
 Button backButton=null!,forwardButton=null!;
 bool syncingControls,playerReady;
 long lastProgressSave;
 void BuildTimeline(StackPanel body){
  timeline.ColumnDefinitions.Add(new(){Width=new(42)});timeline.ColumnDefinitions.Add(new());timeline.ColumnDefinitions.Add(new(){Width=new(42)});
  elapsed.HorizontalAlignment=HorizontalAlignment.Left;duration.HorizontalAlignment=HorizontalAlignment.Right;
  timeline.Children.Add(elapsed);Grid.SetColumn(seek,1);timeline.Children.Add(seek);Grid.SetColumn(duration,2);timeline.Children.Add(duration);var progress=new Grid();progress.Children.Add(timeline);progress.Children.Add(titleFrame);body.Children.Add(progress);
  AutomationProperties.SetName(seek,"Posição do vídeo");AutomationProperties.SetName(volume,"Volume do vídeo");
  seek.ValueChanged+=(_,_)=>{if(!syncingControls&&playerReady){SendPlayer(new{type="seek",seconds=seek.Value});Touch();}};
  volume.ValueChanged+=(_,_)=>{if(!syncingControls&&playerReady){SendPlayer(new{type="volume",value=volume.Value});Touch();}};
 }
 void BuildFeedback(StackPanel body){
  titleFrame.Height=22;titleFrame.Margin=new(12,0,12,0);titleFrame.Visibility=Visibility.Collapsed;titleFrame.ClipToBounds=true;
  status.Margin=new(4,0,4,0);status.FontSize=11;status.MaxHeight=22;status.TextWrapping=TextWrapping.NoWrap;status.TextTrimming=TextTrimming.CharacterEllipsis;status.Visibility=Visibility.Collapsed;
  AutomationProperties.SetLiveSetting(status,AutomationLiveSetting.Polite);titleFrame.Children.Add(status);
 }
 void BuildTransport(StackPanel body){
  transport.ColumnDefinitions.Add(new(){Width=new GridLength(136)});
  transport.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  transport.ColumnDefinitions.Add(new(){Width=new GridLength(136)});
  var windowActions=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Left};
  foreach(var action in new[]{config,pin,expand}){action.Margin=new(0,0,4,0);windowActions.Children.Add(action);}
  AutomationProperties.SetAutomationId(config,"Suite.Settings");AutomationProperties.SetAutomationId(pin,"Suite.Pin");AutomationProperties.SetAutomationId(close,"Suite.Close");
  transport.Children.Add(windowActions);
  var playback=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};
  backButton=IconButton("\uE892","Vídeo anterior",async()=>await PreviousVideo());forwardButton=IconButton("\uE893","Próximo vídeo",async()=>await NextVideo());
  playButton.Width=playButton.Height=36;playButton.Margin=new(0,0,0,0);playButton.SetResourceReference(BorderBrushProperty,"Accent");playButton.BorderThickness=new(2);
  playButton.Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='2' CornerRadius='18'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border></ControlTemplate>");
  playback.Children.Add(backButton);playback.Children.Add(playButton);playback.Children.Add(forwardButton);Grid.SetColumn(playback,1);transport.Children.Add(playback);
  volume.Width=64;volume.MinWidth=0;volume.Margin=new(4,0,4,0);
  var audio=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};audio.Children.Add(muteButton);audio.Children.Add(volume);audio.Children.Add(close);Grid.SetColumn(audio,2);transport.Children.Add(audio);body.Children.Add(transport);EnableTransport();System.Windows.Controls.ToolTipService.SetShowOnDisabled(backButton,true);System.Windows.Controls.ToolTipService.SetShowOnDisabled(forwardButton,true);
 }
 void SendPlayer(object message){if(playerReady)web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message));Touch();}
 void EnableTransport(){playButton.IsEnabled=ready&&current!=null;muteButton.IsEnabled=volume.IsEnabled=playerReady;backButton.IsEnabled=ready&&(prefs.HistoryIndex>0||playlistIndex>0);forwardButton.IsEnabled=ready&&(NextRecommended()!=null||prefs.Queue.Count>0||prefs.HistoryIndex+1<prefs.History.Count||playlistIndex>=0&&playlistIndex+1<playlistCount);backButton.ToolTip=backButton.IsEnabled?"Vídeo anterior":"Sem vídeo anterior nesta sequência";forwardButton.ToolTip=forwardButton.IsEnabled?"Próximo vídeo":"Fim da sequência · abra o início do YouTube para atualizar";}
 static string Timestamp(double seconds){var t=TimeSpan.FromSeconds(Math.Clamp(double.IsFinite(seconds)?seconds:0,0,604800));return t.TotalHours>=1?$"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}":$"{(int)t.TotalMinutes}:{t.Seconds:00}";}
 void ReceiveProgress(JsonElement r){
  double seconds=r.GetProperty("seconds").GetDouble(),length=r.TryGetProperty("duration",out var d)?d.GetDouble():0;
  syncingControls=true;try{seek.IsEnabled=playerReady&&length>0;seek.Maximum=Math.Max(1,length);if(!seek.IsMouseCaptureWithin)seek.Value=Math.Clamp(seconds,0,seek.Maximum);elapsed.Text=Timestamp(seconds);duration.Text=length>0?Timestamp(length):"--:--";seek.ToolTip=$"{Timestamp(seconds)} / {duration.Text}";if(r.TryGetProperty("volume",out var v)&&!volume.IsMouseCaptureWithin)volume.Value=Math.Clamp(v.GetDouble(),0,100);volume.ToolTip=$"Volume: {(int)volume.Value}%";}finally{syncingControls=false;}EnableTransport();
 }
 void ShowPlayerMenu(){
  Touch();SuiteActivation.MenuOpen=true;var menu=new ContextMenu{Placement=PlacementMode.MousePoint};menu.Closed+=(_,_)=>{SuiteActivation.MenuOpen=false;Touch();};
  void Add(string text,Action action,bool enabled=true){var item=new MenuItem{Header=text,IsEnabled=enabled};item.Click+=(_,_)=>action();menu.Items.Add(item);}
  Add((string)star.ToolTip,Favorite,current!=null);Add("Adicionar vídeo à fila",QueueCurrent,current!=null);Add("Próximo vídeo da fila",async()=>await Next());menu.Items.Add(new Separator());
  Add("Abrir vídeo no navegador",()=>{if(current!=null)External(current.Url);},current!=null);Add("Buscar nova recomendação",()=>{prefs.PreserveCurrentVideo=false;Save();External("https://www.youtube.com/");SetStatus("Abra o início do YouTube com a extensão conectada.",true);});Add("Fila, favoritos e configurações",ShowSettings);menu.IsOpen=true;
 }
}
