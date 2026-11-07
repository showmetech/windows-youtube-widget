using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
namespace YouTubeWidget;
// Prefer the composed control. On unsupported desktops, isolate the HWND player
// in an owned opaque viewport; the widget's text and background stay layered.
internal sealed class VideoViewport:ContentControl,IDisposable {
 WebView2CompositionControl? composed=new();WebView2? classic;Window? viewport;Window? owner;
 public string Mode{get;private set;}="Composition";
 public CoreWebView2 CoreWebView2=>composed?.CoreWebView2??classic!.CoreWebView2;
 public VideoViewport(){Content=composed;LayoutUpdated+=(_,_)=>Position();IsVisibleChanged+=(_,_)=>Position();}
 public async Task EnsureCoreWebView2Async(CoreWebView2Environment env) {
  try{await composed!.EnsureCoreWebView2Async(env);return;}
  catch(COMException error) when(error.HResult==unchecked((int)0x8000FFFF)||error.HResult==unchecked((int)0x80004001)) {
   App.Log(error);composed!.Dispose();composed=null;Content=null;Mode="OwnedOpaqueViewport";
  }
  owner=Window.GetWindow(this);classic=new WebView2{DefaultBackgroundColor=System.Drawing.Color.Black};
  viewport=new Window{Title="YouTube · Vídeo",Owner=owner,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,ShowActivated=false,Background=Brushes.Black,Content=classic,Width=Math.Max(1,ActualWidth),Height=Math.Max(1,ActualHeight)};
  viewport.Show();Position();await classic.EnsureCoreWebView2Async(env);
  if(owner!=null){owner.LocationChanged+=OwnerChanged;owner.SizeChanged+=OwnerChanged;owner.IsVisibleChanged+=OwnerVisibilityChanged;owner.StateChanged+=OwnerChanged;}
 }
 void OwnerChanged(object? sender,EventArgs e)=>Position();
 void OwnerVisibilityChanged(object sender,DependencyPropertyChangedEventArgs e)=>Position();
 int? revealHeight;
 public void SetRevealHeight(int? height){revealHeight=height;lastX=int.MinValue;Position();}
 public void Suspend(bool value){suspended=value;Position();}bool suspended;int lastX=int.MinValue,lastY,lastW,lastH;
 void Position(){if(viewport==null||owner==null)return;bool visible=IsVisible&&owner.IsVisible&&owner.WindowState!=WindowState.Minimized&&!suspended;if(!visible){viewport.Hide();return;}if(!viewport.IsVisible)viewport.Show();var point=PointToScreen(new Point());var dpi=VisualTreeHelper.GetDpi(this);int width=Math.Max(1,(int)Math.Round(ActualWidth*dpi.DpiScaleX)),height=Math.Max(1,(int)Math.Round(ActualHeight*dpi.DpiScaleY));var handle=new WindowInteropHelper(viewport).Handle;if(lastX==(int)Math.Round(point.X)&&lastY==(int)Math.Round(point.Y)&&lastW==width&&lastH==height){viewport.Topmost=owner.Topmost;return;}lastX=(int)Math.Round(point.X);lastY=(int)Math.Round(point.Y);lastW=width;lastH=height;SetWindowPos(handle,IntPtr.Zero,(int)Math.Round(point.X),(int)Math.Round(point.Y),width,height,0x14);int clipTop=0;if(revealHeight is int shown){GetWindowRect(new WindowInteropHelper(owner).Handle,out var ownerRect);clipTop=Math.Clamp(ownerRect.Bottom-shown-lastY,0,height);}
 var region=CreateRoundRectRgn(0,clipTop,width+1,height+1,16,16);if(SetWindowRgn(handle,region,true)==0)DeleteObject(region);viewport.Topmost=owner.Topmost;}
 public void Reset(){Dispose();owner=null;viewport=null;classic=null;composed=new();Content=composed;Mode="Composition";suspended=false;revealHeight=null;lastX=int.MinValue;}
 public void Dispose(){composed?.Dispose();classic?.Dispose();if(owner!=null){owner.LocationChanged-=OwnerChanged;owner.SizeChanged-=OwnerChanged;owner.IsVisibleChanged-=OwnerVisibilityChanged;owner.StateChanged-=OwnerChanged;}viewport?.Close();}
 [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("user32.dll")]static extern int SetWindowRgn(IntPtr hwnd,IntPtr region,bool redraw);
 [DllImport("gdi32.dll")]static extern IntPtr CreateRoundRectRgn(int a,int b,int c,int d,int e,int f);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
}


