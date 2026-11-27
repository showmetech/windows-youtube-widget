using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Windows.UI.ViewManagement;
namespace YouTubeWidget;
public sealed partial class MainWindow {
 UISettings? accessibilitySettings;
 sealed class TextSize {internal readonly double Value;internal TextSize(double value){Value=value;}}
 readonly ConditionalWeakTable<DependencyObject,TextSize> baseTextSizes=new();
 double textScale=1;
 void InitializeAccessibility(){try{accessibilitySettings=new UISettings();accessibilitySettings.TextScaleFactorChanged+=TextScaleChanged;ApplyTextScale(accessibilitySettings.TextScaleFactor);}catch(System.Runtime.InteropServices.COMException e){App.Log(e);}}
 void TextScaleChanged(UISettings sender,object args)=>Dispatcher.BeginInvoke(()=>ApplyTextScale(sender.TextScaleFactor));
 internal void ApplyTextScale(double scale){textScale=Math.Clamp(scale,1,2.25);ScaleTree(this);if(settingsWindow!=null)ScaleTree(settingsWindow);if(settingsHeader!=null)settingsHeader.Height=64+20*(textScale-1);timeline.Height=22*textScale;transport.Height=48*textScale;titleFrame.Height=status.MaxHeight=22*textScale;videoTitle.Height=videoTitle.MaxHeight=48*textScale;Position();}
 void ScaleTree(DependencyObject element){var property=element is TextBlock?TextBlock.FontSizeProperty:element is Control?Control.FontSizeProperty:null;if(element is TextBlock glyph&&glyph.FontFamily.Source.Contains("Icons"))property=null;if(property!=null&&element.ReadLocalValue(property)!=DependencyProperty.UnsetValue){if(!baseTextSizes.TryGetValue(element,out var original)){original=new TextSize((double)element.GetValue(property));baseTextSizes.Add(element,original);}element.SetValue(property,original.Value*textScale);}foreach(var child in LogicalTreeHelper.GetChildren(element))if(child is DependencyObject dependency)ScaleTree(dependency);}
 void DisposeAccessibility(){if(accessibilitySettings!=null)accessibilitySettings.TextScaleFactorChanged-=TextScaleChanged;}
}

