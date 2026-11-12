using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 internal TabControl? settingsTabs;
 internal FrameworkElement? settingsHeader,settingsFooter;
 TextBlock? libraryPageLabel;
 Button? libraryPrevious,libraryNext,libraryOpen,libraryRemove;
 UniformGrid? libraryTabs;
 int libraryPage;
 const int LibraryPageSize=3;
 void ToggleSettings(){if(PanelVisible)CloseSettings();else ShowSettings();}
 internal void ShowSettings(){
  Touch();if(PanelVisible){if(panelDismissing){panelReveal?.Dispose();panelDismissing=false;}settingsWindow!.Activate();return;}
  settingsWindow=new Window{Title="Configurações · YouTube",Owner=this,Width=780,Height=560,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,FontFamily=FontFamily,FontSize=14,UseLayoutRounding=true,SnapsToDevicePixels=true,Topmost=prefs.AlwaysOnTop};
  settingsWindow.SetResourceReference(ForegroundProperty,"Ink");
  settingsSurface=new Border{CornerRadius=new(8),BorderThickness=new(1)};settingsSurface.SetResourceReference(Border.BorderBrushProperty,"BorderLine");settingsWindow.Content=settingsSurface;
  var root=new Grid();root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new());root.RowDefinitions.Add(new(){Height=GridLength.Auto});settingsSurface.Child=root;
  settingsHeader=SuiteChrome.Header(Text("Configurações",20,true),IconButton("\uE8BB","Fechar configurações",()=>CloseSettings()));root.Children.Add(settingsHeader);
  settingsTabs=new TabControl{TabStripPlacement=Dock.Left,Margin=new(16,0,16,0),Padding=new(12),BorderThickness=new(0)};settingsTabs.SetResourceReference(BackgroundProperty,"PlayerChrome");settingsTabs.SetResourceReference(ForegroundProperty,"Ink");Grid.SetRow(settingsTabs,1);root.Children.Add(settingsTabs);
  StackPanel Page(string glyph,string label,string subtitle){var body=new StackPanel();body.Children.Add(Text(label,16,true));body.Children.Add(Text(subtitle,11,false,"SecondaryInk",new(0,2,0,12)));var item=new TabItem{Header=IconLabel(glyph,label),Content=body,Padding=new(10,10,10,10),HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch};AutomationProperties.SetName(item,label);settingsTabs.Items.Add(item);return body;}
  var general=Page("\uE713","Geral","Sua conta e o comportamento do aplicativo.");
  general.Children.Add(Text("YouTube conectado pelo Chrome",12,true));
  general.Children.Add(Text(StoreIntegration.LastError.Length>0?"Conexão indisponível. Confira a extensão no Chrome.":"Use a conta já conectada no Chrome. A extensão deve estar ativa.",11,false,"SecondaryInk",new(0,4,0,8)));
  general.Children.Add(SettingsAction("\uE774","Conectar ao YouTube",Connect));
  var recommendations=Check("Receber recomendações",prefs.RecommendationsEnabled);recommendations.ToolTip="Ao abrir pela bandeja, busca uma nova recomendação da sua conta.";recommendations.Click+=(_,_)=>{prefs.RecommendationsEnabled=recommendations.IsChecked==true;Save();};general.Children.Add(recommendations);
  var startupState=StartupState();var startup=Check("Iniciar com o Windows",startupState.Enabled);startup.IsEnabled=startupState.Available;startup.Click+=async(_,_)=>{try{if(StoreIntegration.IsPackaged){await StoreIntegration.SetStartup(startup.IsChecked==true);startup.IsChecked=await StoreIntegration.StartupState()==Windows.ApplicationModel.StartupTaskState.Enabled;}else SetStartup(startup.IsChecked==true);}catch(Exception e){startup.IsChecked=StartupState().Enabled;SetStatus("Não foi possível alterar a inicialização.",true);App.Log(e);}};general.Children.Add(startup);if(StoreIntegration.IsPackaged)_=ReadPackagedStartup(startup);if(!startupState.Available)startup.ToolTip="Inicialização indisponível neste ambiente.";
  var appearance=Page("\uE790","Aparência","Personalize o visual do widget.");appearance.Children.Add(Text("Tema",12,true));
  var themes=new UniformGrid{Rows=1,Columns=3,Margin=new(0,6,0,10)};
  foreach(var option in new[]{("System","Sistema"),("Dark","Escuro"),("Light","Claro")}){var b=ActionButton(option.Item2,()=>{prefs.Theme=option.Item1;appearanceSignature="";ApplyAppearance();UpdateThemeChoices(themes);Save();});b.Tag=option.Item1;b.Margin=new(0,0,4,0);b.FontSize=12;themes.Children.Add(b);}appearance.Children.Add(themes);UpdateThemeChoices(themes);
  appearance.Children.Add(Text("Opacidade do fundo",12,true));var materials=new UniformGrid{Rows=1,Columns=3,Margin=new(0,6,0,8)};
  void UpdateMaterials(){foreach(Button choice in materials.Children){bool active=(int)choice.Tag==prefs.Material;choice.SetResourceReference(Control.BackgroundProperty,active?"Accent":"TileBrush");choice.SetResourceReference(Control.ForegroundProperty,active?"AccentInk":"Ink");}}
  foreach(var option in new[]{(0,"100% opaco"),(1,"82%"),(2,"57%")}){var choice=ActionButton(option.Item2,()=>{prefs.Material=option.Item1;appearanceSignature="";ApplyAppearance();UpdateMaterials();Save();});choice.Tag=option.Item1;choice.FontSize=12;choice.Margin=new(0,0,4,0);AutomationProperties.SetName(choice,"Opacidade do fundo: "+option.Item2);materials.Children.Add(choice);}appearance.Children.Add(materials);UpdateMaterials();
  var animations=Check("Animações",prefs.Animations);animations.Click+=(_,_)=>{prefs.Animations=animations.IsChecked==true;Save();};appearance.Children.Add(animations);
  var above=Check("Sempre acima das outras janelas",prefs.AlwaysOnTop);above.Click+=(_,_)=>{prefs.AlwaysOnTop=above.IsChecked==true;ApplyAppearance();Save();};appearance.Children.Add(above);
  var playback=Page("\uE768","Reprodução","Links e sequência de vídeos.");playback.Children.Add(Text("Link de vídeo ou playlist",12,true));
  var linkRow=new DockPanel{Margin=new(0,8,0,8)};var open=IconButton("\uE768","Abrir link",async()=>await OpenInput());DockPanel.SetDock(open,Dock.Right);linkRow.Children.Add(open);input.FontSize=12;input.Margin=new(0,0,8,0);linkRow.Children.Add(input);playback.Children.Add(linkRow);
  playback.Children.Add(SettingsAction("\uE710","Adicionar link à fila",()=>{try{prefs.Queue.Add(Video.Parse(input.Text));Save();RefreshLibrary();SetStatus("Link adicionado à fila.",true);}catch(Exception e){SetStatus(e.Message,true);}}));
  var queue=Check("Reproduzir o próximo ao terminar",prefs.ContinueQueue);queue.Click+=(_,_)=>{prefs.ContinueQueue=queue.IsChecked==true;Save();};playback.Children.Add(queue);playback.Children.Add(Text("Recolher o widget pausa o vídeo.",11,false,"SecondaryInk",new(0,8,0,0)));
  var lists=Page("\uE8F1","Biblioteca","Sua fila e seus vídeos favoritos.");
  var listGrid=new Grid();listGrid.RowDefinitions.Add(new(){Height=GridLength.Auto});listGrid.RowDefinitions.Add(new());listGrid.RowDefinitions.Add(new(){Height=GridLength.Auto});listGrid.RowDefinitions.Add(new(){Height=GridLength.Auto});
  var tabs=new UniformGrid{Rows=1,Columns=2,Margin=new(0,0,0,8)};libraryTabs=tabs;foreach(var name in new[]{"Fila","Favoritos"}){var choice=SettingsAction(name=="Fila"?"\uE8FD":"\uE734",name,()=>{listKind=name;libraryPage=0;RefreshLibrary();});choice.Tag=name;choice.Margin=new(0,0,4,0);tabs.Children.Add(choice);}listGrid.Children.Add(tabs);
  library.MinHeight=0;library.MaxHeight=double.PositiveInfinity;library.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);library.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);library.HorizontalContentAlignment=HorizontalAlignment.Stretch;Grid.SetRow(library,1);listGrid.Children.Add(library);
  var paging=new Grid{Margin=new(0,6,0,6)};paging.ColumnDefinitions.Add(new(){Width=GridLength.Auto});paging.ColumnDefinitions.Add(new());paging.ColumnDefinitions.Add(new(){Width=GridLength.Auto});libraryPrevious=IconButton("\uE76B","Página anterior",()=>{libraryPage--;RefreshLibrary();});libraryNext=IconButton("\uE76C","Próxima página",()=>{libraryPage++;RefreshLibrary();});libraryPageLabel=Text("",11,false,"SecondaryInk");libraryPageLabel.HorizontalAlignment=HorizontalAlignment.Center;paging.Children.Add(libraryPrevious);Grid.SetColumn(libraryPageLabel,1);paging.Children.Add(libraryPageLabel);Grid.SetColumn(libraryNext,2);paging.Children.Add(libraryNext);Grid.SetRow(paging,2);listGrid.Children.Add(paging);
  var actions=new UniformGrid{Rows=1,Columns=2};libraryOpen=SettingsAction("\uE768","Abrir",async()=>await OpenSelected());libraryRemove=SettingsAction("\uE74D","Remover",RemoveSelected);actions.Children.Add(libraryOpen);actions.Children.Add(libraryRemove);Grid.SetRow(actions,3);listGrid.Children.Add(actions);
  var libraryBody=new Grid();libraryBody.RowDefinitions.Add(new(){Height=GridLength.Auto});libraryBody.RowDefinitions.Add(new(){Height=GridLength.Auto});libraryBody.RowDefinitions.Add(new());while(lists.Children.Count>0){var child=lists.Children[0];lists.Children.RemoveAt(0);Grid.SetRow(child,libraryBody.Children.Count);libraryBody.Children.Add(child);}Grid.SetRow(listGrid,2);libraryBody.Children.Add(listGrid);((TabItem)settingsTabs.Items[3]).Content=libraryBody;
  var footer=new Grid{Margin=new(20,6,20,8)};footer.ColumnDefinitions.Add(new());footer.ColumnDefinitions.Add(new(){Width=GridLength.Auto});var info=Text("v"+typeof(App).Assembly.GetName().Version?.ToString(3)+" · Dados somente neste PC",11,false,"SecondaryInk");footer.Children.Add(info);var exit=SettingsAction("\uE7E8","Sair",ExitApp);Grid.SetColumn(exit,1);footer.Children.Add(exit);settingsFooter=footer;Grid.SetRow(footer,2);root.Children.Add(footer);
  settingsTabs.SelectionChanged+=(_,_)=>Touch();RefreshLibrary();
  settingsWindow.PreviewMouseMove+=(_,_)=>Touch();settingsWindow.PreviewMouseDown+=(_,_)=>Touch();settingsWindow.PreviewKeyDown+=(_,e)=>{Touch();if(e.Key==System.Windows.Input.Key.Escape){CloseSettings();e.Handled=true;}};
  settingsWindow.Closing+=(_,e)=>{if(!settingsClosing){e.Cancel=true;CloseSettings();}};
  PositionSettings();settingsWindow.Show();appearanceSignature="";ApplyAppearance();settingsWindow.Activate();ApplyTextScale(textScale);Reveal(settingsWindow,true);UpdateActions();
 }
 internal void PositionSettings(){
  if(settingsWindow==null)return;var monitor=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);var dpi=VisualTreeHelper.GetDpi(this);var area=monitor.WorkingArea;
  double left=area.Left/dpi.DpiScaleX,top=area.Top/dpi.DpiScaleY,width=area.Width/dpi.DpiScaleX,height=area.Height/dpi.DpiScaleY;
  settingsWindow.Width=Math.Min(780,width-24);settingsWindow.Height=Math.Min(500+80*(textScale-1),height-24);settingsWindow.Left=left+width-settingsWindow.Width-12;settingsWindow.Top=top+(height-settingsWindow.Height)/2;
 }
 static StackPanel IconLabel(string glyph,string label){var row=new StackPanel{Orientation=Orientation.Horizontal};var icon=Text(glyph,14);icon.FontFamily=new("Segoe Fluent Icons");icon.Margin=new(0,0,8,0);row.Children.Add(icon);row.Children.Add(Text(label,12));return row;}
 static Button SettingsAction(string glyph,string label,Action action){var button=ActionButton(label,action);button.Content=IconLabel(glyph,label);button.Padding=new(8,6,8,6);button.FontSize=12;return button;}
 void UpdateLibraryActions(){bool selected=library.SelectedItem is ListBoxItem item&&item.Tag is Video;if(libraryOpen!=null)libraryOpen.IsEnabled=selected;if(libraryRemove!=null)libraryRemove.IsEnabled=selected;}
 bool panelDismissing;
 internal void CloseSettings(bool animate=true){
  if(settingsWindow==null||(panelDismissing&&animate))return;panelDismissing=true;panelReveal?.Dispose();var panel=settingsWindow;
  void Finish(){if(settingsWindow!=panel)return;panelDismissing=false;if(input.Parent is Panel owner)owner.Children.Remove(input);if(library.Parent is Panel listOwner)listOwner.Children.Remove(library);settingsClosing=true;panel.Close();settingsClosing=false;settingsWindow=null;settingsSurface=null;settingsTabs=null;settingsHeader=settingsFooter=null;libraryPageLabel=null;libraryPrevious=libraryNext=libraryOpen=libraryRemove=null;libraryTabs=null;Position();appearanceSignature="";ApplyAppearance();UpdateActions();if(animate&&IsVisible&&!animating)Activate();Touch();}
  if(animate&&panel.IsVisible)Reveal(panel,false,Finish);else Finish();
 }
 void UpdateThemeChoices(UniformGrid themes){foreach(var element in themes.Children)if(element is Button button){bool active=(string?)button.Tag==prefs.Theme;button.SetResourceReference(Control.BackgroundProperty,active?"Accent":"TileBrush");button.SetResourceReference(Control.ForegroundProperty,active?"AccentInk":"Ink");}}
 static CheckBox Check(string text,bool enabled){var box=new CheckBox{Content=text,IsChecked=enabled,Margin=new(0,6,0,6),FontSize=12};box.SetResourceReference(Control.ForegroundProperty,"Ink");AutomationProperties.SetName(box,text);return box;}
 void Connect(){External("https://www.youtube.com/");var guide=Path.Combine(AppContext.BaseDirectory,"CONECTAR-YOUTUBE.md");if(File.Exists(guide))Process.Start(new ProcessStartInfo(guide){UseShellExecute=true});else SetStatus("Ative a extensão YouTube Widget no Chrome. Consulte CONECTAR-YOUTUBE.md.");}
 async System.Threading.Tasks.Task ReadPackagedStartup(CheckBox control){try{var state=await StoreIntegration.StartupState();control.IsChecked=state is Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;control.IsEnabled=state is not(Windows.ApplicationModel.StartupTaskState.DisabledByUser or Windows.ApplicationModel.StartupTaskState.DisabledByPolicy or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy);if(!control.IsEnabled)control.ToolTip="Gerencie a inicialização nas Configurações do Windows.";}catch(Exception error){control.IsEnabled=false;App.Log(error);}}
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 static (bool Enabled,bool Available) StartupState(){if(StoreIntegration.IsPackaged)return(false,true);try{using var key=Registry.CurrentUser.OpenSubKey(RunKey);return ((key?.GetValue("WindowsYouTubeWidget") as string)?.Contains(Environment.ProcessPath??"<none>",StringComparison.OrdinalIgnoreCase)==true,true);}catch(System.Security.SecurityException){return(false,false);}catch(UnauthorizedAccessException){return(false,false);}}
 static void SetStartup(bool enabled){using var key=Registry.CurrentUser.CreateSubKey(RunKey);if(enabled)key.SetValue("WindowsYouTubeWidget","\""+Environment.ProcessPath+"\" --startup",RegistryValueKind.String);else key.DeleteValue("WindowsYouTubeWidget",false);}
}






