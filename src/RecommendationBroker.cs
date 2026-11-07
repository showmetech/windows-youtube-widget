using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace YouTubeWidget;
internal sealed class RecommendationBroker:IDisposable {
 const string Name="Bruno.WindowsYouTubeWidget.Recommendations.v1";
 readonly CancellationTokenSource stop=new();
 public RecommendationBroker(){_ = Task.Run(Listen);}
 async Task Listen(){
  while(!stop.IsCancellationRequested){
   try{
    var pipe=new NamedPipeServerStream(Name,PipeDirection.InOut,8,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
    await pipe.WaitForConnectionAsync(stop.Token);
    _ = Task.Run(()=>Serve(pipe));
   }catch(OperationCanceledException){return;}catch(IOException){if(!stop.IsCancellationRequested)await Task.Delay(200);}catch(Exception error){App.Log(error);if(!stop.IsCancellationRequested)await Task.Delay(1000);}
  }
 }
 async Task Serve(NamedPipeServerStream pipe){
  using(pipe){try{
    using var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true);
    using var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true){AutoFlush=true};
    string? message=await reader.ReadLineAsync(stop.Token);string response="{\"ok\":false}";
    if(message!=null&&message.Length<=65536){
     try{using var d=JsonDocument.Parse(message);var root=d.RootElement;
      if(root.GetProperty("type").GetString()=="refreshPoll"){
       string token="";try{using var refresh=JsonDocument.Parse(File.ReadAllText(Recommendations.RefreshPath));if(refresh.RootElement.GetProperty("At").GetDateTimeOffset()>DateTimeOffset.UtcNow.AddSeconds(-45))token=refresh.RootElement.GetProperty("Token").GetString()??"";}catch(IOException){}catch(JsonException){}
       response=JsonSerializer.Serialize(new{ok=true,token});
      }else if(root.GetProperty("type").GetString()=="recommendation")response=Recommendations.SaveRecommendation(root);
      else if(root.GetProperty("type").GetString()=="handoff")response=JsonSerializer.Serialize(HandoffTransport.Handle(root,false));
      if(root.GetProperty("type").GetString()!="refreshPoll")File.WriteAllText(Path.Combine(Preferences.Folder,"broker-last.json"),JsonSerializer.Serialize(new{At=DateTimeOffset.UtcNow,Operation=root.GetProperty("type").GetString(),Response=response}));
     }catch(Exception error) when(error is IOException or JsonException or FormatException or InvalidOperationException or UnauthorizedAccessException){App.Log(error);}
    }
    await writer.WriteLineAsync(response);
   }catch(OperationCanceledException){}catch(IOException){}catch(Exception error){App.Log(error);}
  }
 }
 public static string Call(string message){
  using var pipe=new NamedPipeClientStream(".",Name,PipeDirection.InOut,PipeOptions.CurrentUserOnly);pipe.Connect(3000);
  using var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true){AutoFlush=true};
  using var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true);writer.WriteLine(message);return reader.ReadLine()??"{\"ok\":false}";
 }
 public void Dispose(){stop.Cancel();}
}
