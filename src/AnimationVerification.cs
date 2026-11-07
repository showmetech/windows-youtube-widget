using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 internal async Task VerifyAnimations(){
  if(!App.Testing)return;
  var checks=new Dictionary<string,bool>();prefs.Animations=true;
  await Task.Delay(350);
  ShowWidget();var initial=reveal;ShowWidget();checks["RepeatedShowDoesNotRestart"]=ReferenceEquals(initial,reveal);
  HideWidget();await Task.Delay(80);double closing=reveal?.Progress??0;ShowWidget();double reopening=reveal?.Progress??0;
  checks["ReverseContinuesFromCurrentProgress"]=Math.Abs(closing-reopening)<0.02;
  await Task.Delay(350);checks["ReopenCompletes"]=IsVisible&&!animating&&Math.Abs(reveal!.Progress-1)<0.01;
  HideWidget();await Task.Delay(350);checks["CloseCompletes"]=!IsVisible&&!animating;
  ShowWidget();await Task.Delay(350);checks["OpenCompletes"]=IsVisible&&Math.Abs(reveal!.Progress-1)<0.01;
  int frames=reveal?.Frames??0;double maxGap=reveal?.MaxFrameGapMs??0;checks["FramesRendered"]=frames>=3;checks["TaskbarExcluded"]=NativePosition.TaskbarExcluded(this);
  prefs.Animations=false;HideWidget();checks["AnimationsDisabledHide"]=!IsVisible;ShowWidget();checks["AnimationsDisabledShow"]=IsVisible;
  File.WriteAllText(Path.Combine(Preferences.Folder,"animation-verification.json"),JsonSerializer.Serialize(new{Checks=checks,Frames=frames,MaxFrameGapMs=maxGap},new JsonSerializerOptions{WriteIndented=true}));
  ExitApp();
 }
}
