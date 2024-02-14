using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace self_signed_exe_updater.deps
{
    internal class BuildLogFile
    {
        
        public readonly string ProjectName;
        public readonly int EntryCount;
        public readonly string? LogFileName;
        public BuildLoggingCollection? Loggings;
        public bool AutoUpdateFile { get; set; } = false;
        public BuildLogFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
               
            }
            else
            {
                string[] data = File.ReadAllLines(path);
                ProjectName = data[0];
                EntryCount = data.Where((ss, ee) => ss.StartsWith('*') && ee > 0).Count();
                Load(data);
            }
        }
        private void Load(string[] data)
        {
            Loggings = new BuildLoggingCollection(this);
            Task.Run(() =>
            {
                foreach (var item in data)
                {
                    Loggings.Add(item);
                }
            });
        }
        public void Save()
        {
            if (Loggings == null || LogFileName == null)
            {
                throw new InvalidOperationException();
            }
            if (File.Exists(LogFileName))
            {
                File.Delete(LogFileName);
            }
            StreamWriter sw  =File.CreateText(LogFileName); 
            foreach (BuildLogging log in Loggings)
            {
                sw.WriteLine(log.Definition);
            }
            sw.FlushAsync();
            sw.Close();
            sw.Dispose();
        }
        public void AddLogChange(LoggingUpdate type, string targetFilename, string reference = "")
        {
            if (Loggings == null)
            {
                throw new InvalidOperationException();
            }
            string deno = string.Format(
                BuildLogging.Format,
                DateTimeOffset.Now,
                targetFilename,
                ProjectName,
                new FileInfo(targetFilename).Length,
                reference, 
                type
                );
            Loggings.Add(deno);
            if (AutoUpdateFile)
            {
                Save();
            }
        }        

    }
    internal sealed class BuildLoggingCollection : Collection<BuildLogging>, IEnumerable<BuildLogging>
    {
        public readonly BuildLogFile Owner;
        public BuildLoggingCollection(BuildLogFile owner)
        {
            Owner = owner;
        }
        private BuildLoggingCollection() { }
        public void Add(string denomination)
        {
            if (denomination.StartsWith("*"))
            {
                base.Add(new BuildLogging(denomination));
            }
        }

    }
    enum LoggingUpdate
    {
        Build, 
        DebugBuild,
        UpdateFile,
        UpdateResource,
        undefined,
    }
    internal class BuildLogging
    {
        
        public const string Format = "[{0}] {1} {2} {3} {4} {5}";
        public readonly string Definition;
        public readonly DateTimeOffset CreationTime;
        public readonly string FileName;
        public readonly string ProjectName;
        public readonly long Size;
        public readonly string Reference;
        public readonly LoggingUpdate Type;
        public BuildLogging(string deno)
        {
            Definition = deno;
            string[] tokens = deno.Split(' ');
            var dtstr = tokens[0].Trim('[', ']');
            var fname = tokens[1];
            var prname = tokens[2];
            var len = tokens[3];
            var refr = tokens[4];
            var type = tokens[5];
            FileName = fname;
            CreationTime = DateTimeOffset.Parse(dtstr);
            Size = long.Parse(len);
            Type = (LoggingUpdate)Enum.Parse(typeof(LoggingUpdate), type);
            Reference = refr;
            ProjectName = prname;
        }
        public static BuildLogging[] FromFile(string path)
        {
            var lns = File.ReadAllLines(path);
            Collection<BuildLogging> list = new Collection<BuildLogging>();
            foreach(var l in lns)
            {
                list.Add(new BuildLogging(l));
            }
            return list.ToArray();
        }
    }
}
