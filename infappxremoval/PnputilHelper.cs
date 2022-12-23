using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
        private string preLine;
        // oem*.inf
        private static Regex oemInfRgx = new Regex(@"oem\d+\.inf");
        // *.inf
        private static Regex infRgx = new Regex(@"\w+\.inf");
        // xx/xx/xxxx xx.xx driver version
        private static Regex drvVerRgx = new Regex(@"\d+/\d+/\d+\s+\w+");
        private static string extGuid = "{e2f84ce7-8efa-411c-aa69-97454ca4cb57}";
        private static string swcGuid = "{5c4c3332-344d-483c-8739-259e934c9cc8}";

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
                preLine = string.Empty;
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
        public Task<string> DeleteAndUninstallDriverForce(string oemNumber)
        {
            outputlog.Clear();
            outputlog.Append("pnputil.exe /delete-driver " + oemNumber + " /uninstall /force").AppendLine();
            currentAction = PnputilAction.DelAndUninstall;
            startInfo.Arguments = "/delete-driver " + oemNumber + " /uninstall /force";
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
                    //ParsePnputilData(e.Data);
                    ParsePnputilDataMultiLang(e.Data);
                }
                else
                {
                    if (e.Data.Contains("Microsoft PnP")) // skip the first line
                    {
                        return;
                    }
                    outputlog.Append(e.Data).AppendLine();
                }
            }
        }

        private void ParsePnputilDataMultiLang(string data)
        {
            if (!data.Contains(":"))
            {
                return;
            }
            string[] temp = data.Split(new char[] { ':' }, 2);
            temp[1] = temp[1].Trim();
            if (oemInfRgx.IsMatch(temp[1]))
            {
                tempData = new PnputilData();
                infList.Add(tempData);
                tempData.PublishedName = temp[1];
                preLine = "oeminf";
            }
            else if (infRgx.IsMatch(temp[1]))
            {
                tempData.OriginalName = temp[1];
                preLine = "orginfname";
            }
            else if (preLine.Equals("orginfname"))
            {
                tempData.ProviderName = temp[1];
                preLine = "provider name";
            }
            else if (preLine.Equals("provider name"))
            {
                tempData.OrgClassName = temp[1];
                preLine = "original class name";
            }
            else if (preLine.Equals("original class name"))
            {
                PnputilData.InfClass infClass;
                if (temp[1].Equals(extGuid))
                {
                    infClass = PnputilData.InfClass.Extensions;
                }
                else if (temp[1].Equals(swcGuid))
                {
                    infClass = PnputilData.InfClass.SoftwareComponets;
                }
                else
                {
                    infClass = PnputilData.InfClass.Base;
                }
                tempData.ClassName = infClass;
                tempData.ClassGuid = temp[1];
                preLine = "class guid";
            }
            else if (drvVerRgx.IsMatch(temp[1]))
            {
                tempData.DriverVersion = temp[1];
                preLine = "driver version";
            }
            else if (preLine.Equals("driver version"))
            {
                tempData.SignerName = temp[1];
                preLine = "signer name";
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
    }
}
