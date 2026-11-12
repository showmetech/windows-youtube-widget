using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 static IEnumerable<DependencyObject> SettingsVisuals(DependencyObject root){yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in SettingsVisuals(VisualTreeHelper.GetChild(root,i)))yield return child;}
 internal bool SettingsInsideScreen(){if(settingsWindow==null)return false;var area=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).WorkingArea;var dpi=VisualTreeHelper.GetDpi(this);return settingsWindow.Left>=area.Left/dpi.DpiScaleX&&settingsWindow.Top>=area.Top/dpi.DpiScaleY&&settingsWindow.Left+settingsWindow.Width<=area.Right/dpi.DpiScaleX+1&&settingsWindow.Top+settingsWindow.Height<=area.Bottom/dpi.DpiScaleY+1;}
 internal bool SettingsPageFits(){
  if(settingsWindow==null||settingsTabs==null)return false;settingsWindow.UpdateLayout();bool Fail(string message){if(App.Testing)File.AppendAllText(Path.Combine(Preferences.Folder,"settings-fit.log"),$"Page {settingsTabs.SelectedIndex} scale {textScale}: {message}\n");return false;}
  var page=(FrameworkElement)((TabItem)settingsTabs.SelectedItem).Content;
  if(page.DesiredSize.Height>page.ActualHeight+2||page.DesiredSize.Width>page.ActualWidth+2)return Fail($"page desired {page.DesiredSize}, actual {page.ActualWidth}x{page.ActualHeight}");
  foreach(var element in SettingsVisuals(settingsSurface!).OfType<FrameworkElement>().Where(e=>e.IsVisible&&e is Button or CheckBox or ComboBox or TextBox or ListBox)){
   var point=element.TranslatePoint(new Point(),settingsWindow);if(point.X<0||point.Y<0||point.X+element.ActualWidth>settingsWindow.ActualWidth+2||point.Y+element.ActualHeight>settingsWindow.ActualHeight+2)return Fail($"{element.GetType().Name} {point} {element.ActualWidth}x{element.ActualHeight}");
  }
  foreach(var scroll in SettingsVisuals(settingsSurface!).OfType<ScrollViewer>().Where(s=>s.IsVisible))if(scroll.ScrollableHeight>=1||(scroll.TemplatedParent is not TextBox&&scroll.ScrollableWidth>=1))return Fail($"Scroll {scroll.ScrollableWidth}x{scroll.ScrollableHeight}, parent {scroll.TemplatedParent?.GetType().Name}");return true;
 }
 internal async Task<Dictionary<string,bool>> VerifySettingsPages(){
  var checks=new Dictionary<string,bool>();if(settingsTabs==null)return checks;
  foreach(double scale in new[]{1.0,1.5,2.0}){
   ApplyTextScale(scale);foreach(int page in Enumerable.Range(0,4)){settingsTabs.SelectedIndex=page;settingsWindow!.UpdateLayout();await Task.Delay(60);checks[$"SettingsPage{page}Scale{scale}"]=SettingsInsideScreen()&&SettingsPageFits();}
  }
  ApplyTextScale(1);settingsTabs.SelectedIndex=1;settingsWindow!.UpdateLayout();
  string originalTheme=prefs.Theme;int originalMaterial=prefs.Material;
  Button ThemeButton(string value)=>SettingsVisuals(settingsSurface!).OfType<Button>().First(b=>b.Tag is string tag&&tag==value);
  Button MaterialButton(int value)=>SettingsVisuals(settingsSurface!).OfType<Button>().First(b=>b.Tag is int tag&&tag==value);
  ThemeButton("Light").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["ThemeChoiceUpdatesPreference"]=prefs.Theme=="Light";
  MaterialButton(1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["OpacityChoiceUpdatesSurface"]=prefs.Material==1&&surface.Background is SolidColorBrush tinted&&tinted.Color.A==210;
  MaterialButton(0).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["OpaqueChoiceUpdatesBothSurfaces"]=surface.Background is SolidColorBrush opaque&&opaque.Color.A==255&&settingsSurface!.Background is SolidColorBrush panel&&panel.Color.A==255;
  MaterialButton(originalMaterial).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));ThemeButton(originalTheme).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));settingsTabs.SelectedIndex=3;

  var originalQueue=prefs.Queue.ToList();string originalKind=listKind;int originalPage=libraryPage;
  try{
   prefs.Queue.Clear();prefs.Queue.AddRange(Enumerable.Range(0,8).Select(_=>new Video("M7lc1UVf-VE")));listKind="Fila";libraryPage=0;RefreshLibrary();
   checks["LibraryFirstPage"]=library.Items.Count==3&&libraryPrevious?.IsEnabled==false&&libraryNext?.IsEnabled==true;
   libraryNext!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["LibraryNextPage"]=libraryPage==1&&library.Items.Count==3;
   libraryNext.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["LibraryLastPage"]=libraryPage==2&&library.Items.Count==2&&!libraryNext.IsEnabled;
   libraryPrevious!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["LibraryPreviousPage"]=libraryPage==1;library.SelectedIndex=0;checks["LibrarySelectionEnablesActions"]=libraryOpen?.IsEnabled==true&&libraryRemove?.IsEnabled==true;libraryRemove!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));checks["LibraryRemoveAction"]=prefs.Queue.Count==7;ApplyTextScale(2);settingsWindow!.UpdateLayout();checks["LibraryPopulatedAt200Percent"]=SettingsInsideScreen()&&SettingsPageFits();ApplyTextScale(1);
  }finally{prefs.Queue=originalQueue;listKind=originalKind;libraryPage=originalPage;RefreshLibrary();}
  settingsTabs.SelectedIndex=0;return checks;
 }
 internal async Task VerifySettingsLayout(){
  if(!App.Testing)return;ShowSettings();await Task.Delay(250);var checks=await VerifySettingsPages();
  foreach(int page in Enumerable.Range(0,4)){
   settingsTabs!.SelectedIndex=page;settingsWindow!.UpdateLayout();await Task.Delay(100);
   var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawRectangle(new VisualBrush(settingsSurface!),null,new Rect(0,0,settingsSurface!.ActualWidth,settingsSurface.ActualHeight));
   var image=new RenderTargetBitmap((int)Math.Ceiling(settingsSurface!.ActualWidth),(int)Math.Ceiling(settingsSurface.ActualHeight),96,96,PixelFormats.Pbgra32);image.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(Preferences.Folder,$"settings-page-{page}.png"));encoder.Save(file);
  }
  File.WriteAllText(Path.Combine(Preferences.Folder,"settings-layout.json"),JsonSerializer.Serialize(new{Checks=checks,Passed=checks.Values.All(x=>x)},new JsonSerializerOptions{WriteIndented=true}));ExitApp();
 }
}
