using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Web.Script.Serialization;
using System.Diagnostics;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
namespace BioShell {
static class Desktop {
 [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool SetDllDirectory(string path);
 public static string DataRoot;public static bool QA;
 [STAThread]static int Main(string[] args){
  if(args.Contains("--self-test"))return Tests.Run();
  QA=args.Contains("--qa");DataRoot=QA?Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qa-data",Path.GetFileNameWithoutExtension(Application.ExecutablePath)):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BioShellLab");
  try{
   Directory.CreateDirectory(DataRoot);string runtime=Path.Combine(DataRoot,"runtime-1.0.4314.35-x64");Directory.CreateDirectory(runtime);
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("WebView2Loader.dll")){string target=Path.Combine(runtime,"WebView2Loader.dll");if(!File.Exists(target))using(var dest=File.Create(target))stream.CopyTo(dest);}
   SetDllDirectory(runtime);
   AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{string name=new AssemblyName(e.Name).Name+".dll";using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){if(stream==null)return null;using(var mem=new MemoryStream()){stream.CopyTo(mem);return Assembly.Load(mem.ToArray());}}};
   return RunUI();
  }catch(Exception ex){try{File.WriteAllText(Path.Combine(DataRoot,"startup-error.txt"),ex.ToString());}catch{}MessageBox.Show("程序启动失败。请确认本机已安装 Microsoft Edge WebView2 Runtime 和 .NET Framework 4.7.2 或更高版本。\n\n"+ex.Message,"BioShell Lab");return 1;}
 }
 [MethodImpl(MethodImplOptions.NoInlining)]static int RunUI(){SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Studio());return 0;}
}
public class Studio:Form {
 WebView2 web;Shell shell;List<Lesson> lessons=Lesson.All();HashSet<int> done=new HashSet<int>();int index;bool free;string warning="";JavaScriptSerializer json=new JavaScriptSerializer();
 string ProgressPath{get{return Path.Combine(Desktop.DataRoot,"progress-v1.txt");}}
 void Log(string text){if(Desktop.QA)File.AppendAllText(Path.Combine(Desktop.DataRoot,"diagnostics.txt"),DateTime.Now.ToString("HH:mm:ss")+" "+text+Environment.NewLine);}
 public Studio(){Icon=new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("bioshell.ico"));Text="BioShell Lab · 生物科研 Linux 学习工作台";AutoScaleMode=AutoScaleMode.None;BackColor=Color.FromArgb(246,248,247);using(var g=CreateGraphics()){float scale=g.DpiX/96f;Size area=Screen.FromControl(this).WorkingArea.Size;Size=new Size(Math.Min((int)(1440*scale),area.Width),Math.Min((int)(960*scale),area.Height));MinimumSize=new Size(Math.Min((int)(1050*scale),area.Width),Math.Min((int)(720*scale),area.Height));}StartPosition=FormStartPosition.CenterScreen;
  try{if(File.Exists(ProgressPath))foreach(string v in File.ReadAllLines(ProgressPath)){int n;if(Int32.TryParse(v,out n)&&n>=0&&n<lessons.Count)done.Add(n);}}catch{warning="进度记录读取失败，本次仍可练习。";}index=FirstIncomplete();shell=new Shell(lessons[index].Start);
  web=new WebView2{Dock=DockStyle.Fill,DefaultBackgroundColor=BackColor,CreationProperties=new CoreWebView2CreationProperties{UserDataFolder=Path.Combine(Desktop.DataRoot,"browser")}};Controls.Add(web);Shown+=async(s,e)=>{try{Log("initializing");await web.EnsureCoreWebView2Async(null);Log("initialized");web.CoreWebView2.Settings.AreDefaultContextMenusEnabled=false;web.CoreWebView2.Settings.AreDevToolsEnabled=Desktop.QA;web.CoreWebView2.Settings.IsStatusBarEnabled=false;web.CoreWebView2.Settings.IsZoomControlEnabled=false;web.CoreWebView2.Settings.AreHostObjectsAllowed=false;
   web.CoreWebView2.NewWindowRequested+=(a,b)=>{b.Handled=true;};web.CoreWebView2.NavigationStarting+=(a,b)=>{Log("navigation "+b.Uri.Substring(0,Math.Min(160,b.Uri.Length)));if(b.Uri!="https://bioshell.local/index.html")b.Cancel=true;};web.CoreWebView2.NavigationCompleted+=async(a,b)=>{Log("loaded "+b.IsSuccess+" "+b.WebErrorStatus);if(Desktop.QA)Log(await web.CoreWebView2.ExecuteScriptAsync("JSON.stringify({title:document.title,body:document.body.innerText.slice(0,160),width:innerWidth,height:innerHeight})"));};web.CoreWebView2.WebMessageReceived+=Message;
   using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("studio.html"))){string html=reader.ReadToEnd();string ui=Path.Combine(Desktop.DataRoot,"ui-v02");Directory.CreateDirectory(ui);File.WriteAllText(Path.Combine(ui,"index.html"),html,System.Text.Encoding.UTF8);web.CoreWebView2.SetVirtualHostNameToFolderMapping("bioshell.local",ui,CoreWebView2HostResourceAccessKind.DenyCors);web.CoreWebView2.Navigate("https://bioshell.local/index.html");}
  }catch(Exception ex){Log(ex.ToString());MessageBox.Show("学习界面加载失败："+ex.Message+"\n请检查 WebView2 Runtime 是否可用。","BioShell Lab");Close();}};
 }
 int FirstIncomplete(){for(int i=0;i<lessons.Count;i++)if(!done.Contains(i))return i;return 0;}
 bool Unlocked(int n){return n==0||done.Contains(n)||done.Contains(n-1);}
 object Snapshot(){return new{index=index,free=free,done=done.OrderBy(n=>n).ToArray(),cwd=shell.Cwd,warning=warning,lessons=lessons.Select(l=>new{module=l.Module,title=l.Title,concept=l.Concept,example=l.Example,task=l.Task,hint=l.Hint,why=l.Why}).ToArray(),files=shell.Files.Keys.OrderBy(p=>p).Select(p=>new{path=p,dir=shell.IsDir(p)}).ToArray()};}
 void Send(object data){web.CoreWebView2.PostWebMessageAsJson(json.Serialize(data));}
 void Reset(){shell=new Shell(free?Shell.Home:lessons[index].Start);Send(new{type="state",state=Snapshot(),reset=true});}
 void Save(){try{string tmp=ProgressPath+".tmp";File.WriteAllLines(tmp,done.OrderBy(n=>n).Select(n=>n.ToString()).ToArray());if(File.Exists(ProgressPath))File.Replace(tmp,ProgressPath,null);else File.Move(tmp,ProgressPath);warning="";}catch{warning="进度保存失败，关闭后可能无法恢复。";}}
 void Message(object sender,CoreWebView2WebMessageReceivedEventArgs e){
  Log("message "+e.Source);
  if(e.Source!="https://bioshell.local/index.html")return;
  try{var m=json.Deserialize<Dictionary<string,object>>(e.WebMessageAsJson);string action=Convert.ToString(m["action"]);
   if(action=="ready")Send(new{type="state",state=Snapshot(),reset=true});
   else if(action=="select"){int n=Convert.ToInt32(m["index"]);if(n>=0&&n<lessons.Count&&Unlocked(n)){index=n;free=false;Reset();}else Send(new{type="notice",text="先完成上一关，就能解锁这一关。"});}
   else if(action=="free"){free=true;Reset();}
   else if(action=="resume"){free=false;index=FirstIncomplete();Reset();}
   else if(action=="reset")Reset();
   else if(action=="complete"){string line=Convert.ToString(m["line"]);Send(new{type="completion",line=shell.Complete(line)});}
   else if(action=="run"){
    string line=Convert.ToString(m["line"]);if(line.Length>4000){Send(new{type="notice",text="命令过长，请分步输入。"});return;}
    string output=shell.Execute(line);bool passed=!free&&shell.Success&&lessons[index].Check(shell);bool first=passed&&done.Add(index);if(first)Save();
    Send(new{type="result",line=line,output=output,success=shell.Success,passed=passed,first=first,clear=line.Trim()=="clear"&&shell.Success,state=Snapshot()});
   }
   else if(action=="open"){
    string url=Convert.ToString(m["url"]);string[] allowed={"https://datacarpentry.github.io/shell-genomics/","https://swcarpentry.github.io/shell-novice/","https://www.gnu.org/software/coreutils/manual/html_node/"};if(allowed.Contains(url))Process.Start(new ProcessStartInfo(url){UseShellExecute=true});
   }
  }catch(Exception ex){Send(new{type="notice",text="操作未完成，请重试。"+(Desktop.QA?ex.Message:"")});}
 }
}
}
