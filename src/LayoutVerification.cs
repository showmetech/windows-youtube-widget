using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 internal bool FooterLayoutValid(){
  UpdateLayout();Rect Bounds(FrameworkElement element){var point=element.TranslatePoint(new Point(),this);return new Rect(point,new Size(element.ActualWidth,element.ActualHeight));}
  var ordered=new FrameworkElement[]{config,pin,expand,backButton,playButton,forwardButton,muteButton,volume,close};
  var bounds=ordered.Select(Bounds).ToArray();double center=(Bounds(backButton).Left+Bounds(forwardButton).Right)/2;
  return Math.Abs(center-ActualWidth/2)<1&&Bounds(playerFrame).Top<=2&&bounds.All(r=>r.Left>=1&&r.Right<=ActualWidth-1&&r.Bottom<=ActualHeight-1)&&bounds.Zip(bounds.Skip(1),(a,b)=>a.Right<=b.Left+.1).All(x=>x)&&bounds.All(r=>Math.Abs((r.Top+r.Bottom)/2-(Bounds(playButton).Top+Bounds(playButton).Bottom)/2)<1);
 }
 internal async System.Threading.Tasks.Task VerifyControlLayout(){
  if(!App.Testing)return;
  var results=new System.Collections.Generic.List<object>();
  foreach(bool expanded in new[]{true,false})foreach(double scale in new[]{1.0,1.5,2.0}){
   prefs.ExpandedPlayer=expanded;ApplyTextScale(scale);Position();UpdateLayout();await System.Threading.Tasks.Task.Delay(100);UpdateLayout();
   results.Add(new{Expanded=expanded,Scale=scale,Width=ActualWidth,Height=ActualHeight,HeaderRemoved=playerFrame.TranslatePoint(new Point(),this).Y<=2,TitleRemoved=videoTitle.Parent==null,VideoTop=playerFrame.TranslatePoint(new Point(),this).Y,Controls=new[]{config,pin,expand,backButton,playButton,forwardButton,muteButton,(FrameworkElement)volume,close}.Select(c=>new{X=c.TranslatePoint(new Point(),this).X,Y=c.TranslatePoint(new Point(),this).Y,W=c.ActualWidth,H=c.ActualHeight}),Passed=FooterLayoutValid()});
  }
  ApplyTextScale(1);prefs.ExpandedPlayer=true;Position();UpdateLayout();
  File.WriteAllText(Path.Combine(Preferences.Folder,"layout-verification.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));ExitApp();
 }
}
