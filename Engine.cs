using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BioShell {
public class VFile {
 public bool Dir; public string Text;
 public VFile(bool dir, string text) { Dir=dir; Text=text; }
}
public class CommandEvent {
 public string Name, Output; public List<string> Paths=new List<string>(); public List<string> Args;
}
public class Shell {
 public const string Home="/home/researcher", Project=Home+"/project";
 public Dictionary<string,VFile> Files=new Dictionary<string,VFile>(StringComparer.Ordinal);
 public List<CommandEvent> Events=new List<CommandEvent>();
 public string Cwd=Home, Previous=Home;
 public bool Success; public string Redirected;
 public Shell(string start) {
  foreach(string p in new[]{"/","/home",Home,Project,Project+"/data",Project+"/data/raw",Project+"/results",Project+"/scripts"}) Files[p]=new VFile(true,"");
  Files[Home+"/.bashrc"]=new VFile(false,"# 教学环境预设别名\nalias ll='ls -l'\n");
  Files[Project+"/README.txt"]=new VFile(false,"RNA-seq pilot project\nOrganism: Arabidopsis thaliana\nSamples: 4 | Reads: demonstration data only\n");
  Files[Project+"/samples.tsv"]=new VFile(false,"sample\tgroup\tfile\nctrl_01\tcontrol\tctrl_01.fastq\nctrl_02\tcontrol\tctrl_02.fastq\ntreat_01\ttreatment\ttreat_01.fastq\ntreat_02\ttreatment\ttreat_02.fastq\n");
  Files[Project+"/data/raw/ctrl_01.fastq"]=new VFile(false,"@CTRL_READ_001\nACGTACGTACGT\n+\nIIIIIIIIIIII\n@CTRL_READ_002\nGCTAGCTAGCTA\n+\nIIIIIIIIIIII\n@CTRL_READ_003\nTTGCAATTGCAA\n+\nIIIIIIIIIIII\n@CTRL_READ_004\nCCGTAACCGTAA\n+\nIIIIIIIIIIII\n");
  Files[Project+"/data/raw/treat_01.fastq"]=new VFile(false,"@TREAT_READ_001\nACGTACGTACGT\n+\nIIIIIIIIIIII\n");
  Files[Project+"/results/old.txt"]=new VFile(false,"Temporary practice file\n");
  Cwd=start;
 }
 public string Resolve(string input) {
  if(input=="~") input=Home; else if(input.StartsWith("~/")) input=Home+input.Substring(1);
  if(!input.StartsWith("/")) input=Cwd+"/"+input;
  var parts=new List<string>();
  foreach(string part in input.Split('/')) { if(part==""||part==".") continue; if(part=="..") {if(parts.Count>0)parts.RemoveAt(parts.Count-1);} else parts.Add(part); }
  return "/"+String.Join("/",parts);
 }
 static string Parent(string p) {int n=p.LastIndexOf('/'); return n<=0?"/":p.Substring(0,n);}
 static string Base(string p) {return p.Substring(p.LastIndexOf('/')+1);}
 public bool IsDir(string p) {return Files.ContainsKey(p)&&Files[p].Dir;}
 public bool Has(string p) {return Files.ContainsKey(p);}
 public string Read(string p) {if(!Has(p))throw new Exception("找不到文件："+p+"。可先用 ls 查看目录。"); if(IsDir(p))throw new Exception("这是目录，查看目录请使用 ls："+p); return Files[p].Text;}
 void Write(string p,string value,bool append) {if(!IsDir(Parent(p)))throw new Exception("目标的父目录不存在，请先 mkdir："+Parent(p)); if(IsDir(p))throw new Exception("目标是目录："+p); Files[p]=new VFile(false,(append&&Has(p)?Read(p):"")+value);}
 public static List<string> Tokenize(string text) {
  var list=new List<string>(); var b=new StringBuilder(); char quote='\0'; bool active=false;
  for(int i=0;i<text.Length;i++) { char c=text[i];
   if(quote!='\0') {if(c==quote)quote='\0'; else if(c=='\\'&&quote=='"'&&i+1<text.Length&&(text[i+1]=='"'||text[i+1]=='\\'))b.Append(text[++i]);else b.Append(c); active=true;continue;}
   if(c=='\''||c=='"'){quote=c;active=true;continue;}
   if(c=='\\'&&i+1<text.Length){b.Append(text[++i]);active=true;continue;}
   if(Char.IsWhiteSpace(c)){if(active){list.Add(b.ToString());b.Clear();active=false;}continue;}
   if(c=='|'||c=='>'){if(active){list.Add(b.ToString());b.Clear();active=false;} string op=c.ToString(); if(c=='>'&&i+1<text.Length&&text[i+1]=='>'){op=">>";i++;}list.Add("\0"+op);continue;}
   if(c==';'||c=='&'||c=='<'||c=='`')throw new Exception("本教学终端暂不支持 ;、&、< 或命令替换。请逐条输入命令。");
   b.Append(c);active=true;
  }
  if(quote!='\0')throw new Exception("引号没有闭合。请补上配对的引号。"); if(active)list.Add(b.ToString());return list;
 }
 public string Execute(string line) {
  Success=false;Redirected=null; int originalCount=Events.Count;
  try {
   var all=Tokenize(line);if(all.Count==0){Success=true;return "";}
   string target=null;bool append=false;
   int redir=all.FindIndex(t=>t=="\0>"||t=="\0>>");
   if(redir>=0){if(redir!=all.Count-2||all[redir+1].StartsWith("\0"))throw new Exception("重定向格式：命令 > 文件，或 命令 >> 文件。"); target=Resolve(all[redir+1]);append=all[redir]=="\0>>";all=all.Take(redir).ToList(); if(!IsDir(Parent(target))||IsDir(target))throw new Exception("输出路径不可用："+target);}
   var chunks=new List<List<string>>();var chunk=new List<string>();
   foreach(string t in all){if(t=="\0|"){if(chunk.Count==0)throw new Exception("管道两侧都需要命令。");chunks.Add(chunk);chunk=new List<string>();}else chunk.Add(t);}
   if(chunk.Count==0)throw new Exception("请输入完整命令。");chunks.Add(chunk);
   if(chunks.Count>1&&chunks.Any(c=>!new[]{"cat","head","tail","grep","wc"}.Contains(c[0])))throw new Exception("本版本的管道支持 cat、head、tail、grep、wc。");
   string output=null; foreach(var c in chunks)output=Run(c,output);
   if(target!=null){Write(target,output,append);Redirected=target;output="";}
   Success=true;return output??"";
  } catch(Exception ex){if(Events.Count>originalCount)Events.RemoveRange(originalCount,Events.Count-originalCount);return "提示："+ex.Message+"\n";}
 }
 string Run(List<string> tokens,string stdin) {
  string cmd=tokens[0];var args=tokens.Skip(1).ToList();var e=new CommandEvent{Name=cmd,Args=args};
  var flags=args.Where(a=>a.StartsWith("-")&&a!="-").ToList();var pos=args.Where(a=>!a.StartsWith("-")||a=="-").ToList();
  string output="";
  if(cmd=="pwd") {NoArgs(args);output=Cwd+"\n";}
  else if(cmd=="help") {NoArgs(args);output="教学终端命令\npwd · ls [-lah] [目录] · ll [目录] · cd [目录]\ncat 文件 · head/tail [-n 数字] 文件 · wc -l 文件\ngrep [-in] '表达式' 文件 · find [目录] -name '模式'\nmkdir [-p] 目录 · touch 文件 · cp 源 目标 · mv 源 目标\nrm 文件 · echo 文本 · clear\n管道：cat 文件 | grep control | wc -l\n重定向：grep control samples.tsv > results/control.tsv\n引号支持空格；Tab 补全路径，上下键查看历史。\n仅模拟以上子集；不执行真实系统命令，不支持变量、通配符展开、脚本或软件安装。\n";}
  else if(cmd=="clear") {NoArgs(args);}
  else if(cmd=="cd") {
   if(args.Count>1)throw new Exception("cd 一次只接收一个目录。");string p=args.Count==0?Home:(args[0]=="-"?Previous:Resolve(args[0]));
   if(!IsDir(p))throw new Exception("目录不存在："+p+"。路径区分大小写。");Previous=Cwd;Cwd=p;e.Paths.Add(p);if(args.Count==1&&args[0]=="-")output=Cwd+"\n";
  }
  else if(cmd=="ls"||cmd=="ll") {
   CheckFlags(flags,"lah");if(pos.Count>1)throw new Exception("本版本 ls 一次查看一个路径。");string p=Resolve(pos.Count==0?".":pos[0]); if(!Has(p))throw new Exception("路径不存在："+p);e.Paths.Add(p);
   bool longForm=cmd=="ll"||flags.Any(f=>f.Contains("l")),hidden=flags.Any(f=>f.Contains("a"));
   var entries=IsDir(p)?Files.Keys.Where(k=>k!="/"&&Parent(k)==p&&(hidden||!Base(k).StartsWith("."))).OrderBy(k=>k,StringComparer.Ordinal):new[]{p}.AsEnumerable();
   foreach(string k in entries)output+=(longForm?String.Format("{0}  researcher  {1,5}  ",IsDir(k)?"drwxr-xr-x":"-rw-r--r--",IsDir(k)?0:Encoding.UTF8.GetByteCount(Files[k].Text)):"")+Base(k)+(IsDir(k)?"/":"")+"\n";
  }
  else if(cmd=="cat"||cmd=="head"||cmd=="tail"||cmd=="wc"||cmd=="grep") {
   int count=10;var operands=new List<string>();bool insensitive=false,number=false;
   if(cmd=="head"||cmd=="tail") {
    for(int i=0;i<args.Count;i++) {if(args[i]=="-n"){if(++i>=args.Count||!Int32.TryParse(args[i],out count)||count<0)throw new Exception("-n 后需要非负整数，例如 head -n 4 文件。");}else if(args[i].StartsWith("-"))throw new Exception("本版本仅支持 -n 数字。");else operands.Add(args[i]);}
   }else {CheckFlags(flags,cmd=="grep"?"in":cmd=="wc"?"l":"");operands=pos;insensitive=flags.Any(f=>f.Contains("i"));number=flags.Any(f=>f.Contains("n")); if(cmd=="wc"&&!flags.Any(f=>f.Contains("l")))throw new Exception("本版本请使用 wc -l 统计换行数。");}
   string pattern=null; if(cmd=="grep"){if(operands.Count<1)throw new Exception("用法：grep '关键词' 文件。");pattern=operands[0];operands.RemoveAt(0);if(pattern.Length>200)throw new Exception("搜索表达式过长。");}
   if(operands.Count>1)throw new Exception("本版本一次读取一个文件。");
   string data;if(operands.Count==1){string p=Resolve(operands[0]);e.Paths.Add(p);data=Read(p);}else if(stdin!=null)data=stdin;else throw new Exception("需要文件路径。可先用 ls 查看文件。");
   var lines=data.Split('\n').ToList();if(lines.Count>0&&lines[lines.Count-1]=="")lines.RemoveAt(lines.Count-1);
   if(cmd=="cat")output=data;
   if(cmd=="head")output=JoinLines(lines.Take(count));
   if(cmd=="tail")output=JoinLines(lines.Skip(Math.Max(0,lines.Count-count)));
   if(cmd=="wc")output=data.Count(c=>c=='\n').ToString()+(operands.Count==1?" "+operands[0]:"")+"\n";
   if(cmd=="grep") {var rx=new Regex(pattern,insensitive?RegexOptions.IgnoreCase:RegexOptions.None,TimeSpan.FromMilliseconds(100));output=JoinLines(lines.Select((s,i)=>new{Text=s,N=i+1}).Where(x=>rx.IsMatch(x.Text)).Select(x=>(number?x.N+":":"")+x.Text));}
  }
  else if(cmd=="mkdir"||cmd=="touch") {
   CheckFlags(flags,cmd=="mkdir"?"p":"");if(pos.Count!=1)throw new Exception(cmd+" 需要一个路径。");string p=Resolve(pos[0]);e.Paths.Add(p);
   if(cmd=="touch"){if(!Has(p))Write(p,"",false);}
   else {if(Has(p)){if(!IsDir(p)||!flags.Contains("-p"))throw new Exception("路径已存在："+p);}else {if(flags.Contains("-p")){string q="";foreach(var s in p.Split('/').Where(x=>x!="")){q+="/"+s;if(Has(q)&&!IsDir(q))throw new Exception("父路径中有文件。");if(!Has(q))Files[q]=new VFile(true,"");}}else{if(!IsDir(Parent(p)))throw new Exception("父目录不存在；可使用 mkdir -p。");Files[p]=new VFile(true,"");}}}
  }
  else if(cmd=="cp"||cmd=="mv") {
   CheckFlags(flags,"");if(pos.Count!=2)throw new Exception(cmd+" 需要源文件和目标路径。");string src=Resolve(pos[0]),dst=Resolve(pos[1]);if(IsDir(dst))dst+="/"+Base(src);e.Paths.Add(src);e.Paths.Add(dst);
   string data=Read(src);if(src==dst)throw new Exception("源路径和目标路径相同。");Write(dst,data,false);if(cmd=="mv")Files.Remove(src);
  }
  else if(cmd=="rm") {
   CheckFlags(flags,"");if(pos.Count!=1)throw new Exception("本版本 rm 一次删除一个文件，暂不支持递归删除。");string p=Resolve(pos[0]);Read(p);Files.Remove(p);e.Paths.Add(p);
  }
  else if(cmd=="find") {
   string root=".";int i=0;if(args.Count>0&&!args[0].StartsWith("-"))root=args[i++];if(args.Count!=i+2||args[i]!="-name")throw new Exception("用法：find 目录 -name '*.fastq'（模式请加引号）。");
   string p=Resolve(root);if(!IsDir(p))throw new Exception("搜索目录不存在。");e.Paths.Add(p);string glob="^"+Regex.Escape(args[i+1]).Replace("\\*",".*").Replace("\\?",".")+"$";
   output=JoinLines(Files.Keys.Where(k=>(k==p||k.StartsWith(p=="/"?"/":p+"/"))&&Regex.IsMatch(Base(k),glob)).OrderBy(k=>k));
  }
  else if(cmd=="echo")output=String.Join(" ",args)+"\n";
  else throw new Exception("暂不支持命令「"+cmd+"」。输入 help 查看支持的命令；Linux 命令区分大小写。");
  e.Output=output;Events.Add(e);return output;
 }
 static void NoArgs(List<string> a){if(a.Count>0)throw new Exception("该命令不需要参数。");}
 static void CheckFlags(List<string> flags,string supported){foreach(string f in flags)if(f.Length<2||f.Substring(1).Any(c=>supported.IndexOf(c)<0))throw new Exception("本版本不支持参数 "+f+"。输入 help 查看支持范围。");}
 static string JoinLines(IEnumerable<string> lines){var a=lines.ToArray();return a.Length==0?"":String.Join("\n",a)+"\n";}
 public bool Used(string cmd,string path){return Events.Any(e=>e.Name==cmd&&(path==null||e.Paths.Contains(path)));}
 public string Complete(string line){int n=line.LastIndexOf(' ');string prefix=line.Substring(n+1);if(prefix.Length==0)return line;string full=Resolve(prefix);var choices=Files.Keys.Where(k=>k.StartsWith(full,StringComparison.Ordinal)&&k!="/").ToArray();if(choices.Length!=1)return line;string addition=choices[0].Substring(full.Length)+(IsDir(choices[0])?"/":"");return line+addition;}
}
public class Lesson {
 public string Module,Title,Concept,Example,Task,Hint,Start,Why;public Func<Shell,bool> Check;
 public Lesson(string module,string title,string concept,string example,string task,string hint,string start,string why,Func<Shell,bool> check){Module=module;Title=title;Concept=concept;Example=example;Task=task;Hint=hint;Start=start;Why=why;Check=check;}
 public static List<Lesson> All(){string p=Shell.Project,r=p+"/data/raw/ctrl_01.fastq",s=p+"/samples.tsv";return new List<Lesson>{
 new Lesson("01  认识目录","我现在在哪里？","终端通过文字接收指令。输入命令后按 Enter 执行。pwd 会显示当前工作目录的绝对路径，从 / 开始。\n\n命令区分大小写。输入时不需要加提示符 $。","pwd","显示你现在所在的目录。","输入 pwd，然后按 Enter。",Shell.Home,"就像先确认实验台的位置，操作文件前先确认当前目录。",x=>x.Used("pwd",null)),
 new Lesson("01  认识目录","看看目录里有什么","ls 列出当前目录中的文件和子目录。目录类似文件夹；文件则保存具体内容。以 . 开头的隐藏文件需要 ls -a 才能看到。","ls","列出 home 目录中的内容，找到 project。","使用 ls；输出中的 project/ 是项目文件夹。",Shell.Home,"先检查数据放在哪里，避免凭记忆输入错误路径。",x=>x.Used("ls",Shell.Home)),
 new Lesson("01  认识目录","进入你的研究项目","cd 表示 change directory，用于切换当前目录。相对路径从当前位置出发，绝对路径从根目录 / 出发。\n\ncd 只改变你所在的位置，不会移动文件。","cd project","进入 project 目录。可以再用 pwd 确认位置。","输入 cd project。",Shell.Home,"进入项目目录后，就能用简短的相对路径访问样本表。",x=>x.Cwd==p),
 new Lesson("01  认识目录","读懂详细文件列表","ls -l 显示文件类型、权限、所属用户和大小。开头的 d 表示目录，- 表示普通文件。\n\nll 是常见别名，并非所有 Linux 系统都自带。本练习将 ll 设为 ls -l。","ls -l","查看项目目录的详细列表，使用 ls -l 或 ll。","输入 ll，或输入 ls -l。",p,"可以快速确认样本表是否存在，以及一个路径到底是文件还是目录。",x=>x.Events.Any(e=>e.Paths.Contains(p)&&(e.Name=="ll"||(e.Name=="ls"&&e.Args.Any(a=>a.StartsWith("-")&&a.Contains("l")))))),
 new Lesson("01  认识目录","沿着路径返回",". 表示当前目录，.. 表示上一级目录。~ 表示用户主目录。\n\ncd .. 返回一层；cd - 回到上次所在目录；单独输入 cd 返回主目录。","cd ..","从 data/raw 返回到 data 目录。","当前位置是 project/data/raw；输入 cd ..。",p+"/data/raw","分析时经常在原始数据目录、结果目录之间切换。",x=>x.Cwd==p+"/data"),
 new Lesson("02  查看生物数据","打开样本信息表","cat 会把文本文件的全部内容输出到终端。samples.tsv 是用制表符分隔的样本表，首行是列名。\n\n真实测序文件可能很大，先用 head 查看会更合适。","cat samples.tsv","查看 samples.tsv 的完整内容。","输入 cat samples.tsv。",p,"分析前先核对样本名称与实验分组。",x=>x.Used("cat",s)),
 new Lesson("02  查看生物数据","查看第一条测序读段","head 默认显示前 10 行，-n 可以指定行数。本例为未压缩、每条读段恰好 4 行的简化 FASTQ：标识、序列、+、质量值。","head -n 4 data/raw/ctrl_01.fastq","只显示 ctrl_01.fastq 的前 4 行。","head -n 4 data/raw/ctrl_01.fastq",p,"快速检查文件格式，无需把整个测序文件打印出来。",x=>x.Events.Any(e=>e.Name=="head"&&e.Paths.Contains(r)&&e.Output==String.Join("\n",x.Read(r).Split('\n').Take(4))+"\n")),
 new Lesson("02  查看生物数据","检查文件结尾","tail 与 head 相反，查看末尾内容。tail -n 4 显示最后 4 行。这里使用小型示例；本版本不支持 tail -f。","tail -n 4 data/raw/ctrl_01.fastq","查看 ctrl_01.fastq 的最后 4 行。","tail -n 4 data/raw/ctrl_01.fastq",p,"观察输出文件末尾，有助于初步检查内容是否完整。",x=>x.Events.Any(e=>e.Name=="tail"&&e.Paths.Contains(r)&&e.Output=="@CTRL_READ_004\nCCGTAACCGTAA\n+\nIIIIIIIIIIII\n")),
 new Lesson("02  查看生物数据","数一数文件有多少行","wc -l 统计换行符数量。演示 FASTQ 有 16 行，每条读段 4 行，所以是 4 条读段。\n\n这个换算仅适用于此处的四行式 FASTQ；压缩文件不能直接这样统计。","wc -l data/raw/ctrl_01.fastq","统计 ctrl_01.fastq 的行数。","使用 wc -l，加上 FASTQ 文件的路径。",p,"在理解文件格式的前提下，做一次简单的数据量核对。",x=>x.Used("wc",r)),
 new Lesson("02  查看生物数据","从样本表筛选对照组","grep 查找匹配表达式的行。grep control samples.tsv 会保留包含 control 的行。\n\n-i 忽略大小写，-n 显示行号。这里的 control 会匹配两个对照样本。","grep control samples.tsv","筛选并显示 samples.tsv 中的两行对照组样本。","grep control samples.tsv",p,"从样本表快速找到一个实验分组。",x=>x.Events.Any(e=>e.Name=="grep"&&e.Paths.Contains(s)&&e.Output.Contains("ctrl_01")&&e.Output.Contains("ctrl_02")&&!e.Output.Contains("treat_01"))),
 new Lesson("03  整理分析文件","为本次分析新建目录","mkdir 创建目录。目录尚不存在时才需要创建；mkdir -p 可以连同不存在的父目录一起创建。","mkdir results/qc","在 results 下创建 qc 子目录。","输入 mkdir results/qc，再用 ls results 检查。",p,"将质控结果和原始测序数据分开存放。",x=>x.IsDir(p+"/results/qc")),
 new Lesson("03  整理分析文件","备份样本表","cp 源文件 目标路径 会复制文件，原文件仍然保留。\n\n本教学版本只复制普通文件；真实环境中覆盖同名文件需要特别留意。","cp samples.tsv results/samples_backup.tsv","把样本表备份为 results/samples_backup.tsv。","cp samples.tsv results/samples_backup.tsv",p,"编辑重要样本信息前，先保留一份备份。",x=>x.Has(p+"/results/samples_backup.tsv")&&x.Read(p+"/results/samples_backup.tsv")==x.Read(s)),
 new Lesson("03  整理分析文件","给临时文件改名","mv 源路径 目标路径 可以改名或移动文件。与 cp 不同，成功后旧路径不再存在。","mv results/old.txt results/notes.txt","将 results/old.txt 改名为 results/notes.txt。","mv results/old.txt results/notes.txt",p,"让分析结果名称更明确，便于以后查找。",x=>!x.Has(p+"/results/old.txt")&&x.Has(p+"/results/notes.txt")),
 new Lesson("03  整理分析文件","删除一个临时文件","rm 删除指定文件。真实 Linux 中，rm 通常不会把文件放入回收站。\n\n这里操作的是模拟数据，可以通过“重置本关”恢复。本版本不支持 rm -r。","rm results/old.txt","删除 results/old.txt，保留其他项目文件。","先 ls results 确认，再输入 rm results/old.txt。",p,"仅删除确认无用的临时文件，养成先检查再操作的习惯。",x=>!x.Has(p+"/results/old.txt")),
 new Lesson("04  组合命令","让两条命令协作","管道符 | 将左侧命令的输出交给右侧命令。\n\ngrep 先筛选对照组，wc -l 再统计筛选结果的行数。这次统计的是样本数，不是读段数。","grep control samples.tsv | wc -l","用管道筛选对照组并统计行数，让终端输出 2。","grep control samples.tsv | wc -l",p,"把简单工具组合起来，回答具体的实验问题。",x=>x.Events.Count>=2&&x.Events[x.Events.Count-1].Name=="wc"&&x.Events[x.Events.Count-1].Paths.Count==0&&x.Events[x.Events.Count-1].Output=="2\n"&&x.Events[x.Events.Count-2].Name=="grep"&&x.Events[x.Events.Count-2].Paths.Contains(s)),
 new Lesson("04  组合命令","把筛选结果保存下来","> 将输出保存到文件，并覆盖同名文件原有内容。>> 则在文件末尾追加。\n\n重定向成功时终端可能不显示内容，可以用 cat 检查保存的结果。","grep control samples.tsv > results/control.tsv","把两行对照组样本保存为 results/control.tsv。","grep control samples.tsv > results/control.tsv",p,"将筛选结果保留下来，供下一步分析使用。",x=>x.Has(p+"/results/control.tsv")&&x.Read(p+"/results/control.tsv")=="ctrl_01\tcontrol\tctrl_01.fastq\nctrl_02\tcontrol\tctrl_02.fastq\n"),
 new Lesson("04  组合命令","寻找测序文件","find 从指定目录开始搜索。-name 按文件名匹配，* 表示任意长度的字符。\n\n用引号包住 '*.fastq'，让模式由 find 处理。本模拟终端只在 find -name 中支持通配符。","find data -name '*.fastq'","找出 data 目录下的两个 .fastq 文件。","find data -name '*.fastq'",p,"当数据按多层目录存放时，递归搜索比逐层打开更方便。",x=>x.Events.Any(e=>e.Name=="find"&&e.Paths.Contains(p+"/data")&&e.Args.Contains("*.fastq"))),
 new Lesson("05  综合小实验","整理一份对照组交付包","现在把前面学过的命令串起来。你将建立交付目录、保留样本表副本，再输出对照组信息。\n\n本关不限定输入顺序；完成后会检查文件位置与实际内容。","mkdir results/delivery\ncp samples.tsv results/delivery/samples.tsv\ngrep control samples.tsv > results/delivery/control.tsv","① 创建 results/delivery\n② 复制完整样本表到该目录，文件名 samples.tsv\n③ 筛选两行对照样本并保存为同目录的 control.tsv","按上方三条示例依次输入；可用 ls 和 cat 自行检查。",p,"你已经能完成一个小型、可重复的生物数据整理流程。",x=>x.IsDir(p+"/results/delivery")&&x.Has(p+"/results/delivery/samples.tsv")&&x.Read(p+"/results/delivery/samples.tsv")==x.Read(s)&&x.Has(p+"/results/delivery/control.tsv")&&x.Read(p+"/results/delivery/control.tsv")=="ctrl_01\tcontrol\tctrl_01.fastq\nctrl_02\tcontrol\tctrl_02.fastq\n")
 };}
}
}
