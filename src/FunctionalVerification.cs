using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 internal async Task<Dictionary<string,bool>> VerifyFunctions(){
  if(!App.Testing)throw new InvalidOperationException("Verification requires an isolated test profile.");
  var result=new Dictionary<string,bool>();
  result["TrayRegistered"]=tray?.TryBounds(out _)==true;
  result["TrayUsesApplicationIcon"]=tray!=null&&Path.GetFileName(tray.IconFile)=="App.ico";
  result["TrayHoverIgnored"]=new[]{0x200,0x406,0x407}.All(c=>NativeTray.CallbackAction(c,true)==0);
  result["TrayMouseAndKeyboardCallbacks"]=NativeTray.CallbackAction((1L<<16)|0x400,true)==1&&NativeTray.CallbackAction(0x401,true)==1&&NativeTray.CallbackAction(0x7b,true)==2;
  bool previousPin=prefs.Pinned;pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["PinButton"]=prefs.Pinned!=previousPin;pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  prefs.ExpandedPlayer=true;Position();expand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["CompactButton"]=Width==360;expand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["ExpandButton"]=Width==640;
  await Load(new Video("M7lc1UVf-VE"));
  for(int i=0;i<30;i++){await Task.Delay(500);if((await web.CoreWebView2.ExecuteScriptAsync("typeof player!=='undefined'&&!!player?.getPlayerState")).Contains("true"))break;}
  double beforeFeedbackHeight=Height;
  prefs.Favorites.Clear();star.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["FavoriteAddsAndShowsState"]=prefs.Favorites.Count==1&&(string)star.ToolTip=="Remover dos favoritos"&&status.Visibility==Visibility.Visible;star.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["FavoriteToggleRemoves"]=prefs.Favorites.Count==0;star.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["FavoritesNoDuplicates"]=prefs.Favorites.Count==1;
  result["FeedbackKeepsWindowHeight"]=Height==beforeFeedbackHeight;
  listKind="Favoritos";RefreshLibrary();library.SelectedIndex=0;RemoveSelected();result["FavoriteRemoval"]=prefs.Favorites.Count==0;
  prefs.Queue.Clear();prefs.Queue.Add(new Video("M7lc1UVf-VE","",12));await Next();result["QueueNext"]=prefs.Queue.Count==0&&current?.Start==12;for(int i=0;i<20&&!playing;i++)await Task.Delay(500);result["QueueNextStartsPlayback"]=playing;PausePlayer();
  await Next();result["EmptyQueuePreservesVideo"]=current?.Id=="M7lc1UVf-VE";result["EmptyQueueShowsFeedback"]=status.Visibility==Visibility.Visible&&status.Text.StartsWith("Fila vazia");
  input.Text="https://example.com/";await OpenInput();result["InvalidLinkPreservesVideo"]=current?.Id=="M7lc1UVf-VE";result["InvalidLinkShowsFeedback"]=status.Visibility==Visibility.Visible;
  prefs.Theme="Light";ApplyAppearance();ShowSettings();await Task.Delay(300);result["LightInputContrast"]=input.Foreground.ToString()==Application.Current.Resources["Ink"].ToString();CloseSettings(false);prefs.Theme="Dark";ApplyAppearance();ShowSettings();await Task.Delay(300);result["LibraryReparenting"]=PanelVisible&&library.Parent!=null&&input.Parent!=null;CloseSettings(false);
  await Load(new Video("M7lc1UVf-VE"));await Task.Delay(4000);
  muteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(700);
  playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));for(int i=0;i<20&&!playing;i++)await Task.Delay(500);result["PlayButton"]=playing;
  if(!muted)muteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));for(int i=0;i<12&&!muted;i++)await Task.Delay(500);result["MuteButton"]=muted;
  playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(700);
  seek.Value=42;await Task.Delay(900);double actualTime=System.Text.Json.JsonSerializer.Deserialize<double>(await web.CoreWebView2.ExecuteScriptAsync("player.getCurrentTime()"));result["SeekSliderControlsPlayer"]=Math.Abs(actualTime-42)<3;
  bool previousRecommendations=prefs.RecommendationsEnabled;prefs.RecommendationsEnabled=true;File.WriteAllText(Recommendations.PathName,System.Text.Json.JsonSerializer.Serialize(new{At=DateTimeOffset.UtcNow,Id="M7lc1UVf-VE",Title="Same recommendation"}));await CheckRecommendation();result["SameRecommendationKeepsPlaybackPosition"]=playerReady&&Math.Abs(seek.Value-42)<3;prefs.RecommendationsEnabled=previousRecommendations;
  volume.Value=27;await Task.Delay(700);result["VolumeSliderControlsPlayer"]=System.Text.Json.JsonSerializer.Deserialize<int>(await web.CoreWebView2.ExecuteScriptAsync("player.getVolume()"))==27;
  muteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(700);playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(700);
  bool savedRecommendations=prefs.RecommendationsEnabled;prefs.RecommendationsEnabled=true;
  File.WriteAllText(Recommendations.PathName,System.Text.Json.JsonSerializer.Serialize(new{At=DateTimeOffset.UtcNow,Id="h5pWEluX-yU",Title="Test recommendation"}));
  await CheckRecommendation();result["RecommendationDoesNotInterruptPlayback"]=playing&&current?.Id=="M7lc1UVf-VE";
  prefs.RecommendationsEnabled=false;
  playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(1000);result["PauseButton"]=!playing;
  playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(1000);HideWidget();await Task.Delay(1000);result["RecollectPausesAndKeepsTray"]=!IsVisible&&!playing&&tray?.TryBounds(out _)==true;
  ShowWidget();await Task.Delay(700);result["ReopenDoesNotAutoplay"]=IsVisible&&!playing;
  prefs.History.Clear();prefs.HistoryIndex=-1;prefs.Queue.Clear();await Load(new Video("M7lc1UVf-VE"));await WaitForVideo("M7lc1UVf-VE");prefs.Queue.Add(new Video("h5pWEluX-yU"));RefreshLibrary();forwardButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["NextButtonChangesVideo"]=await WaitForVideo("h5pWEluX-yU");
  backButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["PreviousButtonChangesVideo"]=await WaitForVideo("M7lc1UVf-VE");
  forwardButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["NextButtonRestoresForwardHistory"]=await WaitForVideo("h5pWEluX-yU")&&prefs.Queue.Count==0;
  PausePlayer();Save();var restoredHistory=Preferences.Load();result["NavigationHistoryPersists"]=restoredHistory.History.Count==2&&restoredHistory.HistoryIndex==1;
  result["InternalYouTubeControlsDisabled"]=(await web.CoreWebView2.ExecuteScriptAsync("document.querySelector('iframe').src.includes('controls=0')")).Contains("true");
  RememberVideo(new Video("","PL1234567890"));result["PlaylistPlaceholderNotRecordedAsVideo"]=prefs.History.Count==2;
  int feedArg=Array.IndexOf(args,"--test-feed");
  if(feedArg>=0&&feedArg+1<args.Length){
   using var captured=System.Text.Json.JsonDocument.Parse(File.ReadAllText(args[feedArg+1]));var entries=captured.RootElement.GetProperty("Videos");string firstId=entries[0].GetProperty("Id").GetString()!,secondId=entries[1].GetProperty("Id").GetString()!;
   prefs.Queue.Clear();prefs.History.Clear();prefs.HistoryIndex=-1;prefs.Recommended.Clear();recommendationAt=DateTimeOffset.MinValue;
   await Load(new Video("h5pWEluX-yU"));await WaitForVideo("h5pWEluX-yU");PausePlayer();await Task.Delay(300);
   File.Copy(args[feedArg+1],Recommendations.PathName,true);prefs.RecommendationsEnabled=true;await CheckRecommendation();await WaitForVideo(firstId);
   result["HomepageSequenceLoaded"]=prefs.Recommended.Count>=2;result["NextEnabledWithEmptyManualQueue"]=prefs.Queue.Count==0&&forwardButton.IsEnabled;
   forwardButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["NextPlaysSecondHomepageRecommendation"]=await WaitForVideo(secondId);for(int i=0;i<20&&(await web.CoreWebView2.ExecuteScriptAsync("player.getPlayerState()"))!="1";i++)await Task.Delay(500);result["SecondHomepageRecommendationPlaying"]=(await web.CoreWebView2.ExecuteScriptAsync("player.getPlayerState()"))=="1";
   backButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["PreviousReturnsToFirstHomepageRecommendation"]=await WaitForVideo(firstId);
   forwardButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));result["NextRestoresRecommendationWithoutQueue"]=await WaitForVideo(secondId)&&prefs.Queue.Count==0;
   PausePlayer();File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"..","navegacao-recomendacoes-1.5.2.json"),System.Text.Json.JsonSerializer.Serialize(new{At=DateTimeOffset.Now,First=firstId,Next=secondId,Count=prefs.Recommended.Count,ManualQueueCount=prefs.Queue.Count,Source="Authenticated homepage observed in Chrome; assisted import"}));
  }
  prefs.RecommendationsEnabled=savedRecommendations;File.Delete(Recommendations.PathName);listKind="Fila";RefreshLibrary();Save();
  return result;
  async Task<bool> WaitForVideo(string id){for(int i=0;i<30;i++){await Task.Delay(500);if(playerReady&&current?.Id==id&&(await web.CoreWebView2.ExecuteScriptAsync("player.getVideoData().video_id"))=="\""+id+"\"")return true;}return false;}
 }
}
