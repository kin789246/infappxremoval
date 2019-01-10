using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infappxremoval
{
    class PsexecHelper
    {
        private StringBuilder outputlog;
        private ProcessStartInfo startInfo;
        private string basePath;

        public PsexecHelper()
        {
            outputlog = new StringBuilder();
            startInfo = new ProcessStartInfo
            {
                FileName = "PsExec.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };
        }

        public Task<string> DeleteRegistryKey(string instanceId)
        {
            outputlog.Clear();
            basePath = "hklm\\system\\currentcontrolset\\enum\\";
            basePath += instanceId;
            string s = "-s -accepteula powershell.exe \"reg delete '" + basePath + "' /f\"";
            outputlog.Append("PsExec.exe " + s).Append("\n");
            startInfo.Arguments = s;
            Process delRegKey = new Process();
            delRegKey.StartInfo = startInfo;

            return Task.Run(() =>
            {
                ExecuteProc(delRegKey);
                return outputlog.ToString();
            });
        }

        public Task<string> RegCommand()
        {
            outputlog.Clear();

            string s = "-s -accpeteula powershell.exe reg";
            outputlog.Append("PsExec.exe " + s).Append("\n");
            startInfo.Arguments = s;
            Process delRegKey = new Process();
            delRegKey.StartInfo = startInfo;

            return Task.Run(() =>
            {
                ExecuteProc(delRegKey);
                return outputlog.ToString();
            });
        }

        private void ExecuteProc(Process process)
        {
            process.Start();
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.Close();
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                //if (e.Data.ToLower().Contains("success") || e.Data.ToLower().Contains("error"))
                //{
                    outputlog.Append(e.Data).AppendLine();
                //}
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.ToLower().Contains("success") || e.Data.ToLower().Contains("error"))
                {
                    outputlog.Append(e.Data).AppendLine();
                }
            }
        }
    }
}
