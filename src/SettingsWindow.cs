using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Win32;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 void ToggleSettings(){if(PanelVisible)CloseSettings();else ShowSettings();}
 internal void ShowSettings() {
  Touch();if(PanelVisible){if(panelDismissing){panelReveal?.Dispose();panelDismissing=false;}settingsWindow!.Activate();return;}
  settingsWindow=new Window{Title="Configurações · YouTube",Owner=this,Width=Width,Height=400,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,FontFamily=FontFamily,FontSize=14,UseLayoutRounding=true,SnapsToDevicePixels=true,Topmost=prefs.AlwaysOnTop};
  settingsWindow.SetResourceReference(ForegroundProperty,"Ink");
  settingsSurface=new Border{CornerRadius=new(8),BorderThickness=new(1)};settingsSurface.SetResourceReference(Border.BorderBrushProperty,"BorderLine");settingsWindow.Content=settingsSurface;
  var layout=new DockPanel();settingsSurface.Child=layout;
  var head=SuiteChrome.Header(Text("Configurações",20,true),IconButton("\uE8BB","Fechar configurações",()=>CloseSettings()));DockPanel.SetDock(head,Dock.Top);layout.Children.Add(head);
  var body=new StackPanel();settingsScroll=new ScrollViewer{Content=body};var scrolling=SuiteScroll.Wrap(settingsScroll);scrolling.Margin=new(20,0,20,12);layout.Children.Add(scrolling);
  var account=Group(body,"\uE77B","Conta do YouTube");
  account.Children.Add(Text("Use as recomendações da conta conectada no Chrome.",12,false,"SecondaryInk"));if(StoreIntegration.LastError.Length>0)account.Children.Add(Text("A conexão com o navegador não pôde ser registrada neste Windows. Verifique as permissões ou políticas da organização.",12,false,"SecondaryInk"));
  account.Children.Add(ActionButton("Configurar conexão com o YouTube",Connect));
  var recommendations=Check("Receber recomendações do YouTube",prefs.RecommendationsEnabled);recommendations.Click+=(_,_)=>{prefs.RecommendationsEnabled=recommendations.IsChecked==true;Save();};account.Children.Add(recommendations);
  account.Children.Add(Text("Abra a página inicial do YouTube para receber uma nova recomendação. A extensão precisa estar ativada.",12,false,"SecondaryInk",new(0,4,0,4)));
  var appearance=Group(body,"\uE790","Aparência");appearance.Children.Add(Text("Tema",14,true));
  var themes=new UniformGrid{Rows=1,Columns=3,Margin=new(0,6,0,12)};
  foreach(var option in new[]{("System","Padrão"),("Dark","Escuro"),("Light","Claro")}){var b=ActionButton(option.Item2,()=>{prefs.Theme=option.Item1;appearanceSignature="";ApplyAppearance();UpdateThemeChoices(themes);Save();});b.Tag=option.Item1;b.Margin=new(0,0,4,0);b.FontSize=12;themes.Children.Add(b);}appearance.Children.Add(themes);UpdateThemeChoices(themes);
  appearance.Children.Add(Text("Transparência",14,true));var material=new ComboBox{ItemsSource=new[]{"Opaco · 100% (padrão)","Transparência suave","Transparência intensa"},SelectedIndex=Math.Clamp(prefs.Material,0,2),Margin=new(0,6,0,12)};AutomationProperties.SetName(material,"Transparência");material.SelectionChanged+=(_,_)=>{prefs.Material=material.SelectedIndex;appearanceSignature="";ApplyAppearance();Save();};appearance.Children.Add(material);
  appearance.Children.Add(Text("Textos, controles e vídeo permanecem opacos. Alto contraste usa as cores do Windows.",12,false,"SecondaryInk"));
  var animations=Check("Animações do Windows",prefs.Animations);animations.Click+=(_,_)=>{prefs.Animations=animations.IsChecked==true;Save();};appearance.Children.Add(animations);
  var above=Check("Manter acima de outras janelas",prefs.AlwaysOnTop);above.Click+=(_,_)=>{prefs.AlwaysOnTop=above.IsChecked==true;ApplyAppearance();Topmost=prefs.AlwaysOnTop;settingsWindow!.Topmost=Topmost;Save();};appearance.Children.Add(above);
  var playback=Group(body,"\uE768","Vídeos e reprodução");playback.Children.Add(Text("Abrir um link do YouTube",14,true));
  var linkRow=new DockPanel{Margin=new(0,8,0,4)};var open=IconButton("\uE768","Abrir link",async()=>await OpenInput());DockPanel.SetDock(open,Dock.Right);linkRow.Children.Add(open);input.Margin=new(0,0,8,0);linkRow.Children.Add(input);playback.Children.Add(linkRow);
  playback.Children.Add(ActionButton("Adicionar link à fila",()=>{try{prefs.Queue.Add(Video.Parse(input.Text));Save();RefreshLibrary();SetStatus("Link adicionado à fila.",true);}catch(Exception e){SetStatus(e.Message,true);}}));
  var queue=Check("Reproduzir o próximo vídeo ao terminar",prefs.ContinueQueue);queue.Click+=(_,_)=>{prefs.ContinueQueue=queue.IsChecked==true;Save();};playback.Children.Add(queue);
  playback.Children.Add(Text("Ao recolher o widget, o vídeo é pausado.",12,false,"SecondaryInk"));
  var lists=Group(body,"\uE8FD","Fila e favoritos");var tabs=new UniformGrid{Rows=1,Columns=2,Margin=new(0,0,0,8)};foreach(var name in new[]{"Fila","Favoritos"}){var choice=ActionButton(name,()=>{listKind=name;RefreshLibrary();});choice.Margin=new(0,0,4,0);tabs.Children.Add(choice);}lists.Children.Add(tabs);lists.Children.Add(library);
  lists.Children.Add(ActionButton("Abrir vídeo selecionado",async()=>{if(library.SelectedItem is ListBoxItem selected&&selected.Tag is Video)await OpenSelected();else SetStatus("Selecione um vídeo na lista.",true);}));lists.Children.Add(ActionButton("Remover vídeo selecionado",RemoveSelected));
  var app=Group(body,"\uE7E8","Aplicativo");var startupState=StartupState();var startup=Check("Iniciar com o Windows",startupState.Enabled);startup.IsEnabled=startupState.Available;startup.Click+=async(_,_)=>{try{if(StoreIntegration.IsPackaged){await StoreIntegration.SetStartup(startup.IsChecked==true);startup.IsChecked=await StoreIntegration.StartupState()==Windows.ApplicationModel.StartupTaskState.Enabled;}else SetStartup(startup.IsChecked==true);}catch(Exception e){startup.IsChecked=StartupState().Enabled;SetStatus("Não foi possível alterar a inicialização.");App.Log(e);}};app.Children.Add(startup);if(StoreIntegration.IsPackaged)_ = ReadPackagedStartup(startup);if(!startupState.Available)app.Children.Add(Text("Inicialização indisponível neste ambiente.",12,false,"SecondaryInk"));
  app.Children.Add(Text("YouTube Widget · "+typeof(App).Assembly.GetName().Version?.ToString(3)+"\nDados salvos somente neste PC.",12,false,"SecondaryInk",new(0,8,0,8)));app.Children.Add(ActionButton("Sair do aplicativo",ExitApp));
  settingsWindow.PreviewMouseMove+=(_,_)=>Touch();settingsWindow.PreviewMouseDown+=(_,_)=>Touch();settingsWindow.PreviewKeyDown+=(_,e)=>{Touch();if(e.Key==System.Windows.Input.Key.Escape){CloseSettings();e.Handled=true;}};
  settingsWindow.Closing+=(_,e)=>{if(!settingsClosing){e.Cancel=true;CloseSettings();}};
  SuitePlacement.Above(this,settingsWindow);settingsWindow.Show();appearanceSignature="";ApplyAppearance();settingsWindow.Activate();ApplyTextScale(textScale);Reveal(settingsWindow,true);UpdateActions();
 }
 bool panelDismissing;
 internal void CloseSettings(bool animate=true) {
  if(settingsWindow==null||(panelDismissing&&animate))return;panelDismissing=true;panelReveal?.Dispose();var panel=settingsWindow;
  void Finish(){if(settingsWindow!=panel)return;panelDismissing=false;if(input.Parent is Panel owner)owner.Children.Remove(input);if(library.Parent is Panel listOwner)listOwner.Children.Remove(library);settingsClosing=true;panel.Close();settingsClosing=false;settingsWindow=null;settingsSurface=null;settingsScroll=null;Position();appearanceSignature="";ApplyAppearance();UpdateActions();if(animate&&IsVisible&&!animating)Activate();Touch();}
  if(animate&&panel.IsVisible)Reveal(panel,false,Finish);else Finish();
 }
 static StackPanel Group(StackPanel parent,string glyph,string title){var content=new StackPanel{Margin=new(2,8,2,8)};var caption=new StackPanel{Orientation=Orientation.Horizontal};var symbol=new TextBlock{Text=glyph,FontFamily=new("Segoe Fluent Icons"),FontSize=16,Width=24};symbol.SetResourceReference(TextBlock.ForegroundProperty,"Ink");caption.Children.Add(symbol);caption.Children.Add(Text(title));parent.Children.Add(new Expander{Header=caption,Content=content,IsExpanded=true,Margin=new(0,0,0,10)});return content;}
 void UpdateThemeChoices(UniformGrid themes){foreach(var element in themes.Children)if(element is Button button){bool active=(string?)button.Tag==prefs.Theme;button.SetResourceReference(Control.BackgroundProperty,active?"Accent":"TileBrush");button.SetResourceReference(Control.ForegroundProperty,active?"AccentInk":"Ink");}}
 static CheckBox Check(string text,bool enabled){var box=new CheckBox{Content=text,IsChecked=enabled,Margin=new(0,8,0,8)};box.SetResourceReference(Control.ForegroundProperty,"Ink");AutomationProperties.SetName(box,text);return box;}
 void Connect(){External("https://www.youtube.com/");var guide=Path.Combine(AppContext.BaseDirectory,"CONECTAR-YOUTUBE.md");if(File.Exists(guide))Process.Start(new ProcessStartInfo(guide){UseShellExecute=true});else SetStatus("Ative a extensão YouTube Widget no Chrome. Consulte CONECTAR-YOUTUBE.md.");}
 async System.Threading.Tasks.Task ReadPackagedStartup(CheckBox control){try{var state=await StoreIntegration.StartupState();control.IsChecked=state is Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;control.IsEnabled=state is not(Windows.ApplicationModel.StartupTaskState.DisabledByUser or Windows.ApplicationModel.StartupTaskState.DisabledByPolicy or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy);if(!control.IsEnabled)control.ToolTip="Gerencie a inicialização nas Configurações do Windows.";}catch(Exception error){control.IsEnabled=false;App.Log(error);}}
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 static (bool Enabled,bool Available) StartupState(){if(StoreIntegration.IsPackaged)return(false,true);try{using var key=Registry.CurrentUser.OpenSubKey(RunKey);return ((key?.GetValue("WindowsYouTubeWidget") as string)?.Contains(Environment.ProcessPath??"<none>",StringComparison.OrdinalIgnoreCase)==true,true);}catch(System.Security.SecurityException){return(false,false);}catch(UnauthorizedAccessException){return(false,false);}}
 static void SetStartup(bool enabled){using var key=Registry.CurrentUser.CreateSubKey(RunKey);if(enabled)key.SetValue("WindowsYouTubeWidget","\""+Environment.ProcessPath+"\" --startup",RegistryValueKind.String);else key.DeleteValue("WindowsYouTubeWidget",false);}
}






