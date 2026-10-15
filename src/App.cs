using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace FarTrainer {
 public static class App {
  [STAThread] public static int Main(string[] args){
   try{
    if(args.Contains("--self-test"))return Validation.Run(false);
    if(args.Contains("--diagnose"))return Validation.Run(args.Contains("--roundtrip"));
    if(args.Contains("--ballistics-check"))return LiveBallisticsCheck.Run();
    if(args.Contains("--weapon-check"))return LiveWeaponCheck.Run();
    if(args.Contains("--inventory-check"))return LiveInventoryCheck.Run();
    var application=new Application();application.ShutdownMode=ShutdownMode.OnMainWindowClose;
    var window=new MainWindow();
    if(args.Contains("--ui-test"))return window.TestUi();
    if(args.Contains("--preview-live"))window.PrepareLivePreview(args.Contains("--inventory"));
    if(args.Contains("--configs"))window.SelectCategory("Configs");
    if(args.Contains("--settings"))window.SelectCategory("Game settings");
    if(args.Contains("--inventory"))window.SelectCategory("Currency and inventory");
    if(args.Contains("--preview")){
     var visual=(FrameworkElement)window.Content;visual.Width=1320;visual.Height=900;visual.Measure(new Size(1320,900));visual.Arrange(new Rect(0,0,1320,900));visual.UpdateLayout();
     var bmp=new RenderTargetBitmap(1320,900,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png")))png.Save(f);window.FinishPreview();return 0;
    }
    application.Run(window);return 0;
   }catch(Exception e){Directory.CreateDirectory(Data.UserRoot);File.WriteAllText(Path.Combine(Data.UserRoot,"startup-error.txt"),e.ToString());if(!args.Any(a=>a.StartsWith("--")))MessageBox.Show(e.Message,"Hope Control");return 1;}
  }
 }
}

