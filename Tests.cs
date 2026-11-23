using System;
using System.IO;
using System.Linq;
using System.Text;
namespace BioShell {
public static class Tests {
 static int count; static void Assert(bool ok,string name){count++;if(!ok)throw new Exception("FAIL: "+name);}
 public static int Run(){try{
  string p=Shell.Project;var s=new Shell(p);
  Assert(s.Execute("wc -l data/raw/ctrl_01.fastq").StartsWith("16 "),"FASTQ lines");
  Assert(s.Execute("grep control samples.tsv | wc -l")=="2\n"&&s.Success,"pipeline count");
  s.Execute("grep control samples.tsv > results/control.tsv");Assert(s.Success&&s.Read(p+"/results/control.tsv").Split('\n').Length==3,"redirect data");
  s.Execute("echo 'a > b | c' > results/quoted.txt");Assert(s.Read(p+"/results/quoted.txt")=="a > b | c\n","quoted operators");
  s.Execute("echo next >> results/quoted.txt");Assert(s.Read(p+"/results/quoted.txt").EndsWith("next\n"),"append");
  s.Execute("mkdir -p 'results/my folder/sub'");Assert(s.IsDir(p+"/results/my folder/sub"),"quoted nested directories");
  s.Execute("cp samples.tsv 'results/my folder/copy.tsv'");s.Execute("mv 'results/my folder/copy.tsv' results/moved.tsv");Assert(s.Has(p+"/results/moved.tsv")&&!s.Has(p+"/results/my folder/copy.tsv"),"move preserves data");
  s.Execute("rm results/moved.tsv");Assert(!s.Has(p+"/results/moved.tsv"),"remove file");
  s.Execute("rm /");Assert(!s.Success&&s.IsDir("/"),"reject directory deletion");
  s.Execute("cat missing.tsv");Assert(!s.Success,"missing file error");s.Execute("cat 'unclosed");Assert(!s.Success,"quote error");
  s.Execute("cd data/raw");s.Execute("cd ../..");Assert(s.Cwd==p,"relative normalization");s.Execute("cd -");Assert(s.Cwd==p+"/data/raw","previous directory");s.Execute("cd ~");Assert(s.Cwd==Shell.Home,"home expansion");
  Assert(s.Execute("ls").Contains("project/")&&!s.Execute("ls").Contains(".bashrc"),"hide dotfiles");Assert(s.Execute("ls -a").Contains(".bashrc"),"show dotfiles");
  s=new Shell(p);Assert(s.Execute("find data -name '*.fastq'").Split('\n').Length==3,"recursive glob");
  Assert(s.Execute("head -n 0 samples.tsv")=="","zero head");Assert(s.Execute("tail -n 0 samples.tsv")=="","zero tail");
  s.Execute("wc -z samples.tsv");Assert(!s.Success,"unsupported flags explicit");
  Assert(s.Complete("cat sam")=="cat samples.tsv","path completion");
  string[][] solutions={new[]{"pwd"},new[]{"ls"},new[]{"cd ./project"},new[]{"ll"},new[]{"cd .."},new[]{"cat samples.tsv"},new[]{"head -n 4 data/raw/ctrl_01.fastq"},new[]{"tail -n 4 data/raw/ctrl_01.fastq"},new[]{"wc -l data/raw/ctrl_01.fastq"},new[]{"grep control samples.tsv"},new[]{"mkdir results/qc"},new[]{"cp samples.tsv results/samples_backup.tsv"},new[]{"mv results/old.txt results/notes.txt"},new[]{"rm results/old.txt"},new[]{"grep control samples.tsv | wc -l"},new[]{"grep control samples.tsv > results/control.tsv"},new[]{"find data -name '*.fastq'"},new[]{"mkdir results/delivery","cp samples.tsv results/delivery/samples.tsv","grep control samples.tsv > results/delivery/control.tsv"}};
  var lessons=Lesson.All();Assert(lessons.Count==solutions.Length,"solution coverage");
  for(int i=0;i<lessons.Count;i++){s=new Shell(lessons[i].Start);Assert(!lessons[i].Check(s),"lesson not precompleted "+i);foreach(string cmd in solutions[i]){s.Execute(cmd);Assert(s.Success,"lesson command "+i);}Assert(lessons[i].Check(s),"lesson passes "+i);}
  s=new Shell(p);s.Execute("echo 2");Assert(!lessons[14].Check(s),"reject unrelated output for pipeline");
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test-result.txt"),"PASS: "+count+" assertions; all 18 lessons solved.\r\n",Encoding.UTF8);return 0;
 }catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test-result.txt"),e.ToString(),Encoding.UTF8);return 1;}}
}
}
