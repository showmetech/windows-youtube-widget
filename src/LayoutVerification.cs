using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 internal async System.Threading.Tasks.Task VerifyControlLayout(){
  if(!App.Testing)return;
  var results=new System.Collections.Generic.List<object>();
  foreach(bool expanded in new[]{true,false}){
   prefs.ExpandedPlayer=expanded;Position();UpdateLayout();await System.Threading.Tasks.Task.Delay(100);UpdateLayout();
   Rect Bounds(FrameworkElement element){var point=element.TranslatePoint(new Point(),this);return new Rect(point,new Size(element.ActualWidth,element.ActualHeight));}
   var previous=Bounds(backButton);var play=Bounds(playButton);var next=Bounds(forwardButton);var slider=Bounds(volume);var size=Bounds(expand);var mute=Bounds(muteButton);
   double center=(previous.Left+next.Right)/2;
   bool passed=Math.Abs(center-ActualWidth/2)<1&&next.Right<=mute.Left&&mute.Right<=slider.Left&&slider.Right<=size.Left&&size.Right<=ActualWidth;
   results.Add(new{Expanded=expanded,Width=ActualWidth,Center=center,WindowCenter=ActualWidth/2,VolumeLeft=slider.Left,VolumeRight=slider.Right,SizeButtonLeft=size.Left,TitleRemoved=videoTitle.Parent==null,HeaderAligned=SuiteChrome.Aligned(this,false,40),Passed=passed});
  }
  prefs.ExpandedPlayer=true;Position();UpdateLayout();
  File.WriteAllText(Path.Combine(Preferences.Folder,"layout-verification.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
 }
}
