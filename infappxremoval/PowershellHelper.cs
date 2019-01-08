using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace infappxremoval
{
    class PowershellHelper
    {
        static private Runspace runspace;
        private PowerShell ps;
        private StringBuilder outputlog;
        private Collection<PSObject> pso;

        public PowershellHelper()
        {
            outputlog = new StringBuilder();
            if (runspace == null)
            {
                InitialSessionState iss = InitialSessionState.CreateDefault();
                runspace = RunspaceFactory.CreateRunspace(iss);
                runspace.Open();
            }
        }
        
        public Task<List<string>> GetAppxPackageFullName(string name)
        {
            List<string> packageFullName = new List<string>();
            outputlog.Clear();
            string tempName = "*" + name + "*";
            //string script = "Get-AppxPackage -Name " + tempName;
            ps = PowerShell.Create().AddCommand("Get-AppxPackage").AddParameter("Name", tempName);
            ps.Runspace = runspace;
            return Task.Run(() =>
            {
                ExecutePS(ps);

                foreach (var item in pso)
                {
                    foreach (var member in item.Members)
                    {
                        if (member.Name == "PackageFullName")
                        {
                            packageFullName.Add(member.Value.ToString());
                        }
                    }
                }

                return packageFullName;
            });
        }

        public Task<string> RemoveAppxPackage(string name)
        {
            outputlog.Clear();
            //string script = "Remove-AppxPackage -Package " + name;
            ps = PowerShell.Create().AddCommand("Remove-AppxPackage").AddParameter("Package", name);
            ps.Runspace = runspace;

            return Task.Run(() =>
            {
                ExecutePS(ps);

                if (outputlog.Length == 0)
                {
                    outputlog.Append("Successfully Removed " + name + "\n");
                }

                //foreach (var item in pso)
                //{
                //    foreach (var member in item.Members)
                //    {
                //        if (member.Value != null)
                //        {
                //            outputlog.Append(member.Name + ": " + member.Value.ToString() + "\n");
                //        }
                //        else
                //        {
                //            outputlog.Append(member.Name + ": \n");
                //        }
                //    }
                //}

                return outputlog.ToString();
            });
        }

        public Task<List<string>> GetAppxProvisionedPackageFullName(string name)
        {
            List<string> packageFullName = new List<string>();
            outputlog.Clear();
            string tempName = "*" + name + "*";
            //string script = "Get-AppxProvisionedPackage -Online | where PackageName -Like " + tempName;
            ps = PowerShell.Create().AddCommand("Get-AppxProvisionedPackage").AddParameter("Online")
                .AddCommand("where-object").AddArgument("packagename").AddParameter("like").AddArgument(tempName);
            ps.Runspace = runspace;

            return Task.Run(() =>
            {
                ExecutePS(ps);

                foreach (var item in pso)
                {
                    foreach (var member in item.Members)
                    {
                        if (member.Name == "PackageName")
                        {
                            packageFullName.Add(member.Value.ToString());
                        }
                    }
                }

                return packageFullName;
            });
        }

        public Task<string> RemoveAppxProvisionedPackage(string name)
        {
            outputlog.Clear();
            //string script = "Remove-AppxProvisionedPackage -Online -PackageName " + name;
            ps = PowerShell.Create().AddCommand("Remove-AppxProvisionedPackage").AddParameter("Online")
                .AddParameter("PackageName", name);
            ps.Runspace = runspace;

            return Task.Run(() =>
            {
                ExecutePS(ps);

                if (outputlog.Length == 0)
                {
                    outputlog.Append("Successfully Removed " + name + "\n");
                }

                //foreach (var item in pso)
                //{
                //    foreach (var member in item.Members)
                //    {
                //        if (member.Value != null)
                //        {
                //            outputlog.Append(member.Name + ": " + member.Value.ToString() + "\n");
                //        }
                //        else
                //        {
                //            outputlog.Append(member.Name + ": \n");
                //        }
                //    }
                //}

                return outputlog.ToString();
            });
        }

        // Get-CimInstance win32_pnpsigneddriver | where infname -eq 'oem10.inf'

        public Task<List<Win32PnpSignedDriverData>> GetHwIdofOemInf()
        {
            List<Win32PnpSignedDriverData> wList = new List<Win32PnpSignedDriverData>();
            //string script = "Get-CimInstance win32_pnpsigneddriver | where infname -eq '" + oem + "'";
            //ps = PowerShell.Create().AddCommand("Get-CimInstance").AddArgument("win32_pnpsigneddriver")
            //    .AddCommand("where").AddArgument("infname").AddParameter("like").AddArgument("oem*");
            ps = PowerShell.Create().AddCommand("Get-CimInstance").AddArgument("win32_pnpsigneddriver");
                
            ps.Runspace = runspace;

            return Task.Run(() =>
            {
                ExecutePS(ps);

                foreach (var item in pso)
                {
                    Win32PnpSignedDriverData w32d = new Win32PnpSignedDriverData();
                    wList.Add(w32d);
                    foreach (var member in item.Members)
                    {
                        if (member.Name == "FriendlyName" && member.Value != null)
                        {
                            w32d.FriendlyName = member.Value.ToString();
                        }
                        if (member.Name == "DriverVersion" && member.Value != null)
                        {
                            w32d.DriverVersion = member.Value.ToString();
                        }
                        if (member.Name == "HardWareID" && member.Value != null)
                        {
                            w32d.HardwareId = member.Value.ToString();
                        }
                        if (member.Name == "InfName" && member.Value != null)
                        {
                            w32d.InfName = member.Value.ToString();
                        }
                        if (member.Name == "DeviceName" && member.Value != null)
                        {
                            w32d.DeviceName = member.Value.ToString();
                        }
                    }
                }

                return wList;
            });
         }

        public Task<string> DevconRemove(string name)
        {
            outputlog.Clear();
            string script = ".\\devcon.exe hwid";
            ps = PowerShell.Create().AddScript(script);
            ps.Runspace = runspace;

            return Task.Run(() =>
            {
                ExecutePS(ps);
                
                foreach (var item in pso)
                {
                    foreach (var member in item.Members)
                    {
                        if (member.Value != null)
                        {
                            outputlog.Append(member.Name + ": " + member.Value.ToString());
                        }
                        else
                        {
                            outputlog.Append(member.Name);
                        }
                    }
                }

                return outputlog.ToString();
            });
        }

            private void ExecutePS(PowerShell ps)
        {
            pso = ps.Invoke();
            PSDataCollection<ErrorRecord> pserr = ps.Streams.Error;
            if (pserr != null && pserr.Count > 0)
            {
                foreach (var item in pserr)
                {
                    outputlog.Append(item.ToString()).AppendLine();
                }
            }
        }
    }
}
