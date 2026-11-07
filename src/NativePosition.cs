using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
namespace YouTubeWidget;
internal static class NativePosition {
 internal static bool TaskbarExcluded(Window window)=>!window.ShowInTaskbar&&(GetWindowLongPtr(new WindowInteropHelper(window).Handle,-20).ToInt64()&0x40000)==0;
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
 internal static void BottomRight(Window window,double desiredWidth,double desiredHeight) {
  var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;
  var monitor=System.Windows.Forms.Screen.FromHandle(handle);var area=monitor.WorkingArea;var dpi=VisualTreeHelper.GetDpi(window);
  double width=area.Width/dpi.DpiScaleX,height=area.Height/dpi.DpiScaleY;var fit=SuiteWindow.Fit(width,height);
  window.Width=Math.Max(1,Math.Min(desiredWidth,width-fit.Margin*2));window.Height=Math.Min(desiredHeight,height-fit.Margin*2);window.UpdateLayout();
  SetWindowPos(handle,IntPtr.Zero,area.Right-(int)Math.Round((window.Width+fit.Margin)*dpi.DpiScaleX),area.Bottom-(int)Math.Round((window.Height+fit.Margin)*dpi.DpiScaleY),(int)Math.Round(window.Width*dpi.DpiScaleX),(int)Math.Round(window.Height*dpi.DpiScaleY),0x14);
 }
 internal static void Cloak(Window window,bool on){int flag=on?1:0;DwmSetWindowAttribute(new WindowInteropHelper(window).Handle,13,ref flag,4);}
 internal static void Decorate(Window window,bool dark){int flag=dark?1:0,corner=2;var handle=new WindowInteropHelper(window).Handle;DwmSetWindowAttribute(handle,20,ref flag,4);DwmSetWindowAttribute(handle,33,ref corner,4);}
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
}
