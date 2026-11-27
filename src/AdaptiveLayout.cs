using System;
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
  double video=Math.Max(1,Math.Min((width-2)*9/16,maxHeight-chrome));
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
 internal void AdaptSettingsNavigation(double availableWidth) {
  if(settingsTabs==null)return;
  bool narrow=availableWidth<680;settingsTabs.TabStripPlacement=narrow?Dock.Top:Dock.Left;
  settingsTabs.Margin=new Thickness(narrow?8:16,0,narrow?8:16,0);settingsTabs.Padding=new Thickness(narrow?4:12);
  foreach(TabItem item in settingsTabs.Items)if(item.Header is StackPanel row&&row.Children.Count==2){
   row.Children[1].Visibility=narrow?Visibility.Collapsed:Visibility.Visible;
   row.Children[0].SetValue(FrameworkElement.MarginProperty,narrow?new Thickness(0):new Thickness(0,0,8,0));
   item.ToolTip=System.Windows.Automation.AutomationProperties.GetName(item);item.MinWidth=narrow?48:0;
  }
 }
}
