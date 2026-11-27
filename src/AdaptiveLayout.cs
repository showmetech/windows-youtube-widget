using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace YouTubeWidget;
internal readonly record struct PlayerLayout(double Width,double VideoHeight,double ChromeHeight,bool Stacked) {
 internal static PlayerLayout Fit(double availableWidth,double availableHeight,bool expanded,double scale) {
  double maxWidth=Math.Max(1,availableWidth-24),maxHeight=Math.Max(1,availableHeight-24);
  double chrome=10+70*scale;
  double width=Math.Min(expanded?640:360,Math.Min(maxWidth,Math.Max(1,(maxHeight-chrome)*16/9+2)));
  bool stacked=width<350;
  if(stacked)chrome+=40;
  width=Math.Min(width,Math.Max(1,(maxHeight-chrome)*16/9+2));
  // The YouTube embed requires at least 200 CSS pixels in each dimension.
  // Below a 16:9 frame of that height, let the official player letterbox the video.
  width=Math.Max(202,width);
  double video=Math.Max(200,Math.Min((width-2)*9/16,maxHeight-chrome));
  return new(width,video,Math.Min(chrome,maxHeight),stacked);
 }
}
public sealed partial class MainWindow {
 internal void AdaptTransport(PlayerLayout plan) {
  bool stacked=plan.Stacked;
  transport.RowDefinitions.Clear();transport.RowDefinitions.Add(new());if(stacked)transport.RowDefinitions.Add(new(){Height=new GridLength(40)});
  transport.Height=48*textScale+(stacked?40:0);
  var windowActions=transport.Children[0];var playback=transport.Children[1];var audio=transport.Children[2];
  Grid.SetRow(windowActions,stacked?1:0);Grid.SetRow(playback,0);Grid.SetRow(audio,stacked?1:0);
  Grid.SetColumn(playback,stacked?0:1);Grid.SetColumnSpan(playback,stacked?3:1);
  volume.Width=plan.Width>=500?64:40;
  transport.ColumnDefinitions[0].Width=transport.ColumnDefinitions[2].Width=new GridLength(stacked?1:plan.Width>=500?136:112,stacked?GridUnitType.Star:GridUnitType.Pixel);
 }
 static IEnumerable<DependencyObject> SettingsLogicalObjects(DependencyObject root){yield return root;foreach(var child in LogicalTreeHelper.GetChildren(root))if(child is DependencyObject item)foreach(var descendant in SettingsLogicalObjects(item))yield return descendant;}
 internal void AdaptSettingsNavigation(double availableWidth) {
  if(settingsTabs==null)return;
  bool narrow=availableWidth<680||availableWidth/textScale<500;
  if(narrow&&settingsWindow!=null){var area=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea;var dpi=VisualTreeHelper.GetDpi(this);settingsWindow.Height=Math.Min(540+260*(textScale-1),area.Height/dpi.DpiScaleY-24);settingsWindow.Top=Math.Clamp(settingsWindow.Top,area.Top/dpi.DpiScaleY+12,area.Bottom/dpi.DpiScaleY-settingsWindow.Height-12);}
  if(themeOptions!=null)themeOptions.Visibility=narrow?Visibility.Collapsed:Visibility.Visible;
  if(opacityOptions!=null)opacityOptions.Visibility=narrow?Visibility.Collapsed:Visibility.Visible;
  syncingAppearanceChoices=true;try{
   if(compactTheme!=null){compactTheme.Visibility=narrow?Visibility.Visible:Visibility.Collapsed;compactTheme.SelectedIndex=prefs.Theme=="Light"?2:prefs.Theme=="Dark"?1:0;}
   if(compactOpacity!=null){compactOpacity.Visibility=narrow?Visibility.Visible:Visibility.Collapsed;compactOpacity.SelectedIndex=Math.Clamp(prefs.Material,0,2);}
  }finally{syncingAppearanceChoices=false;}
  settingsTabs.TabStripPlacement=narrow?Dock.Top:Dock.Left;
  if(settingsHeader is Grid header&&header.Children.Count>0&&header.Children[0] is TextBlock title)title.Text=narrow?"Ajustes":"Configurações";
  if(settingsSurface!=null)foreach(var button in SettingsLogicalObjects(settingsSurface).OfType<Button>())if(button.Content is Grid label&&label.Children.Count==2&&label.Children[0] is TextBlock icon&&icon.FontFamily.Source.Contains("Icons")){icon.Visibility=narrow&&textScale>1.5?Visibility.Collapsed:Visibility.Visible;button.Padding=narrow?new Thickness(4,6,4,6):new Thickness(8,6,8,6);}
  settingsTabs.Margin=new Thickness(narrow?8:16,0,narrow?8:16,0);settingsTabs.Padding=new Thickness(narrow?4:12);
  foreach(TabItem item in settingsTabs.Items)if(item.Header is StackPanel row&&row.Children.Count==2){
   row.Children[1].Visibility=narrow?Visibility.Collapsed:Visibility.Visible;
   row.Children[0].SetValue(FrameworkElement.MarginProperty,narrow?new Thickness(0):new Thickness(0,0,8,0));
   item.ToolTip=System.Windows.Automation.AutomationProperties.GetName(item);item.MinWidth=narrow?48:0;
  }
 }
}
