using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Console = System.ExConsole;
using prot = System.FTP;
using System.Net.Http.Headers;
using System.Net;
using System.Web;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.IO;
using System.Xml.Linq;

namespace sseu
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct OpenFileName
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int flagsEx;
    }

    public class Program
    {
        public const string RegistryPath = "/dev_flash2/etc/xRegistry.sys";
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);
        static 
            long oldsize = -99;
        static long size = -99;
        private static string ShowDialog()
        {
            var ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            // Define Filter for your extensions (Excel, ...)
            ofn.lpstrFilter = "PS3 Signed PRX(*.sprx)\0*.sprx\0All Files (*.*)\0*.*\0";
            ofn.lpstrFile = new string(new char[256]);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string(new char[64]);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
            ofn.lpstrTitle = "Open File Dialog...";
            if (GetOpenFileName(ref ofn))
                return ofn.lpstrFile;
            return string.Empty;
        }
        static FileSystemWatcher? _lsprx;
        private static string? sprx;
        private static string? address;
        private static DateTime? lupdtime;
        private static string GetAppPath() => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static void PrintCmd(string cmd, string hint)
        {
            Console.Write(cmd, ConsoleColor.Blue);
            Console.Write(": " + hint + ".\n", ConsoleColor.Gray);
        }
        private static bool TExists() => sprx != null && File.Exists(sprx);
        private static void PrintCmds()
        {
            Console.Write("-------------------------------------\n");
            PrintCmd("active", "Prints active SPRX filename");
            PrintCmd("upload", "uploads the sprx copy to /hdd0/tmp");
            PrintCmd("resign", "resigns the debug sprx for working on HEN. Must have a compatible ps3 tools version");
            PrintCmd("updrn", "uploads the sprx copy to /hdd0/tmp with a different name");
            PrintCmd("chksz", "displays active local copy size");
            PrintCmd("remove", "deletes the sprx copy from /hdd0/tmp");
            PrintCmd("debug", "Performs a SPRX by custom protocol");
            PrintCmd("delete", "deletes the local selected sprx");
            PrintCmd("chup", "displays last uploaded copy time");
            PrintCmd("trace", "if the trace exists, reads it");
            PrintCmd("tracerm", "if the trace exists, deletes it");
            PrintCmd("reload", "Restarts the active PS3 process");
            PrintCmd("help", "Prints this list");
            PrintCmd("ps3", "sends a webMAN command request");
            PrintCmd("bkrst", "Restores a specified registry file by safefilename");
            PrintCmd("bkls", "List all backups from registrys on the directory");
            PrintCmd("bkrm", "Deletes all registry backups");
            PrintCmd("bkreg", "Backups active console server's registry file");
            PrintCmd("chip [string ipByDots]", "Changes active console IP address");
            PrintCmd("popup [string message]", "Popups messages to the VSH");
            PrintCmd("ping [int ping=3000]", "Pings the console ip server");
            if (Environment.UserName == "root")
            {
                PrintCmd("release", "sends release to Discord.");
            }
            Console.Write("-------------------------------------\n");

        }
        private static void Ps3BackupRestore(string safename)
        {
            if (address == null)
            {
                Console.Write("No address has been specified yet. Use 'chip'", ConsoleColor.Yellow); return;
            }
            // removes the format for working as always
            var safesafename = safename;
            if (safename.Split('.').Length > 1)
            {
                safesafename = safename.Split(".")[0];
            }
            // the dir
            var bkpath = GetAppPath() + "\\reg\\";
            var bk = bkpath + safesafename + ".sys";
            if (File.Exists(bk))
            {
                Console.Write("Are you ready. Don't shutdown the PS3", ConsoleColor.Yellow);
                Console.ReadLine(); var loc = $"ftp://{address}" + RegistryPath;

                FTP.DeleteFile(new Uri(loc));

                FTP.Upload(bk, $"ftp://{address}/dev_flash2/etc", (o2) =>
                {
                    // dl progress
                    var progress = o2;
                    int x = (int)progress.X / 1024;
                    int y = (int)progress.Y / 1024;
                    Console.Clear();
                    Console.Write($"{x}/{y} \n", ConsoleColor.DarkGray);

                }, ThreadPriority.Normal, (ss, ee) =>
                {
                    new WebClient().DownloadData($"http://{address}/restart.ps3");
                    Console.Write("Restoring finished. Restarting the system...");
                    Thread.Sleep(4000);
                    Console.Clear();
                });
            }
            else
            {
                Console.Write("Cannot find file.", ConsoleColor.Yellow);
            }
        }

        /// <summary>
        /// List all backups in directory
        /// </summary>
        private static void Ps3BackupLS()
        {
            var bkpath = GetAppPath() + "\\reg";
            var fns = Directory.GetFiles(bkpath).Where((s, b) => Path.GetExtension(s) == ".sys");

            foreach (var fn in fns)
            {
                var ct = GetTimeStr(new FileInfo(fn).CreationTime);
                Console.Write(Path.GetFileName(fn), ConsoleColor.Magenta);
                Console.Write(" " + ct + "\n", ConsoleColor.Gray);
            }
        }
        /// <summary>
        ///  Deletes all active backups on the directory.
        /// </summary>
        private static void Ps3BackupClear()
        {

            var bkpath = GetAppPath() + "\\reg";
            var fns = Directory.GetFiles(bkpath).Where((s, b) => Path.GetExtension(s) == ".sys");
            int count = 0;
            foreach (string s in fns)
            {

                try
                {
                    File.Delete(s);
                    count++;
                }
                catch (Exception err)
                {
                    Console.Write("Can't delete " + Path.GetFileName(s));
                }
            }
            Console.Write($"OK! Deleted {count} files", ConsoleColor.Yellow);
        }
        private static void Ps3BackupActiveRegistry()
        {
            if (address == null)
            {
                Console.Write("No address has been specified yet. Use 'chip'", ConsoleColor.Yellow); return;
            }
            else
            {
                if (address.IsAccessible())
                {
                    var loc = $"ftp://{address}" + RegistryPath;
                    if (FTP.GetLength(loc) > 0)
                    {
                        var bkpath = GetAppPath() + "\\reg";
                        if (!Directory.Exists(bkpath))
                        {
                            Directory.CreateDirectory(bkpath);
                        }
                        File.SetAttributes(bkpath, FileAttributes.Hidden);
                        Console.Write("Don't share your registry files to another persons!.\n", ConsoleColor.Red);
                        Console.Write("Set backup name.\n", ConsoleColor.Yellow);
                        var r = Console.ReadLine();

                        int flag = Directory.GetFiles(bkpath).Where((ss, ee) => Path.GetExtension(ss) == ".sys").ToArray().Length;
                        var nm = String.IsNullOrEmpty(r) ? $"reg{flag}_{GetTimeStr()}" : r;
                        var locname = bkpath + $"\\{nm}.sys";
                        bool overwriting = false;
                        if (File.Exists(locname))
                        {
                            Console.Write("Overwrite? [Y/N]", ConsoleColor.Yellow);
                            var rr = Console.ReadLine();
                            if (rr.Contains("Y"))
                            {
                                overwriting = true;
                            }
                            else if (rr.Contains("N"))
                            {
                                overwriting = false;
                            }
                        }
                        if (overwriting)
                        {
                            File.Move(locname, locname + "_old");
                        }
                        else
                        {
                            File.Delete(locname);
                        }
                        FTP.Download(locname, loc, "sseu", "sseu", (ss, ee) =>
                        {
                            long len = new FileInfo(locname).Length;
                            // finish
                            Console.Write($"Backup complete ({len.ToMemUnit()})\n", ConsoleColor.Green);

                        }, (o1, o2) =>
                        {
                            // dl progress
                            var progress = o2;
                            int x = (int)progress.X / 1024;
                            int y = (int)progress.Y / 1024;
                            Console.Clear();
                            Console.Write($"{x}/{y} \n", ConsoleColor.DarkGray);

                        }, (o1, o2) =>
                        {
                            Console.LogException(o2);
                            Console.ReadLine();
                        }, ThreadPriority.Normal);
                    }
                }
            }
        }
        public static long Notify(string message)
        {
            if (address != null)
            {
                try
                {
                    var wc = new WebClient();
                    return wc.DownloadData("http://" + address + "/popup.ps3?" + HttpUtility.UrlEncode(message)).LongLength;

                }
                catch (Exception error)
                {
                    Console.LogException(error);
                    return 0;
                }
            }
            else
            {
                Console.Write("No address specified.", ConsoleColor.Red);
            }
            return 0;
        }
        public static void Main(string[] args)
        {
            var filename = ShowDialog();
            sprx = filename;
            PrintCmds();
            Console.Write("Set console server IP address.\n");
            address = Console.ReadLine();
            Console.Write("Ready.\n", ConsoleColor.Green);
            while (true)
            {
                var e = Console.ReadLine();
                if (e == "exit")
                {
                    Environment.Exit(0);
                    break;
                }
                else if (e == "upload")
                {
                    if (TExists())
                    {

                        Console.Clear();
                        PrintCmds();
                        Console.Write($"{address} << {Path.GetFileName(filename)}\n", ConsoleColor.Blue);
                        prot.Upload(filename, $"ftp://{address}/dev_hdd0/tmp/", UploadProgression, ThreadPriority.AboveNormal, (ss, ee) => OnUploadFinished());

                    }
                    else
                    {
                        Console.Write("Sprx doest not exists.\n", ConsoleColor.Red);
                    }
                }
                else if (e.StartsWith("ps3"))
                {
                    var arg = e.Split(' ').ToList();
                    arg.Remove("ps3");
                    arg = arg.ToList();
                    var cmd = (string.Join("", arg)).TrimStart('/');
                    var addr = $"http://{address}/{cmd}";
                    try
                    {

                        Console.Write(new WebClient().DownloadString(addr));
                    }
                    catch (Exception er)
                    {
                        Console.LogException(er);
                    }
                }
                else if ((Environment.UserName == "root") && e.StartsWith("release"))
                {
                    Console.Clear();
                    Console.Write("Post: " + Path.GetFileName(sprx)+"\n");
                    Console.Write("Set release message: ", ConsoleColor.Blue);
                    var r = Console.ReadLine();
                    dpos.Initialize();
                    Thread.Sleep(10000);
                    dpos.Release(sprx, r);
                    Thread.Sleep(5000);
                    dpos.Deinitialize();
                }
                else if (e.StartsWith("pad"))
                {
                    var arg = e.Split(' ').ToList();
                    arg.Remove("pad");
                    arg = arg.ToList();
                    var cmd = (string.Join("", arg)).TrimStart('/');
                    var addr = $"http://{address}/pad.ps3*";
                    Console.Write("Press 2 to exit", ConsoleColor.Gray);
                    while (true)
                    {
                        var r = System.Console.ReadKey();

                        void d(string code) =>
                       new WebClient().DownloadString(addr + code + (r.Modifiers == ConsoleModifiers.Shift ? "hold" : "press"));
                        if (r.Key == ConsoleKey.LeftArrow)
                        {
                            d("left");
                        }
                        else if (r.Key == ConsoleKey.RightArrow)
                        {
                            d("right");
                        }

                        else if (r.Key == ConsoleKey.DownArrow)
                        {
                            d("down");
                        }

                        else if (r.Key == ConsoleKey.UpArrow)
                        {
                            d("up");
                        }

                        else if (r.Key == ConsoleKey.Spacebar || r.Key == ConsoleKey.Enter)
                        {
                            d("cross");
                        }
                        else if (r.Key == ConsoleKey.Q) { d("ps_btn"); }
                        else if (r.Key == ConsoleKey.E) { d("triangle"); }
                        else if (r.Key == ConsoleKey.F) { d("circle"); }
                        else if (r.Key == ConsoleKey.R) { d("square"); }
                        else if (r.Key == ConsoleKey.D) { d("cross"); }
                        else if (r.Key == ConsoleKey.D1) { d("ps_btn"); }
                        else if (r.Key == ConsoleKey.W) { d("analogL_up"); }
                        else if (r.Key == ConsoleKey.A) { d("analogL_left"); }
                        else if (r.Key == ConsoleKey.S) { d("analogL_down"); }
                        else if (r.Key == ConsoleKey.L) { d("analogR_right"); }
                        else if (r.Key == ConsoleKey.I) { d("analogR_up"); }
                        else if (r.Key == ConsoleKey.J) { d("analogR_left"); }
                        else if (r.Key == ConsoleKey.K) { d("analogR_down"); }
                        else if (r.Key == ConsoleKey.Tab) { d("select"); }
                        else if (r.Key == ConsoleKey.Backspace) { d("start"); }

                        else if (r.Key == ConsoleKey.D2) { break; }
                    }
                }
                else if (e == "trace")
                {
                    var t = $"ftp://{address}/dev_hdd0/trace.log";
                    if (FTP.GetLength(t) != 0)
                    {
                        var trace = new WebClient().DownloadString(t);
                        Console.Clear();
                        Console.Write("<------ TRACE ------->\n", ConsoleColor.DarkYellow);
                        Console.Write(trace);
                        Console.Write("\n<------ ENDOFTRACE ------->\n");
                    }
                }
                else if (e == "tracerm")
                {
                    var t = $"ftp://{address}/dev_hdd0/trace.log";
                    if (FTP.GetLength(t) != 0)
                    {
                        FTP.DeleteFile(new Uri(t));
                        Console.Write("\nTrace removing ok!", ConsoleColor.Green);
                    }
                }
                else if (e == "debug")
                {
                    void Op()
                    {
                        Console.Write("Set debugging flags: ", ConsoleColor.White); Console.Write("[N: Upload Rename, R: Reload, B: Backup, S: Resign]\n", ConsoleColor.Yellow);
                        var f = Console.ReadLine();
                        var n = Path.GetFileName(sprx);
                        bool rl = false;
                        bool bk = false;
                        bool rs = false;
                        var newname = sprx;



                        if (f.Contains('N'))
                        {
                            Console.Write("Set target name:\n", ConsoleColor.Yellow);
                            n = Console.ReadLine();
                            newname = Path.GetDirectoryName(sprx) + "\\" + n;
                            File.Copy(sprx, newname, true);
                            Console.Write("Copied as new name for reuploading\n", ConsoleColor.Green);
                        }

                        if (bk)
                        {
                            File.Copy(sprx, sprx + "_UnresignedDebugBackup", true);
                        }
                        if (f.Contains('R')) { rl = true; }
                        if (f.Contains('B')) { bk = true; }
                        if (f.Contains('S')) { rs = true; }
                        if (rs)
                        {
                            var usnr = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            var path = filename;
                            string cmd = $"cd {usnr}\\ps3tools\\ps3tools\\tools\r\nPKG_ContentID.exe --resign {newname}";
                            var batch = GetAppPath() + "\\resigns.bat";
                            if (File.Exists(batch))
                            {
                                File.Delete(batch);
                            }
                            File.WriteAllText(batch, cmd);
                            Thread.Sleep(100);
                            if (File.Exists(batch))
                            {
                                var prc = Process.Start(batch);
                                prc.EnableRaisingEvents = true;
                                Console.Write("Waiting for external PS3Tools...\n", ConsoleColor.Yellow);
                                while (!prc.HasExited)
                                {
                                    Thread.Sleep(10);
                                }
                                Console.Write("Resigned. \n", ConsoleColor.Green);
                            }
                            else
                            {
                                Console.Write("Cannot resign.", ConsoleColor.Red);
                            }
                        }
                        if (!string.IsNullOrEmpty(n))
                        {
                            if (TExists())
                            {
                                Console.Write($"{address} << {Path.GetFileName(newname)}\n", ConsoleColor.Blue);
                                prot.Upload(newname, $"ftp://{address}/dev_hdd0/tmp/", UploadProgression, ThreadPriority.AboveNormal, (ss, ee) =>
                                {
                                    var fi = new FileInfo(newname);
                                    Console.Write("Changes updated.(", ConsoleColor.Green);
                                    Console.Write(fi.Length.ToMemUnit() + ")\n");
                                    if (rl)
                                    {
                                        Console.Write("Requesting process to reload.", ConsoleColor.Gray);
                                        if (address != null)
                                        {
                                            try
                                            {
                                                var r = new WebClient().DownloadData($"http://{address}/xmb.ps3$reloadgame");
                                                if (r.Length > 0)
                                                {
                                                    Console.Write("Sucessfully send and reloaded process\n", ConsoleColor.Green);
                                                }
                                            }
                                            catch (Exception error)
                                            {
                                                Console.LogException(error);
                                            }
                                        }
                                        else
                                        {
                                            Console.Write("No address specified.", ConsoleColor.Red);
                                        }
                                    }
                                    string msg = "Update Build OK!\n" + n + "\nArguments: \n";
                                    int s = 0;
                                    if (rl)
                                    {
                                        msg += "Reload\n";
                                        s += 1;
                                    }
                                    if (bk)
                                    {
                                        msg += "Backup\n";
                                        s += 1;
                                    }
                                    if (rs)
                                    {
                                        msg += "Resign\n";
                                        s += 1;
                                    }
                                    if (s == 0)
                                    {
                                        msg += "None\n";
                                    }
                                    char c = '*';
                                    if (size != -99)
                                    {
                                        oldsize = size;
                                    }
                                    size = fi.Length;
                                    if (oldsize > size)
                                    {
                                        c = '-';
                                    }
                                    if (oldsize < size)
                                    {
                                        c = '+';
                                    }
                                    msg += c + fi.LastWriteTime.ToString("G") + "\n";
                                    msg += fi.Length.ToMemUnit();

                                    Notify(msg);
                                });
                            }
                            else
                            {
                                Console.Write("Sprx doest not exists.\n", ConsoleColor.Red);
                            }
                        }
                    }
                    try
                    {
                        Op();
                    }
                    catch (Exception err)
                    {
                        Console.LogException(err);
                        Console.Write("Retry?: Y/N\n", ConsoleColor.Yellow);
                        string s = Console.ReadLine();
                        if (s == "Y")
                        {
                            Op();
                        }
                        else
                        {
                            Console.Clear();
                            PrintCmds();
                            Console.Write("Operations cancelled", ConsoleColor.Yellow);
                        }
                    }


                }
                else if (e == "bkrm")
                {
                    Ps3BackupClear();
                }
                else if (e == "bkls")
                {
                    Ps3BackupLS();
                }
                else if (e == "resign")
                {
                    try
                    {
                        var usnr = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        var path = filename;
                        string cmd = $"cd {usnr}\\ps3tools\\ps3tools\\tools\r\nPKG_ContentID.exe --resign {path}";
                        var batch = GetAppPath() + "\\resigns.bat";
                        if (File.Exists(batch))
                        {
                            File.Delete(batch);
                        }
                        File.WriteAllText(batch, cmd);
                        Thread.Sleep(100);
                        if (File.Exists(batch))
                        {
                            var prc = Process.Start(batch);
                            prc.EnableRaisingEvents = true;
                            Console.Write("Waiting for external PS3Tools...\n", ConsoleColor.Yellow);
                            while (!prc.HasExited)
                            {
                                Thread.Sleep(10);
                            }
                            Console.Clear();
                            PrintCmds();
                            Console.Write("Resigning finished. No exceptions\n", ConsoleColor.Green);

                        }
                    }
                    catch (Exception err)
                    {
                        Console.Clear();
                        PrintCmds();
                        Console.LogException(err);
                    }
                }
                else if (e == "chksz")
                {
                    if (File.Exists(filename))
                    {
                        Console.Write(Path.GetFileName(filename) + " " + new FileInfo(filename).Length.ToMemUnit() + "\n");
                        Console.ReadLine();
                    }

                    else
                    {
                        Console.Write("Sprx doest not exists.\n", ConsoleColor.Red);
                    }
                }
                else if (e == "remove")
                {
                    string path = $"/dev_hdd0/tmp/{Path.GetFileName(filename)}";
                    FTP.DeleteFile(new Uri($"ftp://{address}" + path));
                    Console.Write($"-{path}\n", ConsoleColor.Red);
                }
                else if (e == "updrn")
                {
                    Console.Write("Set new output name: \n", ConsoleColor.Blue);
                    var n = Console.ReadLine();
                    if (!string.IsNullOrEmpty(n))
                    {
                        if (TExists())
                        {
                            var sprx = n.Split('.');
                            if (sprx.Length > 0)
                            {
                                n = sprx[0];
                            }
                            var cname = Path.GetDirectoryName(filename) + "\\" + n + ".sprx";
                            File.Copy(filename, cname, true);
                            Console.Clear();
                            PrintCmds();
                            Console.Write($"{address} << {Path.GetFileName(filename)}\n", ConsoleColor.Blue);
                            prot.Upload(cname, $"ftp://{address}/dev_hdd0/tmp/", UploadProgression, ThreadPriority.AboveNormal, (ss, ee) => OnUploadFinished());
                        }
                        else
                        {
                            Console.Write("Sprx doest not exists.\n", ConsoleColor.Red);
                        }
                    }

                }
                else if (e == "delete")
                {
                    if (TExists())
                    {
                        File.Delete(sprx);
                        Console.Write("-" + sprx, ConsoleColor.Red);
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.Write("Sprx doest not exists.\n", ConsoleColor.Red);
                    }
                }
                else if (e == "chup")
                {
                    if (lupdtime != null)
                    {
                        Console.Write("lupd: " + lupdtime.ToString() + "\n", ConsoleColor.Gray);
                    }
                    else
                    {
                        Console.Write("No data.\n", ConsoleColor.Gray);
                    }
                }
                else if (e.StartsWith("popup"))
                {
                    var r = e.Split(' ');
                    if (r.Length > 1)
                    {
                        long l = Notify(r[1]);
                        Console.Write(l.ToMemUnit() + "\n", ConsoleColor.Gray);
                    }
                    else
                    {
                        long l = Notify("Self Signed PRX Updater");
                        Console.Write(l.ToMemUnit() + "\n", ConsoleColor.Gray);
                    }
                }
                else if (e.StartsWith("bkrst"))
                {
                    var r = e.Split(' ');
                    if (r.Length > 1)
                    {
                        Console.Write("OK");
                        Ps3BackupRestore(r[1]);
                    }
                }
                else if (e == "reload")
                {

                    if (address != null)
                    {
                        try
                        {
                            new WebClient().DownloadData($"http://{address}/xmb.ps3$reloadgame");
                        }
                        catch (Exception error)
                        {
                            Console.LogException(error);
                        }
                    }
                    else
                    {
                        Console.Write("No address specified.", ConsoleColor.Red);
                    }
                }
                else if (e.StartsWith("chip"))
                {
                    var r = e.Split(' ');
                    if (r.Length > 1)
                    {
                        address = r[1];
                        Console.Write("Changed.", ConsoleColor.Gray);
                    }
                    else
                    {
                        Console.Write("No argument provided.\n", ConsoleColor.Gray);

                    }
                }
                else if (e == "help")
                {
                    Console.Clear();
                    PrintCmds();
                }
                else if (e == "clear")
                {
                    Console.Clear();
                }
                else if (e == "bkreg")
                {
                    Notify("Performing registry backup.\n Do not shutdown the system.");
                    Ps3BackupActiveRegistry();
                }
                else if (e.StartsWith("ping"))
                {
                    try
                    {

                        Console.Clear();
                        PrintCmds();
                        var ee = e.Split(' ');

                        var P = new Ping();
                        int to = (ee.Length > 1 ? (int.Parse(ee[1])) : 3000);
                        var PR = P.Send(IPAddress.Parse(address), to);
                        Console.Write("RT: " + PR.RoundtripTime + "ms\n", ConsoleColor.Gray);
                        Console.Write("Received " + PR.Buffer.LongLength.ToMemUnit() + "\n", ConsoleColor.Gray);
                        Console.Write(PR.Status.ToString(), ConsoleColor.Yellow);
                    }
                    catch (Exception err) { Console.LogException(err); }
                }
                else if (e == "active")
                {
                    Console.Write(sprx+"\n", ConsoleColor.DarkGray);
                    Console.ReadLine();
                }
                else
                {
                    Console.Write("?\n", ConsoleColor.Yellow);
                }
            }
        }
        private static Action<System.Pair<long>> UploadProgression
        {
            get { return new Action<Pair<long>>((p) => { OnUploadProgression(p); }); }
        }
        private static void OnUploadProgression(Pair<long> progress)
        {
            int x = (int)progress.X / 1024;
            int y = (int)progress.Y / 1024;
            Console.Clear();
            Console.Write($"{x}/{y} \n", ConsoleColor.DarkGray);
            if (x == y)
            {
                //OnUploadFinished();
            }
            else
            {
                Console.ReadLine();
            }
        }
        private static void OnUploadFinished()
        {
            Console.Write("Upload finished.\n", ConsoleColor.Green);
            Console.Write(Path.GetFileName(sprx) + "\n", ConsoleColor.Gray);
        }
        private static string GetTimeStr()
        {
            var dt = DateTime.Now;
            return $"{dt.Year}/{dt.Month}/{dt.Day} | {dt.Hour}:{dt.Minute}:{dt.Second}";
        }
        private static string GetTimeStr(DateTime over)
        {
            var dt = over;
            return $"{dt.Year}/{dt.Month}/{dt.Day} | {dt.Hour}:{dt.Minute}:{dt.Second}";
        }
        private static string GetStrW(WatcherChangeTypes type)
        {
            string m(string mm) => GetTimeStr() + mm;
            switch (type)
            {
                case WatcherChangeTypes.Changed: return m(" updated.");
                case WatcherChangeTypes.Created: return m(" created.");
                case WatcherChangeTypes.Renamed: return m(" renamed.");
                case WatcherChangeTypes.Deleted: return m(" deleted.");
                default: return m(string.Empty);
            }
        }
        private static void _lsprx_Changed(object sender, FileSystemEventArgs e)
        {
            Console.Write($"[{e.Name}] {GetStrW(e.ChangeType)}", ConsoleColor.Yellow);
            Console.ReadLine();
        }
    }
}