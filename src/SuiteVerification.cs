using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace YouTubeWidget;
internal static class SuiteVerification {
 internal static async Task Run(MainWindow w) {
  var checks=new Dictionary<string,bool>();
  try {
   await Task.Delay(600);w.UpdateLayout();
   checks["StartsExpanded"]=Math.Abs(w.Width-640)<1;
   w.prefs.ExpandedPlayer=false;w.Position();w.UpdateLayout();
   checks["StandardWidth"]=Math.Abs(w.Width-360)<1;checks["StandardHeight"]=w.Height>=310&&w.Height<=325;
   checks["MainExcludedFromTaskbar"]=NativePosition.TaskbarExcluded(w);
   checks["HeaderAlignment"]=SuiteChrome.Aligned(w,false,40);checks["TransparentWindow"]=w.AllowsTransparency&&w.Background==Brushes.Transparent;
   checks["OpaqueContent"]=w.Opacity==1;checks["RoundedSurface"]=w.surface.CornerRadius.TopLeft==8;
   checks["PlayerViewport"]=w.web.Mode is "Composition" or "OwnedOpaqueViewport";
   checks["PinSeparateFromTopmost"]=w.prefs.Pinned&&!w.Topmost;
   checks["FocusDismissalPolicy"]=SuiteActivation.VerifyFocusPolicy();
   w.ShowSettings();await Task.Delay(400);
   checks["SettingsAbove"]=SuitePlacement.IsAbove(w,w.settingsWindow);checks["SettingsHeaderAligned"]=SuiteChrome.Aligned(w.settingsWindow!,true);
   checks["SettingsExcludedFromTaskbar"]=NativePosition.TaskbarExcluded(w.settingsWindow!);
   checks["SameWidth"]=Math.Abs(w.settingsWindow!.Width-w.Width)<1;
   w.settingsScroll!.ScrollToEnd();w.settingsWindow.UpdateLayout();checks["SettingsBottomReachable"]=Math.Abs(w.settingsScroll.VerticalOffset-w.settingsScroll.ScrollableHeight)<1;
   checks["NoHorizontalScroll"]=w.settingsScroll.ScrollableWidth<1;checks["NoVisibleScrollbar"]=w.settingsScroll.VerticalScrollBarVisibility==ScrollBarVisibility.Hidden;
   foreach(double scale in new[]{1.5,2.0}){w.ApplyTextScale(scale);w.UpdateLayout();w.settingsWindow!.UpdateLayout();checks["TextScale"+scale]=(w.surface.Child is Grid scaled&&Math.Abs(scaled.RowDefinitions[0].ActualHeight-40*scale)<1&&w.settingsScroll.ScrollableWidth<1);}w.ApplyTextScale(1);
   foreach(var theme in new[]{"Light","Dark","System"}){w.prefs.Theme=theme;w.ApplyAppearance();checks["Theme"+theme]=Application.Current.Resources["Ink"] is SolidColorBrush;}
   foreach(var material in new[]{0,1,2}){w.prefs.Material=material;w.ApplyAppearance();checks["Material"+material]=w.surface.Background is SolidColorBrush brush&&brush.Color.A>0;}
   w.prefs.Theme="System";w.prefs.Material=0;w.prefs.ExpandedPlayer=true;w.Position();checks["ExpandedWidth"]=w.Width>360;w.prefs.ExpandedPlayer=false;
   w.CloseSettings();await Task.Delay(400);checks["SettingsClosePreservesWidget"]=w.settingsWindow==null&&w.IsVisible;
   w.ShowSettings();await Task.Delay(350);checks["SettingsCanReopen"]=w.PanelVisible;w.CloseSettings(false);
   w.HideWidget();await Task.Delay(450);checks["Recollect"]=!w.IsVisible||w.WindowState==WindowState.Minimized;w.ShowWidget();await Task.Delay(400);checks["Reopen"]=w.IsVisible&&w.WindowState==WindowState.Normal;
   w.Save();checks["PreferencesPersisted"]=Preferences.Load().Theme=="System"&&Preferences.Load().Material==0;
   w.prefs.Theme="Dark";w.ApplyAppearance();
   foreach(var check in await w.VerifyFunctions())checks[check.Key]=check.Value;

  }catch(Exception e){checks["UnexpectedError"]=false;App.Log(e);}
  var report=new{At=DateTimeOffset.Now,Checks=checks,Passed=checks.Values.All(x=>x),Limits="Conta e extensão no Chrome, hardware ARM64 e reinício físico não verificados."};
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"suite-verification.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
  if(!Environment.GetCommandLineArgs().Contains("--inspect"))w.ExitApp();
 }
}


