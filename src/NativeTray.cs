using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
namespace YouTubeWidget;
// Native notification-area integration; owns only this application's icon.
internal sealed class NativeTray:IDisposable {
 const uint Callback=0x8001;
 readonly HwndSource source;
 readonly Action toggle,context;
 readonly uint taskbarCreated;
 readonly Guid id=App.Testing?new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Environment.ProcessPath??"YouTubeWidget.Test")).AsSpan(0,16)):new("7d522462-9ed3-4e1c-bb66-52dc7e562a1e");
 IntPtr icon;string iconPath;
 bool modern,disposed;
 internal string IconFile=>iconPath;
 public NativeTray(HwndSource source,string iconFile,Action toggle,Action context){
  this.source=source;this.toggle=toggle;this.context=context;iconPath=iconFile;
  taskbarCreated=RegisterWindowMessage("TaskbarCreated");
  icon=LoadImage(IntPtr.Zero,iconFile,1,GetSystemMetrics(49),GetSystemMetrics(50),0x10);
  if(icon==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"Não foi possível carregar o ícone da bandeja.");
  source.AddHook(Hook);try{Add();}catch{source.RemoveHook(Hook);DestroyIcon(icon);throw;}
 }
 Data Create()=>new(){Size=(uint)Marshal.SizeOf<Data>(),Window=source.Handle,Id=1,Flags=1|2|4|0x20|0x80,Message=Callback,Icon=icon,Tip="YouTube Widget",Guid=id};
 void Add(){var data=Create();if(!Shell_NotifyIcon(0,ref data))throw new Win32Exception(Marshal.GetLastWin32Error(),"Não foi possível adicionar o ícone à bandeja.");data.Version=4;modern=Shell_NotifyIcon(4,ref data);}
 IntPtr Hook(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled){
  if((uint)message==taskbarCreated&&!disposed){try{Add();}catch{}return IntPtr.Zero;}
  if(message!=Callback||disposed)return IntPtr.Zero;
  int action=CallbackAction(l.ToInt64(),modern);
  if(action==1){handled=true;source.Dispatcher.BeginInvoke(toggle);}
  else if(action==2){handled=true;SetForegroundWindow(source.Handle);source.Dispatcher.BeginInvoke(context);}
  return IntPtr.Zero;
 }
 internal static int CallbackAction(long value,bool version4){int code=version4?(int)(value&0xffff):unchecked((int)value);return code==0x400||code==0x401||(!version4&&code==0x202)?1:code==0x7b||(!version4&&code==0x205)?2:0;}
 public void UpdateIcon(string file){
  if(disposed||file==iconPath)return;
  var next=LoadImage(IntPtr.Zero,file,1,GetSystemMetrics(49),GetSystemMetrics(50),0x10);
  if(next==IntPtr.Zero)return;
  var previous=icon;icon=next;var data=Create();
  if(Shell_NotifyIcon(1,ref data)){iconPath=file;DestroyIcon(previous);}else{icon=previous;DestroyIcon(next);}
 }
 public bool TryBounds(out IconRect rect){var identity=new Identity{Size=(uint)Marshal.SizeOf<Identity>(),Window=source.Handle,Id=1,Guid=id};return Shell_NotifyIconGetRect(ref identity,out rect)==0;}
 public void Dispose(){if(disposed)return;disposed=true;var data=Create();Shell_NotifyIcon(2,ref data);source.RemoveHook(Hook);if(App.Testing)RemoveTestRegistration();if(icon!=IntPtr.Zero){DestroyIcon(icon);icon=IntPtr.Zero;}}
 void RemoveTestRegistration(){
  try{using var root=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings",true);if(root==null)return;foreach(var key in root.GetSubKeyNames()){bool own;using(var item=root.OpenSubKey(key)){own=string.Equals(item?.GetValue("ExecutablePath") as string,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase)&&Guid.TryParse(item?.GetValue("IconGuid") as string,out var guid)&&guid==id;}if(own)root.DeleteSubKeyTree(key,false);}}catch(Exception error){App.Log(error);}
 } [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Data{
  public uint Size;public IntPtr Window;public uint Id,Flags,Message;public IntPtr Icon;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Tip;
  public uint State,StateMask;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)]public string Info;
  public uint Version;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)]public string Title;
  public uint InfoFlags;public Guid Guid;public IntPtr BalloonIcon;
 }
 [StructLayout(LayoutKind.Sequential)]struct Identity{public uint Size;public IntPtr Window;public uint Id;public Guid Guid;}
 [StructLayout(LayoutKind.Sequential)]public struct IconRect{public int Left,Top,Right,Bottom;}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Shell_NotifyIcon(uint operation,ref Data data);
 [DllImport("shell32.dll")]static extern int Shell_NotifyIconGetRect(ref Identity identity,out IconRect rect);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadImage(IntPtr instance,string file,uint type,int cx,int cy,uint flags);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
 [DllImport("user32.dll")]static extern int GetSystemMetrics(int index);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern uint RegisterWindowMessage(string name);
}




