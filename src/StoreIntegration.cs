using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.ApplicationModel;
namespace YouTubeWidget;
internal static class StoreIntegration {
 internal static string LastError="";
 internal const string StartupId="YouTubeWidgetStartup";
 internal static bool IsPackaged {get{uint length=0;return GetCurrentPackageFullName(ref length,null)==122;}}
 // The Store manifest declares narrowly scoped HKCU/AppData interoperability.
 // The same manifest is refreshed after upgrades, because WindowsApps paths change.
 internal static void RegisterNativeHost(){
  var folder=Path.Combine(Preferences.Folder,"NativeMessaging");Directory.CreateDirectory(folder);
  var file=Path.Combine(folder,"native-host.json");var temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";
  var json=JsonSerializer.Serialize(new{name=Recommendations.Host,description="YouTube Widget local",path=Environment.ProcessPath,type="stdio",allowed_origins=new[]{"chrome-extension://odeinjoocooeglaocjionmegdgabmojb/","chrome-extension://iophndcddlmmaaanhaiiioaanpgeigak/"}});
  File.WriteAllText(temporary,json,new UTF8Encoding(false));File.Move(temporary,file,true);
  foreach(var view in Environment.Is64BitOperatingSystem?new[]{RegistryView.Registry32,RegistryView.Registry64}:new[]{RegistryView.Registry32}){
   using var current=RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,view);
   foreach(var browser in new[]{"Google\\Chrome","Microsoft\\Edge"}){
    using var key=current.CreateSubKey("Software\\"+browser+"\\NativeMessagingHosts\\"+Recommendations.Host);key.SetValue("",file,RegistryValueKind.String);
   }
  }
 }
 internal static async Task<StartupTaskState> StartupState()=> (await StartupTask.GetAsync(StartupId)).State;
 internal static async Task PrepareRuntime(){
  var runtime=Path.Combine(AppContext.BaseDirectory,"WebView2Runtime");
  if(IsPackaged||Environment.OSVersion.Version.Build>=22000||!File.Exists(Path.Combine(runtime,"msedgewebview2.exe")))return;
  // Microsoft requires read/execute for the renderer AppContainers on Windows 10.
  // Grant only the bundled public runtime folder, never user data or browser profiles.
  foreach(var sid in new[]{"*S-1-15-2-2:(OI)(CI)(RX)","*S-1-15-2-1:(OI)(CI)(RX)"}){
   var info=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"icacls.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
   info.ArgumentList.Add(runtime);info.ArgumentList.Add("/grant");info.ArgumentList.Add(sid);
   using var process=Process.Start(info)??throw new IOException("Não foi possível preparar o componente de vídeo.");
   await process.WaitForExitAsync();if(process.ExitCode!=0)throw new UnauthorizedAccessException("Não foi possível preparar a pasta do runtime no Windows 10.");
  }
 }
 internal static async Task<StartupTaskState> SetStartup(bool enabled){var task=await StartupTask.GetAsync(StartupId);if(enabled)return await task.RequestEnableAsync();task.Disable();return task.State;}
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern int GetCurrentPackageFullName(ref uint length,StringBuilder? name);
}
