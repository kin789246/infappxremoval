using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace infappxremoval
{
    class PnputilHelper
    {
        enum PnputilAction
        {
            Init,
            EnumDrv,
            DelDrv,
            DelAndUninstall
        }

        private StringBuilder outputlog;
        private ProcessStartInfo startInfo;
        private List<PnputilData> infList;
        private PnputilData tempData;
        private PnputilAction currentAction;

        public PnputilHelper()
        {
            currentAction = PnputilAction.Init;
            outputlog = new StringBuilder();
            infList = new List<PnputilData>();
            startInfo = new ProcessStartInfo
            {
                FileName = "pnputil.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };
        }

        public Task<List<PnputilData>> EnumDrivers()
        {
            outputlog.Clear();
            infList.Clear();
            currentAction = PnputilAction.EnumDrv;
            startInfo.Arguments = "/enum-drivers";
            Process enumDrivers = new Process();
            enumDrivers.StartInfo = startInfo;

            return Task.Run(() =>
            {
                ExecuteProc(enumDrivers);
                return infList;
            });
        }

        public Task<string> DeleteDriver(string oemNumber)
        {
            outputlog.Clear();
            outputlog.Append("pnputil.exe /delete-driver " + oemNumber).AppendLine();
            currentAction = PnputilAction.DelDrv;
            startInfo.Arguments = "/delete-driver " + oemNumber;
            Process deleteDriver = new Process();
            deleteDriver.StartInfo = startInfo;

            return Task.Run(() => 
            {
                ExecuteProc(deleteDriver);
                return outputlog.ToString();
            });
        }

        public Task<string> DeleteDriver(List<string> oemNumbers)
        {
            outputlog.Clear();
            currentAction = PnputilAction.DelDrv;

            return Task.Run(() =>
            {
                foreach (var oemNumber in oemNumbers)
                {
                    startInfo.Arguments = "/delete-driver " + oemNumber;
                    Process deleteDriver = new Process();
                    deleteDriver.StartInfo = startInfo;

                    ExecuteProc(deleteDriver);
                }

                return outputlog.ToString();
            });
        }

        public Task<string> DeleteAndUninstallDriver(string oemNumber)
        {
            outputlog.Clear();
            outputlog.Append("pnputil.exe /delete-driver " + oemNumber + " /uninstall").AppendLine();
            currentAction = PnputilAction.DelAndUninstall;
            startInfo.Arguments = "/delete-driver " + oemNumber + " /uninstall";
            Process deleteDriver = new Process();
            deleteDriver.StartInfo = startInfo;

            return Task.Run(() =>
            {
                ExecuteProc(deleteDriver);
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
                if (currentAction == PnputilAction.EnumDrv)
                {
                    if (e.Data.Contains("Microsoft PnP Utility")) // skip the first line
                    {
                        return;
                    }
                    ParsePnputilData(e.Data);
                }
                else
                {
                    if (e.Data.Contains("Microsoft PnP Utility")) // skip the first line
                    {
                        return;
                    }
                    outputlog.Append(e.Data).AppendLine();
                }
            }
        }

        private void ParsePnputilData(string data)
        {
            string[] temp = data.Split(new char[] { ':' }, 2);
            temp[1] = temp[1].Trim();
            if (temp[0].Contains("Published Name")) // oem?.inf
            {
                tempData = new PnputilData();
                infList.Add(tempData);
                tempData.PublishedName = temp[1];
            }
            else if (temp[0].Contains("Original Name"))
            {
                tempData.OriginalName = temp[1];
            }
            else if (temp[0].Contains("Provider Name"))
            {
                tempData.ProviderName = temp[1];
            }
            else if (temp[0].Contains("Class Name"))
            {
                PnputilData.InfClass infClass;
                switch (temp[1])
                {
                    case "Extensions":
                        infClass = PnputilData.InfClass.Extensions;
                        break;
                    case "Software components":
                        infClass = PnputilData.InfClass.SoftwareComponets;
                        break;
                    default:
                        infClass = PnputilData.InfClass.Base;
                        break;
                }
                tempData.ClassName = infClass;
                tempData.OrgClassName = temp[1];
            }
            else if (temp[0].Contains("Driver Version"))
            {
                tempData.DriverVersion = temp[1];
            }
            else if (temp[0].Contains("Signer Name"))
            {
                tempData.SignerName = temp[1];
            }
        }

        //private List<Win32PnpSignedDriverData> GetHwId()
        //{
        //    List<Win32PnpSignedDriverData> list = new List<Win32PnpSignedDriverData>();

        //    PowershellHelper ps = new PowershellHelper();
        //    list = ps.GetHwIdofOemInf();

        //    return list;
        //}
    }
}
