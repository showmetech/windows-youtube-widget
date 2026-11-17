using YouTubeWidget;using System;using System.IO;using System.Linq;using System.Text;using System.Text.Json;using System.Windows;using System.Windows.Threading;using System.Runtime.InteropServices;
internal static class SuiteActivation {
 public static bool MouseListenerActive=>mouseHook!=IntPtr.Zero;
 public static bool MenuOpen;static Window? window;static Action? hide;static Func<bool> pinned=()=>false;static Func<bool> clickPinned=()=>false;static IntPtr mouseHook;static readonly MouseCallback mouseCallback=MouseEvent;static DateTime shown;static bool dismissing;static DispatcherTimer? timer;static string last="",cause="Startup";static readonly DateTimeOffset boot=DateTimeOffset.UtcNow;
 static string Owner=>System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name??"Widget";
 static string FilePath=>Path.Combine(Environment.GetEnvironmentVariable("WINDOWS_WIDGET_TEST_DATA")??Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowsWidgetSuite","activation.json");
 static bool Testing=>App.Testing;
 public static void Attach(Window target,Action hideAction,Func<bool> isPinned,Func<bool>? isClickPinned=null){if(Testing)return;window=target;hide=hideAction;pinned=isPinned;clickPinned=isClickPinned??isPinned;mouseHook=SetWindowsHookEx(14,mouseCallback,GetModuleHandle(null),0);if(mouseHook==IntPtr.Zero)App.Log(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),"Outside-click listener unavailable."));target.Activated+=(_,_)=>Claim();target.IsVisibleChanged+=(_,_)=>{if(target.IsVisible){shown=DateTime.UtcNow;dismissing=false;}Save();};target.Loaded+=(_,_)=>Save();target.Deactivated+=async(_,_)=>{await System.Threading.Tasks.Task.Delay(150);CheckFocus();};timer=new(){Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>{Poll();CheckFocus();};timer.Start();target.Closed+=(_,_)=>{timer?.Stop();if(mouseHook!=IntPtr.Zero){UnhookWindowsHookEx(mouseHook);mouseHook=IntPtr.Zero;}};}
 internal static bool ShouldDismiss(bool fixedOpen,bool menu,bool ownWindow)=>!fixedOpen&&!menu&&!ownWindow;
 internal static bool VerifyFocusPolicy()=>ShouldDismiss(false,false,false)&&!ShouldDismiss(true,false,false)&&!ShouldDismiss(false,true,false)&&!ShouldDismiss(false,false,true)&&!ShouldDismiss(true,true,true);
 internal static bool ShouldDismissClick(bool fixedOpen,bool menu,bool inside,bool previousDown,bool down)=>!fixedOpen&&!menu&&!inside&&!previousDown&&down;
 internal static bool VerifyClickPolicy()=>ShouldDismissClick(false,false,false,false,true)&&!ShouldDismissClick(true,false,false,false,true)&&!ShouldDismissClick(false,false,true,false,true)&&!ShouldDismissClick(false,true,false,false,true)&&!ShouldDismissClick(false,false,false,true,true)&&!ShouldDismissClick(false,false,false,false,false);
 delegate IntPtr MouseCallback(int code,IntPtr message,IntPtr data);
 static IntPtr MouseEvent(int code,IntPtr message,IntPtr data){
  if(code>=0&&message.ToInt64() is 0x201 or 0x204 or 0x207 or 0x20b&&window?.IsVisible==true&&!dismissing&&!MenuOpen&&!clickPinned()){
   var point=Marshal.PtrToStructure<Point>(data);window.Dispatcher.BeginInvoke(()=>ProcessPointer(point));
  }
  return CallNextHookEx(mouseHook,code,message,data);
 }
 static void ProcessPointer(Point point){
  if(dismissing||window?.IsVisible!=true)return;bool inside=false;
  foreach(Window owned in Application.Current.Windows){if(!owned.IsVisible)continue;var handle=new System.Windows.Interop.WindowInteropHelper(owned).Handle;if(GetWindowRect(handle,out var rect)&&point.X>=rect.Left&&point.X<rect.Right&&point.Y>=rect.Top&&point.Y<rect.Bottom){inside=true;break;}}
  if(ShouldDismissClick(clickPinned(),MenuOpen,inside,false,true)){cause="OutsideClick";hide?.Invoke();dismissing=true;}
 }
 [StructLayout(LayoutKind.Sequential)]struct WindowRect{public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out WindowRect rect);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SetWindowsHookEx(int kind,MouseCallback callback,IntPtr module,uint thread);
 [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string? name);
 static void CheckFocus(){if(dismissing||window?.IsVisible!=true||App.Testing||DateTime.UtcNow-shown<TimeSpan.FromMilliseconds(350))return;var foreground=GetForegroundWindow();if(foreground==IntPtr.Zero)return;ProcessFocus(foreground);}
 static void ProcessFocus(IntPtr foreground){if(!dismissing&&ShouldDismiss(pinned(),MenuOpen,OwnWindow(foreground))){cause="OutsideFocus";hide?.Invoke();dismissing=true;}}
 internal static async System.Threading.Tasks.Task<bool> VerifyNativeFocus(Window target,Action show){var saved=pinned;bool menu=MenuOpen;try{pinned=()=>false;MenuOpen=false;show();await System.Threading.Tasks.Task.Delay(500);ProcessFocus(new System.Windows.Interop.WindowInteropHelper(target).Handle);bool own=target.IsVisible;MenuOpen=true;ProcessFocus(GetShellWindow());bool context=target.IsVisible;MenuOpen=false;pinned=()=>true;ProcessFocus(GetShellWindow());bool fixedOpen=target.IsVisible;pinned=()=>false;ProcessFocus(GetShellWindow());await System.Threading.Tasks.Task.Delay(700);bool hidden=!target.IsVisible;show();await System.Threading.Tasks.Task.Delay(500);return own&&context&&fixedOpen&&hidden&&target.IsVisible;}finally{pinned=saved;MenuOpen=menu;}}
 static bool OwnWindow(IntPtr hwnd){GetWindowThreadProcessId(hwnd,out var pid);if(pid==Environment.ProcessId)return true;try{using var process=System.Diagnostics.Process.GetProcessById((int)pid);var expected=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"Panel","BrunoAudio.exe"));return Owner=="WindowsAudioWidget"&&string.Equals(process.MainModule?.FileName,expected,StringComparison.OrdinalIgnoreCase);}catch{return false;}}
 public static void Claim(){if(Testing)return;dismissing=false;try{cause="Activated";var token=Guid.NewGuid().ToString("N");Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);var file=FilePath+"."+token+".tmp";File.WriteAllText(file,JsonSerializer.Serialize(new Signal(Owner,token,DateTimeOffset.UtcNow)));File.Move(file,FilePath,true);last=token;Save();}catch(IOException){}catch(UnauthorizedAccessException){}}
 static void Poll(){try{var signal=JsonSerializer.Deserialize<Signal>(File.ReadAllText(FilePath));if(signal==null||signal.Ticket==last||signal.At<boot)return;last=signal.Ticket;if(signal.Owner!=Owner&&window?.IsVisible==true&&!pinned()&&!App.Testing){cause="AnotherSuiteWidget:"+signal.Owner;hide?.Invoke();}}catch(IOException){}catch(JsonException){}catch(UnauthorizedAccessException){}}
 static void Save(){try{if(window==null)return;var border=window.Content as System.Windows.Controls.Border;var brush=border?.Background as System.Windows.Media.SolidColorBrush;File.WriteAllText(Path.Combine(AppContext.BaseDirectory,".widget-activation-state.json"),JsonSerializer.Serialize(new{At=DateTimeOffset.Now,Owner,Visible=window.IsVisible,window.IsActive,window.AllowsTransparency,BackgroundAlpha=brush?.Color.A,Cause=cause,ProcessId=Environment.ProcessId}));}catch{}}
 static bool OverTray(){GetCursorPos(out var point);var hwnd=GetAncestor(WindowFromPoint(point),2);return TrayClass(hwnd)||TrayClass(GetForegroundWindow());}
 static bool TrayClass(IntPtr hwnd){var name=new StringBuilder(128);GetClassName(hwnd,name,name.Capacity);return name.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "NotifyIconOverflowWindow";}
 [DllImport("user32.dll")]static extern IntPtr GetShellWindow();
 record Signal(string Owner,string Ticket,DateTimeOffset At);
 [StructLayout(LayoutKind.Sequential)]struct Point{public int X,Y;}
 [DllImport("user32.dll")]static extern bool GetCursorPos(out Point point);
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point point);
 [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr hwnd,uint flags);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder text,int size);
}


