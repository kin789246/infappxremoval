using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace infappxremoval
{
    class DevconHelper
    {
        enum DevconAction
        {
            Init,
            RemoveHwId,
            RemoveInstanceId,
            Rescan,
            FindAll
        }

        private StringBuilder outputlog;
        private ProcessStartInfo startInfo;
        private DevconAction currentAction;
        private List<string> instanceIdList;

        public DevconHelper()
        {
            outputlog = new StringBuilder();
            currentAction = DevconAction.Init;
            instanceIdList = new List<string>();
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

        public Task<string> RemoveHardwareId(string hwid)
        {
            currentAction = DevconAction.RemoveHwId;
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

        public Task<string> RemoveInstanceId(string iId)
        {
            currentAction = DevconAction.RemoveInstanceId;
            outputlog.Clear();
            outputlog.Append("devcon.exe remove @\"" + iId + "\"").AppendLine();
            startInfo.Arguments = "Remove @\"" + iId + "\"";
            Process removeDriver = new Process();
            removeDriver.StartInfo = startInfo;

            return Task.Run(() =>
            {
                ExecuteProc(removeDriver);
                return outputlog.ToString();
            });
        }

        public Task<List<string>> FindAll(string hwid)
        {
            currentAction = DevconAction.FindAll;
            outputlog.Clear();
            outputlog.Append("devcon.exe findall " + hwid).AppendLine();
            startInfo.Arguments = "findall " + hwid;
            Process findAll = new Process();
            findAll.StartInfo = startInfo;

            return Task.Run(() =>
            {
                instanceIdList.Clear();
                ExecuteProc(findAll);
                return instanceIdList;
            });
        }

        public Task<string> Rescan()
        {
            currentAction = DevconAction.Rescan;
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
                if (currentAction == DevconAction.FindAll)
                {
                    if (e.Data.Contains("matching device"))
                    {
                        return;
                    }
                    ParseFindAllMsg(e.Data);

                    //debug
                    //instanceIdList.Add(e.Data + "\n");
                }
                else
                {
                    outputlog.Append(e.Data).AppendLine();
                }
            }
        }

        private void ParseFindAllMsg(string data)
        {
            string[] temp = data.Split(':');
            if (temp.Length == 2)
            {
                instanceIdList.Add(temp[0].Trim());
            }
        }

    }
}


//  C:\Users\RS5\Desktop\infappxremoval_06b_Release>devcon.exe findall "SWC\VEN_10EC&HID_0002"
//  SWD\DRIVERENUM\HPAUDIOCONTROLHSA&5&E830478&0                : HP Audio Hardware Support Application
//  SWD\DRIVERENUM\HPAUDIOCONTROLHSA&5&E830478&1                : HP Audio Hardware Support Application
//  2 matching device(s) found.

//  C:\Users\RS5\Desktop\infappxremoval_06b_Release>devcon.exe findall "INTELAUDIO\FUNC_01&VEN_8086&DEV_280B&SUBSYS_80860101&REV_1000"
//  INTELAUDIO\FUNC_01&VEN_8086&DEV_280B&SUBSYS_80860101&REV_1000\4&3918B3D5&9&0201: Intel(R) Display Audio
//  1 matching device(s) found.

//  C:\Users\RS5\Desktop\infappxremoval_06b_Release>devcon.exe findall "INTELAUDIO\FUNC_01&VEN_8086&DEV_280B&SUBSYS_80860101&R_1000"
//  No matching devices found.