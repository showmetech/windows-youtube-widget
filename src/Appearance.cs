using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace YouTubeWidget;
public sealed partial class MainWindow {
 internal void ApplyAppearance() {
  const string key=@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
  bool light=prefs.Theme=="Light"||(prefs.Theme=="System"&&(int)(Registry.GetValue(key,"AppsUseLightTheme",1)??1)!=0);
  bool contrast=SystemParameters.HighContrast;bool transparency=(int)(Registry.GetValue(key,"EnableTransparency",1)??1)!=0;
  var accent=contrast?SystemColors.HighlightColor:SystemColors.AccentColor;
  string signature=$"{light}/{contrast}/{transparency}/{prefs.Material}/{accent}/{PanelVisible}";
  if(signature==appearanceSignature)return;appearanceSignature=signature;
  var r=Application.Current.Resources;
  r.MergedDictionaries[0]=new ResourceDictionary{Source=new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent."+(light?"Light":"Dark")+".xaml")};
  Brush B(string value)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
  r["Ink"]=contrast?SystemColors.WindowTextBrush:B(light?"#242424":"#FFFFFF");
  r["SecondaryInk"]=contrast?SystemColors.WindowTextBrush:B(light?"#616161":"#D6D6D6");
  r["PlayerChrome"]=contrast?SystemColors.WindowBrush:B(light?"#F3F3F3":"#202020");
  r["BorderLine"]=contrast?SystemColors.WindowTextBrush:B(light?"#D1D1D1":"#525252");
  r["TileBrush"]=contrast?SystemColors.ControlBrush:B(light?"#FFFFFF":"#3D3D3D");
  r["CardBrush"]=contrast?SystemColors.WindowBrush:B(light?"#FFFFFF":"#292929");
  r["ControlHoverOverlay"]=contrast?Brushes.Transparent:B(light?"#0A000000":"#0FFFFFFF");r["ControlPressedOverlay"]=contrast?Brushes.Transparent:B(light?"#14000000":"#18FFFFFF");
  r["NeutralTrack"]=contrast?SystemColors.WindowTextBrush:B(light?"#707070":"#ADADAD");
  double Linear(byte value){double c=value/255.0;return c<=.04045?c/12.92:Math.Pow((c+.055)/1.055,2.4);}
  double luminance=.2126*Linear(accent.R)+.7152*Linear(accent.G)+.0722*Linear(accent.B);
  r["Accent"]=new SolidColorBrush(accent);r["AccentInk"]=contrast?SystemColors.HighlightTextBrush:luminance>.179?Brushes.Black:Brushes.White;
  Topmost=prefs.AlwaysOnTop;
  foreach(var pair in new[]{(window:(Window)this,border:surface),(window:settingsWindow,border:settingsSurface)}) {
   if(pair.window==null||pair.border==null)continue;
   pair.window.Topmost=prefs.AlwaysOnTop;var hwnd=new WindowInteropHelper(pair.window).Handle;if(hwnd==IntPtr.Zero)continue;
   int material=prefs.Material;
   pair.border.Background=!transparency||contrast?(contrast?SystemColors.WindowBrush:B(light?"#F3F3F3":"#202020")):SuiteWindow.Material(pair.window,HwndSource.FromHwnd(hwnd),material,light,transparency,contrast,out _);
   NativePosition.Decorate(pair.window,!light);
  }
  bool trayLight=(int)(Registry.GetValue(key,"SystemUsesLightTheme",1)??1)!=0;
  tray?.UpdateIcon(Path.Combine(AppContext.BaseDirectory,"Assets","App.ico"));
  UpdateActions();
 }
 internal static double ColorContrast(Color a,Color b){double L(Color color){double C(byte v){double c=v/255.0;return c<=.04045?c/12.92:Math.Pow((c+.055)/1.055,2.4);}return .2126*C(color.R)+.7152*C(color.G)+.0722*C(color.B);}double x=L(a),y=L(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
 void UpdateActions() {
  if(star!=null){bool saved=current!=null&&prefs.Favorites.Any(x=>x.Id==current.Id&&x.Playlist==current.Playlist);if(star.Content is TextBlock glyph)glyph.Text=saved?"\uE735":"\uE734";star.ToolTip=saved?"Remover dos favoritos":"Salvar nos favoritos";AutomationProperties.SetName(star,(string)star.ToolTip);}
  if(expand!=null){if(expand.Content is TextBlock glyph)glyph.Text=prefs.ExpandedPlayer?"\uE73F":"\uE740";expand.ToolTip=prefs.ExpandedPlayer?"Reduzir vídeo":"Ampliar vídeo";AutomationProperties.SetName(expand,(string)expand.ToolTip);}
  foreach(var pair in new[]{(button:pin,active:prefs.Pinned),(button:config,active:PanelVisible)}) {
   if(pair.button==null)continue;
   if(pair.active)pair.button.SetResourceReference(Control.BackgroundProperty,"Accent");else pair.button.Background=Brushes.Transparent;
   pair.button.SetResourceReference(Control.ForegroundProperty,pair.active?"AccentInk":"Ink");
  }
  if(pin!=null){pin.ToolTip=prefs.Pinned?"Desafixar · recolher ao clicar fora":"Fixar · manter aberto";AutomationProperties.SetName(pin,(string)pin.ToolTip);AutomationProperties.SetItemStatus(pin,prefs.Pinned?"Fixado":"Não fixado");}
 }
}
