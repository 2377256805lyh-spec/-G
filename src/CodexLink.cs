using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace XiaoGDesktop
{
    // 同一个程序以隐藏模式等待桌面 Codex，无窗口、无联网、无需管理员权限。
    internal static class CodexLink
    {
        private static readonly string Executable = Assembly.GetExecutingAssembly().Location;
        private static readonly string Shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "小G · 跟随Codex.lnk");
        private delegate bool EnumCallback(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);

        internal static bool Enabled { get { return File.Exists(Shortcut); } }

        internal static void Enable()
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object link = null;
            try
            {
                link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { Shortcut });
                Type type = link.GetType();
                type.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { Executable });
                type.InvokeMember("Arguments", BindingFlags.SetProperty, null, link, new object[] { "--watch-codex" });
                type.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { Path.GetDirectoryName(Executable) });
                type.InvokeMember("Description", BindingFlags.SetProperty, null, link, new object[] { "打开桌面 Codex 后自动显示小G" });
                type.InvokeMember("WindowStyle", BindingFlags.SetProperty, null, link, new object[] { 7 });
                type.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
            }
            finally
            {
                if (link != null) Marshal.FinalReleaseComObject(link);
                Marshal.FinalReleaseComObject(shell);
            }
            StartWatcher();
        }

        internal static void Disable() { if (File.Exists(Shortcut)) File.Delete(Shortcut); }

        internal static void StartWatcher()
        {
            Process.Start(new ProcessStartInfo(Executable, "--watch-codex") { UseShellExecute = false, CreateNoWindow = true });
        }

        internal static void Watch()
        {
            bool created;
            using (Mutex singleton = new Mutex(true, "Local\\XiaoGDesktop-CodexWatcher", out created))
            {
                if (!created || !Enabled) return;
                Application app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                bool previousVisible = false;
                string previousSession = "";
                DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                Action check = delegate
                {
                    if (!Enabled) { app.Shutdown(); return; }
                    string session;
                    bool visible = FindCodex(out session);
                    if (visible && (!previousVisible || session != previousSession))
                        Process.Start(new ProcessStartInfo(Executable) { UseShellExecute = false });
                    previousVisible = visible;
                    previousSession = session;
                };
                timer.Tick += delegate { check(); };
                timer.Start();
                check();
                app.Run();
            }
        }

        private static bool FindCodex(out string session)
        {
            var candidates = new System.Collections.Generic.Dictionary<uint, string>();
            foreach (string name in new string[] { "ChatGPT", "Codex" })
                foreach (Process process in Process.GetProcessesByName(name))
                    using (process)
                        try
                        {
                            // 只识别桌面包的主程序，终端 codex.exe 和其他同名程序不算。
                            string path = process.MainModule.FileName;
                            if (path.IndexOf("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                path.IndexOf("WindowsApps", StringComparison.OrdinalIgnoreCase) >= 0)
                                candidates[(uint)process.Id] = process.Id + ":" + process.StartTime.ToUniversalTime().Ticks;
                        }
                        catch (System.ComponentModel.Win32Exception) { }
                        catch (InvalidOperationException) { }
            var visible = new System.Collections.Generic.SortedSet<string>(StringComparer.Ordinal);
            EnumWindows(delegate(IntPtr window, IntPtr unused)
            {
                uint pid;
                GetWindowThreadProcessId(window, out pid);
                if (IsWindowVisible(window) && candidates.ContainsKey(pid)) visible.Add(candidates[pid]);
                return true;
            }, IntPtr.Zero);
            session = string.Join(";", visible);
            return visible.Count > 0;
        }
    }
}
