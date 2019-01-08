using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infappxremoval
{
    class DevconHelper
    {
        private StringBuilder outputlog;
        private ProcessStartInfo startInfo;

        public DevconHelper()
        {
            outputlog = new StringBuilder();
            startInfo = new ProcessStartInfo
            {
                FileName = "devcon.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };
        }

        public Task<string> RemoveDriver(string hwid)
        {
            outputlog.Clear();
            outputlog.Append("devcon.exe remove " + hwid).AppendLine();
            startInfo.Arguments = "Remove " + hwid;
            Process removeDriver = new Process();
            removeDriver.StartInfo = startInfo;

            return Task.Run(() =>
            {
                ExecuteProc(removeDriver);
                return outputlog.ToString();
            });
        }

        public Task<string> Rescan()
        {
            outputlog.Clear();
            outputlog.Append("devcon.exe rescan").AppendLine();
            startInfo.Arguments = "Rescan";
            Process removeDriver = new Process();
            removeDriver.StartInfo = startInfo;

            return Task.Run(() =>
            {
                ExecuteProc(removeDriver);
                return outputlog.ToString();
            });
        }

        private void ExecuteProc(Process process)
        {
            process.Start();
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputlog.Append(e.Data).AppendLine();
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputlog.Append(e.Data).AppendLine();
            }
        }
    }
}
