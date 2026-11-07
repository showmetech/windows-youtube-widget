using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
namespace YouTubeWidget;
public class App:Application {
 public static bool Testing{get;private set;}
 [STAThread] public static void Main(string[] args){
  if(args.Contains("--register-native-host")){StoreIntegration.RegisterNativeHost();return;}
  if(args.Length>=2&&args[0]=="--supervisor-fixture"){RuntimeSupervisor.Fixture(args[1],args.Contains("--graceful"));return;}
  if(args.Length>0&&args[0]=="--watchdog"){RuntimeSupervisor.Watch(args);return;}
  if(args.Contains("--bridge-test")){int testRoot=Array.IndexOf(args,"--data-root");Preferences.TestFolder=testRoot>=0&&testRoot+1<args.Length?Path.GetFullPath(args[testRoot+1]):Path.Combine(AppContext.BaseDirectory,"TestProfile");Recommendations.NativeHost(true);return;}
  if(args.Length>0&&(args[0]=="chrome-extension://odeinjoocooeglaocjionmegdgabmojb/"||args[0]=="chrome-extension://iophndcddlmmaaanhaiiioaanpgeigak/")){Recommendations.NativeHost();return;}
  if(args.Contains("--test")){Tests.Run();return;}
  int dataRoot=Array.IndexOf(args,"--data-root");if(dataRoot>=0&&dataRoot+1<args.Length)Preferences.TestFolder=Path.GetFullPath(args[dataRoot+1]);
  Testing=args.Contains("--suite-test")||args.Contains("--probe")||args.Contains("--handoff-receiver-test");
  if(Testing&&dataRoot<0)Preferences.TestFolder=Path.Combine(AppContext.BaseDirectory,"SuiteTestProfile");
  if(args.Contains("--exit")){Signal("Exit");return;}
  if(args.Contains("--refresh-recommendations")){Signal("Refresh");return;}
  string prefix=Testing?"Local\\WindowsYouTubeWidget.Test."+Environment.ProcessId:"Local\\WindowsYouTubeWidget.Production";
  using var mutex=new Mutex(true,prefix+".Mutex",out bool first);
  if(!first&&!Testing){if(!args.Contains("--startup"))Signal("Show");return;}
  try{if(!Testing){try{StoreIntegration.RegisterNativeHost();}catch(Exception error){StoreIntegration.LastError=error.Message;Log(error);}}var app=new App();app.Resources=new ResourceDictionary{Source=new Uri("/WindowsYouTubeWidget;component/AppResources.xaml",UriKind.Relative)};
   app.DispatcherUnhandledException+=(_,e)=>{Log(e.Exception);e.Handled=true;};var window=new MainWindow(args);
   using var show=new EventWaitHandle(false,EventResetMode.AutoReset,prefix+".Show");using var exit=new EventWaitHandle(false,EventResetMode.AutoReset,prefix+".Exit");using var refresh=new EventWaitHandle(false,EventResetMode.AutoReset,prefix+".Refresh");
   var showWait=ThreadPool.RegisterWaitForSingleObject(show,(_,_)=>app.Dispatcher.BeginInvoke(window.ShowWidget),null,-1,false);var exitWait=ThreadPool.RegisterWaitForSingleObject(exit,(_,_)=>app.Dispatcher.BeginInvoke(window.ExitApp),null,-1,false);
   var refreshWait=ThreadPool.RegisterWaitForSingleObject(refresh,(_,_)=>app.Dispatcher.BeginInvoke(window.OpenFromTray),null,-1,false);
   using var recommendationBroker=Testing?null:new RecommendationBroker();
   using var supervisor=Testing?null:RuntimeSupervisor.Start();
   app.SessionEnding+=(_,_)=>supervisor?.Dispose();app.Exit+=(_,_)=>supervisor?.Dispose();
   try{app.Run(window);}finally{showWait.Unregister(null);exitWait.Unregister(null);refreshWait.Unregister(null);}
  }catch(Exception e){Log(e);}
 }
 static void Signal(string kind)=>TrySignal(kind);
 internal static bool TrySignal(string kind){try{using var signal=EventWaitHandle.OpenExisting("Local\\WindowsYouTubeWidget.Production."+kind);signal.Set();return true;}catch(WaitHandleCannotBeOpenedException){return false;}}
 public static void Log(Exception e){try{Directory.CreateDirectory(Preferences.Folder);File.AppendAllText(Path.Combine(Preferences.Folder,"errors.log"),DateTimeOffset.Now+" "+e+Environment.NewLine);}catch{}}
}
