using System;
using Microsoft.Web.WebView2.Core;
namespace YouTubeWidget;
internal static class PlaybackPermissions {
 internal static CoreWebView2PermissionState Decide(CoreWebView2PermissionKind kind,string origin){
  if(kind!=CoreWebView2PermissionKind.Autoplay||!Uri.TryCreate(origin,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Port!=443||uri.UserInfo.Length>0)return CoreWebView2PermissionState.Deny;
  return uri.IdnHost.ToLowerInvariant() is "com.bruno.youtube-widget" or "www.youtube.com" or "www.youtube-nocookie.com"?CoreWebView2PermissionState.Allow:CoreWebView2PermissionState.Deny;
 }
}
