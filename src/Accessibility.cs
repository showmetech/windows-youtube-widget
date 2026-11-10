using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Windows.UI.ViewManagement;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 UISettings? accessibilitySettings;
 readonly Dictionary<DependencyObject,double> baseTextSizes=new();
 double textScale=1;
 void InitializeAccessibility(){try{accessibilitySettings=new UISettings();accessibilitySettings.TextScaleFactorChanged+=TextScaleChanged;ApplyTextScale(accessibilitySettings.TextScaleFactor);}catch(System.Runtime.InteropServices.COMException e){App.Log(e);}}
 void TextScaleChanged(UISettings sender,object args)=>Dispatcher.BeginInvoke(()=>ApplyTextScale(sender.TextScaleFactor));
 internal void ApplyTextScale(double scale){textScale=Math.Clamp(scale,1,2.25);ScaleTree(this);if(settingsWindow!=null)ScaleTree(settingsWindow);if(settingsSurface?.Child is DockPanel dock&&dock.Children.Count>0)((FrameworkElement)dock.Children[0]).Height=64*textScale;timeline.Height=22*textScale;transport.Height=48*textScale;titleFrame.Height=status.MaxHeight=22*textScale;videoTitle.Height=videoTitle.MaxHeight=48*textScale;Position();}
 void ScaleTree(DependencyObject element){var property=element is TextBlock?TextBlock.FontSizeProperty:element is Control?Control.FontSizeProperty:null;if(element is TextBlock glyph&&glyph.FontFamily.Source.Contains("Icons"))property=null;if(property!=null&&element.ReadLocalValue(property)!=DependencyProperty.UnsetValue){if(!baseTextSizes.TryGetValue(element,out var original)){original=(double)element.GetValue(property);baseTextSizes[element]=original;}element.SetValue(property,original*textScale);}foreach(var child in LogicalTreeHelper.GetChildren(element))if(child is DependencyObject dependency)ScaleTree(dependency);}
 void DisposeAccessibility(){if(accessibilitySettings!=null)accessibilitySettings.TextScaleFactorChanged-=TextScaleChanged;}
}

