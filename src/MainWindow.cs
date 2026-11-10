using System.Collections.Generic;using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;

namespace YouTubeWidget;
public sealed partial class MainWindow:Window {
 internal readonly Preferences prefs=Preferences.Load();
 internal readonly Border surface=new(){CornerRadius=new(8),BorderThickness=new(1)};
 internal readonly VideoViewport web=new();
 readonly TextBlock videoTitle=Text("Sua próxima descoberta",16,true),status=Text("Conecte o YouTube nas configurações.",12,false,"SecondaryInk");
 readonly TextBox input=new(); readonly TextBlock contextLabel=Text("PARA VOCÊ",11,true,"SecondaryInk",new(0,14,0,4));
 readonly ListBox library=new(){BorderThickness=new(0),Background=Brushes.Transparent,MinHeight=70};
 readonly DispatcherTimer tick=new(){Interval=TimeSpan.FromSeconds(1)};
 readonly DispatcherTimer feedbackTimeout=new(){Interval=TimeSpan.FromSeconds(5)};
 readonly Button pin,config,close,expand,star,playButton,muteButton;
 internal ScrollViewer? settingsScroll;
 internal Window? settingsWindow;
 internal Border? settingsSurface;
 NativeTray? tray;
 long lastTrayAttempt=long.MinValue;bool trayErrorLogged;
 WindowRevealAnimation? reveal,panelReveal;
 HwndSource? source;
 bool exiting,animating,settingsClosing,initialized,ready,playing,muted;
 long interaction=Environment.TickCount64;
 DateTimeOffset recommendationAt=DateTimeOffset.MinValue;
 string appearanceSignature="",listKind="Fila";
 Video? current;
 readonly string[] args;
 readonly Grid titleFrame=new(){Height=30};
 readonly Border playerFrame=new(){CornerRadius=new(8),Height=200,ClipToBounds=true,Background=Brushes.Black};
 internal bool PanelVisible=>settingsWindow?.IsVisible==true;
 internal bool Animate=>prefs.Animations&&SystemParameters.ClientAreaAnimation;
 public MainWindow(string[] args) {
  prefs.ExpandedPlayer=true;
  this.args=args;Title="YouTube Widget";Width=640;Height=479;MinWidth=1;MinHeight=1;
  WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;
  ShowInTaskbar=false;FontFamily=new("Segoe UI Variable, Segoe UI");FontSize=14;UseLayoutRounding=true;SnapsToDevicePixels=true;
  SetResourceReference(ForegroundProperty,"Ink");surface.SetResourceReference(Border.BorderBrushProperty,"BorderLine");Content=surface;
  var layout=new Grid();layout.RowDefinitions.Add(new(){Height=new(40)});layout.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});surface.Child=layout;
  config=IconButton("\uE713","Configurações",ToggleSettings);
  pin=IconButton("\uE718","Manter aberto",()=>{prefs.Pinned=!prefs.Pinned;Touch();Save();UpdateActions();});
  close=IconButton("\uE8BB","Recolher para a bandeja",HideWidget);
  var header=SuiteChrome.Header(Text("YouTube",16,true),config,pin,close);header.Height=40;header.Background=Brushes.Transparent;
  header.MouseLeftButtonDown+=(_,e)=>{if(e.OriginalSource is TextBlock t&&!t.FontFamily.Source.Contains("Icons"))DragMove();};layout.Children.Add(header);
  var body=new StackPanel{Margin=new(0,0,0,8)};Grid.SetRow(body,1);layout.Children.Add(body);
  playerFrame.Child=web;playerFrame.SizeChanged+=(_,_)=>playerFrame.Clip=new RectangleGeometry(new Rect(0,0,playerFrame.ActualWidth,playerFrame.ActualHeight),8,8);body.Children.Add(playerFrame);
  contextLabel.Visibility=Visibility.Collapsed;
  playButton=IconButton("\uE768","Reproduzir vídeo",()=>PlayerCommand("toggle"));
  muteButton=IconButton("\uE767","Desativar som",()=>PlayerCommand("mute"));
  star=IconButton("\uE734","Salvar nos favoritos",Favorite);
  expand=IconButton("\uE740","Ampliar vídeo",()=>{prefs.ExpandedPlayer=!prefs.ExpandedPlayer;Position();Save();UpdateActions();});
  playerFooter.SetResourceReference(Panel.BackgroundProperty,"PlayerChrome");BuildTimeline(playerFooter);BuildTransport(playerFooter);body.Children.Add(playerFooter);
  library.SetResourceReference(ForegroundProperty,"Ink");library.MaxHeight=145;library.MouseDoubleClick+=async(_,_)=>await OpenSelected();
  library.KeyDown+=async(_,e)=>{if(e.Key==Key.Enter)await OpenSelected();if(e.Key==Key.Delete)RemoveSelected();};
  input.ToolTip="Link de vídeo, Shorts, live ou playlist";AutomationProperties.SetName(input,"Link do YouTube");input.KeyDown+=async(_,e)=>{if(e.Key==Key.Enter)await OpenInput();};
  PreviewMouseMove+=(_,_)=>Touch();PreviewMouseDown+=(_,_)=>Touch();PreviewMouseWheel+=(_,_)=>Touch();
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Q&&(Keyboard.Modifiers&ModifierKeys.Control)!=0){ExitApp();e.Handled=true;return;}Touch();if(e.Key==Key.Escape){if(PanelVisible)CloseSettings();else HideWidget();e.Handled=true;}else if(e.Key==Key.OemComma&&(Keyboard.Modifiers&ModifierKeys.Control)!=0){ToggleSettings();e.Handled=true;}else if(e.Key==Key.L&&(Keyboard.Modifiers&ModifierKeys.Control)!=0){ShowSettings();input.Focus();input.SelectAll();e.Handled=true;}};
  if(!App.Testing)SuiteActivation.Attach(this,HideWidget,()=>prefs.Pinned||trayRefreshActive||handoffActive||PlaybackPending);
  Loaded+=OnLoaded;DpiChanged+=(_,_)=>Position();SystemEvents.DisplaySettingsChanged+=DisplayChanged;SystemEvents.UserPreferenceChanged+=ThemeChanged;
  Closing+=(_,e)=>{if(!exiting){e.Cancel=true;HideWidget();}};
  Closed+=(_,_)=>{DisposeAccessibility();tick.Stop();feedbackTimeout.Stop();reveal?.Dispose();panelReveal?.Dispose();tray?.Dispose();web.Dispose();SystemEvents.UserPreferenceChanged-=ThemeChanged;SystemEvents.DisplaySettingsChanged-=DisplayChanged;};
  SourceInitialized+=(_,_)=>{if(args.Contains("--startup")){ShowActivated=false;NativePosition.Cloak(this,true);}};
  RefreshLibrary();ApplyAppearance();
  feedbackTimeout.Tick+=(_,_)=>{feedbackTimeout.Stop();SetStatus("");};
 }
 async void OnLoaded(object sender,RoutedEventArgs e) {
  if(initialized)return;initialized=true;source=HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
  var iconPath=Path.Combine(AppContext.BaseDirectory,"Assets","App.ico");if(File.Exists(iconPath))Icon=BitmapFrame.Create(new Uri(iconPath));
  EnsureTray();
  Position();appearanceSignature="";ApplyAppearance();InitializeAccessibility();
  tick.Tick+=async(_,_)=>{EnsureTray();WriteHealth();if(!WindowRevealAnimation.IsRunning)ApplyAppearance();await CheckHandoff();await CheckRecommendation();CheckIdle();if(args.Contains("--inspect")){try{File.WriteAllText(Path.Combine(Preferences.Folder,"ui-state.json"),System.Text.Json.JsonSerializer.Serialize(new{At=DateTimeOffset.Now,Visible=IsVisible,Width=ActualWidth,Height=ActualHeight,TrayRegistered=tray?.TryBounds(out _)==true,PlayerMode=web.Mode,Playing=playing,Muted=muted,PanelVisible,TaskbarExcluded=NativePosition.TaskbarExcluded(this),SeekSeconds=seek.Value,Duration=seek.Maximum,Volume=volume.Value,TrayIcon=tray?.IconFile,VideoId=current?.Id,PreviousEnabled=backButton.IsEnabled,NextEnabled=forwardButton.IsEnabled,RecommendedCount=prefs.Recommended.Count,QueueCount=prefs.Queue.Count,HistoryIndex=prefs.HistoryIndex}));}catch{}}};tick.Start();
  if(args.Contains("--startup")){base.Hide();NativePosition.Cloak(this,false);}else Reveal(this,true);
  await InitPlayer();if(args.Contains("--audible-play-test"))await VerifyAudiblePlayback();if(args.Contains("--animation-test"))await VerifyAnimations();if(args.Contains("--layout-test"))await VerifyControlLayout();if(args.Contains("--suite-test"))await SuiteVerification.Run(this);if(args.Contains("--recovery-test"))await VerifyPlayerRecovery();
 }
 internal static TextBlock Text(string value,double size=14,bool bold=false,string brush="Ink",Thickness? margin=null) {
  var t=new TextBlock{Text=value,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center,Margin=margin??new()};t.SetResourceReference(TextBlock.ForegroundProperty,brush);return t;
 }
 void EnsureTray(){if(source==null||exiting)return;long now=Environment.TickCount64;if(lastTrayAttempt!=long.MinValue&&now-lastTrayAttempt<5000)return;lastTrayAttempt=now;if(tray?.TryBounds(out _)==true)return;tray?.Dispose();tray=null;try{tray=new NativeTray(source,Path.Combine(AppContext.BaseDirectory,"Assets","App.ico"),ToggleWidget,TrayMenu);}catch(Exception error){if(!trayErrorLogged){App.Log(error);trayErrorLogged=true;}}}
 internal static Button IconButton(string glyph,string name,Action action) {
  var b=new Button{ToolTip=name};SuiteChrome.Apply(b,glyph);b.SetResourceReference(ForegroundProperty,"Ink");AutomationProperties.SetName(b,name);b.Click+=(_,_)=>action();return b;
 }
 internal static Button ActionButton(string text,Action action) {
  var b=new Button{Content=text,HorizontalContentAlignment=HorizontalAlignment.Center,Margin=new(0,4,0,4)};b.SetResourceReference(BackgroundProperty,"TileBrush");AutomationProperties.SetName(b,text);b.Click+=(_,_)=>action();return b;
 }
 void Favorite(){if(current==null){SetStatus("Abra um vídeo para salvar nos favoritos.",true);return;}var saved=prefs.Favorites.FirstOrDefault(x=>x.Id==current.Id&&x.Playlist==current.Playlist);if(saved!=null)prefs.Favorites.Remove(saved);else prefs.Favorites.Add(current);Save();RefreshLibrary();UpdateActions();SetStatus(saved==null?"Salvo nos favoritos.":"Removido dos favoritos.",true);}
 void QueueCurrent(){if(current==null)return;prefs.Queue.Add(current);Save();RefreshLibrary();SetStatus("Adicionado à fila.",true);}
 async Task OpenSelected(){if(library.SelectedItem is ListBoxItem item&&item.Tag is Video v)await Load(v);}
 void RemoveSelected(){if(library.SelectedItem is not ListBoxItem item||item.Tag is not Video v){SetStatus("Selecione um vídeo na lista.",true);return;}(listKind=="Fila"?prefs.Queue:prefs.Favorites).Remove(v);Save();RefreshLibrary();UpdateActions();SetStatus("Vídeo removido da lista.",true);}
 void RefreshLibrary(){if(backButton!=null)EnableTransport();library.Items.Clear();var items=listKind=="Fila"?prefs.Queue:prefs.Favorites;foreach(var v in items){var title=prefs.Titles.GetValueOrDefault(v.Id,v.Label);library.Items.Add(new ListBoxItem{Content=Text(title,12),Tag=v,ToolTip=title,Padding=new(4,6,4,6)});}if(items.Count==0)library.Items.Add(new ListBoxItem{Content=Text(listKind=="Fila"?"Sua fila está vazia.":"Salve vídeos para encontrar depois.",12,false,"SecondaryInk"),IsEnabled=false});}
 async Task OpenInput(){try{var v=Video.Parse(input.Text);if(prefs.Last is Video last&&last.Url==v.Url)v=last;await Load(v);}catch(Exception e){SetStatus(e.Message,true);}}
 async Task Next(){if(prefs.Queue.Count==0){SetStatus("Fila vazia. Adicione links nas configurações.",true);return;}var next=prefs.Queue[0];prefs.Queue.RemoveAt(0);Save();RefreshLibrary();await Load(next,true);}
 internal void Touch()=>interaction=Environment.TickCount64;
 internal void CheckIdle(){if(!App.Testing&&tray!=null&&!handoffActive&&!trayRefreshActive&&!prefs.Pinned&&IsVisible&&!PanelVisible&&!animating&&!SuiteActivation.MenuOpen&&!IsMouseCaptureWithin&&Environment.TickCount64-interaction>=20000)HideWidget();}
 void SetStatus(string value,bool feedback=false){feedbackTimeout.Stop();status.Text=value;status.ToolTip=value;videoTitle.ToolTip=videoTitle.Text+"\n"+contextLabel.Text+"\n"+value;bool error=value.Contains("Não foi possível")||value.Contains("indisponível")||value.Contains("não permite")||value.Contains("Sem conexão")||value.Contains("recusou")||value.Contains("Instale o");var show=error||feedback?Visibility.Visible:Visibility.Collapsed;status.Visibility=show;videoTitle.Visibility=show==Visibility.Visible?Visibility.Collapsed:Visibility.Visible;if(feedback)feedbackTimeout.Start();}
 internal void Save(){try{prefs.Save();}catch(Exception e){SetStatus("Não foi possível salvar as preferências.");App.Log(e);}}
 internal void Position(){if(!IsLoaded)return;double width=prefs.ExpandedPlayer?640:360;volume.Width=prefs.ExpandedPlayer?64:40;if(transport.ColumnDefinitions.Count==3)transport.ColumnDefinitions[0].Width=transport.ColumnDefinitions[2].Width=new GridLength(prefs.ExpandedPlayer?136:112);double videoHeight=Math.Max(200,(width-2)*9/16);double chrome=120+(textScale-1)*110;NativePosition.BottomRight(this,width,videoHeight+chrome);playerFrame.Height=Math.Max(200,(Width-2)*9/16);if(PanelVisible)SuitePlacement.Above(this,settingsWindow!);}
 internal void ShowWidget(){if(exiting)return;if(IsVisible&&!animating){Position();Activate();Touch();return;}animating=false;if(!App.Testing)SuiteActivation.Claim();base.Show();WindowState=WindowState.Normal;Position();ApplyAppearance();Activate();Touch();web.Suspend(false);Reveal(this,true,()=>web.SetRevealHeight(null));}
 internal void OpenFromTray(){ShowWidget();_ = RefreshOnTrayOpen();}
 void ToggleWidget(){if(IsVisible)HideWidget();else OpenFromTray();}
 internal void HideWidget(){if(animating||!IsVisible)return;PausePlayer();CloseSettings(false);Save();if(tray==null){base.Hide();return;}animating=true;Reveal(this,false,()=>{base.Hide();web.Suspend(true);animating=false;});}
 void Reveal(Window window,bool opening,Action? completed=null){double start=opening?0:1;if(window==this){if(reveal!=null)start=reveal.Progress;reveal?.Dispose();reveal=null;}else{panelReveal?.Dispose();panelReveal=null;}if(!Animate){if(window==this)web.SetRevealHeight(null);completed?.Invoke();return;}var animation=new WindowRevealAnimation(window,opening,()=>completed?.Invoke(),window==this?h=>web.SetRevealHeight(h):null,start);if(window==this)reveal=animation;else panelReveal=animation;}
 internal void ExitApp(){if(exiting)return;exiting=true;CloseSettings(false);Save();_ = Task.Delay(3000).ContinueWith(_=>Environment.Exit(0));Close();Application.Current.Shutdown();}
 void TrayMenu(){Touch();SuiteActivation.MenuOpen=true;var menu=new ContextMenu{Placement=PlacementMode.MousePoint};menu.Closed+=(_,_)=>{SuiteActivation.MenuOpen=false;Touch();};void Add(string title,Action action){var item=new MenuItem{Header=title};item.Click+=(_,_)=>action();menu.Items.Add(item);}Add(IsVisible?"Recolher widget":"Abrir widget",ToggleWidget);Add("Configurações",()=>{ShowWidget();ShowSettings();});menu.Items.Add(new Separator());Add("Sair do YouTube Widget",ExitApp);menu.IsOpen=true;}
 static void External(string value){if(Uri.TryCreate(value,UriKind.Absolute,out var u)&&u.Scheme=="https"&&new[]{"www.youtube.com","youtube.com","youtu.be","support.google.com"}.Contains(u.Host))Process.Start(new ProcessStartInfo(value){UseShellExecute=true});}
 void DisplayChanged(object? sender,EventArgs e)=>Dispatcher.BeginInvoke(Position);
 void ThemeChanged(object sender,UserPreferenceChangedEventArgs e)=>Dispatcher.BeginInvoke(()=>{appearanceSignature="";ApplyAppearance();});
}






