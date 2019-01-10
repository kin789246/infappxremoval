using System.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace infappxremoval
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PowershellHelper psh;
        private PnputilHelper puh;
        private List<PnputilData> installedInfList;
        //private List<string> infToRemove;
        private List<Win32PnpSignedDriverData> hwIdOemInfList;
        private List<PnpDeviceData> pnpDeviceList;
        //get Intel HD audio extension inf
        private string intelHdAudioExtInf = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            InitAll();

            //load version
            VerLabel.Content = "v0.6b by Kin";
        }

        private async void InitAll()
        {
            AppxPackageLB.Items.Clear();
            AppxProvisionedPackageLB.Items.Clear();
            BaseInfLB.Items.Clear();
            ExtInfLB.Items.Clear();
            SwcInfLB.Items.Clear();
            AppxNameTB.Text = "";
            VendorNameTB.Text = "";
            OutputTB.Inlines.Add(AddString("Initial data..."));

            PowershellHelper psh = new PowershellHelper();
            hwIdOemInfList = await psh.GetWin32PnpSignedDriverData();
            pnpDeviceList = new List<PnpDeviceData>();

            OutputTB.Inlines.Add(AddString("Done\n"));
            await LoadInfData();
            ShowInfListItem(installedInfList);

            string s = "Input keyword to search Appx Packages";
            Label lb1 = new Label();
            lb1.Content = s;
            AppxPackageLB.Items.Add(lb1);

            Label lb2 = new Label();
            lb2.Content = s;
            AppxProvisionedPackageLB.Items.Add(lb2);

            //List<string> nameList = new List<string>();
            //nameList.Add("intel");
            //nameList.Add("hpaudio");
            //await GoSearchAppx(nameList);
        }

        private async Task LoadInfData()
        {
            puh = new PnputilHelper();

            OutputTB.Inlines.Add(AddString("Loading all of the installed inf information..."));
            installedInfList = await puh.EnumDrivers();

            GetHwId(installedInfList);

            OutputTB.Inlines.Add(AddString("Done\n"));
            OutputSV.ScrollToEnd();
        }

        private void GetHwId(List<PnputilData> list)
        {
            if (hwIdOemInfList.Count == 0)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                foreach (var item in hwIdOemInfList)
                {
                    if (item.InfName.Equals(list[i].PublishedName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(list[i].FriendlyName))
                        {
                            list[i].FriendlyName += " | ";
                        }
                        if (!string.IsNullOrEmpty(list[i].HardwareId))
                        {
                            list[i].HardwareId += " | ";
                        }
                        if (!string.IsNullOrEmpty(list[i].Description))
                        {
                            list[i].Description += " | ";
                        }

                        list[i].FriendlyName += item.FriendlyName;
                        list[i].HardwareId += item.HardwareId;
                        list[i].Description += item.Descrpition;
                    }
                }
            }
        }

        private void ShowInfListItem(List<PnputilData> datas)
        {
            if (datas.Count == 0)
            {
                return;
            }
            foreach (var pnpdata in datas)
            {
                Grid grid = new Grid();

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.Tag = pnpdata.PublishedName;
                Label lb1 = new Label();
                lb1.Content = pnpdata.ToListBoxString();
                grid.Children.Add(lb1);
                Button btn = new Button();
                Grid.SetColumn(btn, 0);
                Grid.SetColumn(lb1, 1);
                btn.Content = "Uninstall";
                btn.FontSize = 11;
                btn.Height = 30;
                btn.Tag = pnpdata.PublishedName;
                btn.Click += InfUninstBtn_Click;
                grid.Children.Add(btn);

                switch (pnpdata.ClassName)
                {
                    case PnputilData.InfClass.Base:
                        BaseInfLB.Items.Add(grid);
                        break;
                    case PnputilData.InfClass.Extensions:
                        ExtInfLB.Items.Add(grid);
                        break;
                    case PnputilData.InfClass.SoftwareComponets:
                        SwcInfLB.Items.Add(grid);
                        break;
                    default:
                        break;
                }

                //find Intel HD audio extension inf
                if (pnpdata.OriginalName.ToLower().Contains("IntcDAudioExt".ToLower()) 
                    || pnpdata.OriginalName.ToLower().Contains("HdBusExt".ToLower()))
                {
                    intelHdAudioExtInf = pnpdata.PublishedName;
                }
            }
        }

        private List<string> GetHwId(string oem)
        {
            List<string> hwId = new List<string>();
            if (hwIdOemInfList.Count != 0)
            {
                foreach (var item in hwIdOemInfList)
                {
                    if (item.InfName.Equals(oem,StringComparison.OrdinalIgnoreCase))
                    {
                        hwId.Add(item.HardwareId);
                    }
                }
            }
            return hwId;
        }

        private async void InfUninstBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                PnputilHelper helper = new PnputilHelper();
                //DevconHelper dh = new DevconHelper();

                string oem = btn.Tag.ToString();
                OutputTB.Inlines.Add(AddString("wait for uninstalling " + oem + "\n"));
                try
                {
                    //save oem list before remove
                    List<string> savedList = SaveList();

                    PowershellHelper psh = new PowershellHelper();
                    pnpDeviceList = await psh.GetPnpDeviceData();

                    if (oem.Equals(intelHdAudioExtInf, StringComparison.OrdinalIgnoreCase))
                    {
                        await ProcessIntelHdAudioController();
                    }

                    string s = string.Empty;
                    List<string> instanceIds = GetInstanceId(btn.Tag.ToString());
                    PsexecHelper pseh = new PsexecHelper();
                    if (instanceIds.Count != 0)
                    {
                        foreach (var item in instanceIds)
                        {
                            s = await pseh.DeleteRegistryKey(item);
                            OutputTB.Inlines.Add(AddString(s));
                            OutputSV.ScrollToEnd();
                        }
                    }
                    s = await helper.DeleteDriver(oem);
                    OutputTB.Inlines.Add(AddString(s));
                    OutputSV.ScrollToEnd();

                    await LoadInfData();
                    GoSearchInf(savedList);
                }
                catch (Exception exp)
                {
                    OutputTB.Inlines.Add(AddString(exp.Message));
                    OutputSV.ScrollToEnd();
                }
            }
        }

        private List<string> GetInstanceId(string oeminf)
        {
            List<string> list = new List<string>();
            List<string> result = new List<string>();

            foreach (var w32pnp in hwIdOemInfList)
            {
                if (w32pnp.InfName == oeminf)
                {
                    list.Add(w32pnp.Descrpition);
                }
            }

            foreach (var item in list)
            {
                foreach (var pnp in pnpDeviceList)
                {
                    if (pnp.Description.Equals(item, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(pnp.InstanceId);
                        //OutputTB.Inlines.Add(AddString("add " + pnp.InstanceId + "\n"));
                        //OutputSV.ScrollToEnd();
                    }
                }
            }
            
            return result;
        }

        private List<string> SaveList()
        {
            List<string> result = new List<string>();

            result.AddRange(GetGridTag(SwcInfLB.Items));
            result.AddRange(GetGridTag(BaseInfLB.Items));
            result.AddRange(GetGridTag(ExtInfLB.Items));

            return result;
        }

        private List<string> GetGridTag(ItemCollection items)
        {
            List<string> r = new List<string>();

            foreach (var item in items)
            {
                Grid grid = item as Grid;
                if (grid != null)
                {
                    r.Add(grid.Tag.ToString());
                }
            }

            return r;
        }

        private async void AppxSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(AppxNameTB.Text))
            {
                OutputTB.Inlines.Clear();
                await GoSearchAppx(AppxNameTB.Text);
            }
        }

        private async void AppxNameTB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!string.IsNullOrEmpty(AppxNameTB.Text))
                {

                    OutputTB.Inlines.Clear();
                    await GoSearchAppx(AppxNameTB.Text);
                }
            }
        }

        private async Task GoSearchAppx(List<string> namesToSearch)
        {
            List<string> appxLog = new List<string>();
            List<string> appxProvisionedLog = new List<string>();
            AppxPackageLB.Items.Clear();
            AppxProvisionedPackageLB.Items.Clear();

            foreach (var name in namesToSearch)
            {
                psh = new PowershellHelper();

                OutputTB.Inlines.Add(AddString("Loading " + name + " related Appx Package information..."));
                appxLog.AddRange(await psh.GetAppxPackageFullName(name));
                appxProvisionedLog.AddRange(await psh.GetAppxProvisionedPackageFullName(name));
                OutputTB.Inlines.Add(AddString("Done\n"));
                if (appxLog.Count == 0)
                {
                    OutputTB.Inlines.Add(AddString("Can not find " + name + " related AppxPackage.\n"));
                }
                if (appxProvisionedLog.Count == 0)
                {
                    OutputTB.Inlines.Add(AddString("Can not find " + name + " related AppxProvisionedPackage.\n"));
                }

                OutputSV.ScrollToEnd();

            }
            
            ShowAppxListItem(AppxPackageLB, appxLog);
            ShowAppxListItem(AppxProvisionedPackageLB, appxProvisionedLog);
        }

        private async Task GoSearchAppx(string name)
        {
            List<string> appxLog = new List<string>();
            List<string> appxProvisionedLog = new List<string>();

            AppxPackageLB.Items.Clear();
            AppxProvisionedPackageLB.Items.Clear();

            psh = new PowershellHelper();

            OutputTB.Inlines.Add(AddString("Loading " + name + " related Appx Package information..."));
            appxLog.AddRange(await psh.GetAppxPackageFullName(name));
            appxProvisionedLog.AddRange(await psh.GetAppxProvisionedPackageFullName(name));
            OutputTB.Inlines.Add(AddString("Done\n"));
            if (appxLog.Count == 0)
            {
                OutputTB.Inlines.Add(AddString("Can not find " + name + " related AppxPackage.\n"));
            }
            if (appxProvisionedLog.Count == 0)
            {
                OutputTB.Inlines.Add(AddString("Can not find " + name + " related AppxProvisionedPackage.\n"));
            }

            OutputSV.ScrollToEnd();

            ShowAppxListItem(AppxPackageLB, appxLog);
            ShowAppxListItem(AppxProvisionedPackageLB, appxProvisionedLog);
        }

        private void ShowAppxListItem(ListBox lb, List<string> log)
        {
            if (log.Count == 0)
            {
                return;
            }

            foreach (var item in log)
            {
                Grid grid = new Grid();

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                lb.Items.Add(grid);
                Label lb1 = new Label();
                lb1.Content = item;
                grid.Children.Add(lb1);
                Grid.SetColumn(lb1, 1);

                if (!item.Contains("Can not find"))
                {
                    Button btn = new Button();
                    Grid.SetColumn(btn, 0);
                    btn.Content = "Uninstall";
                    btn.FontSize = 11;
                    string s = item;
                    if (lb.Name == "AppxPackageLB")
                    {
                        s += ":appx";
                    }
                    else if (lb.Name == "AppxProvisionedPackageLB")
                    {
                        s += ":appxProvisioned";
                    }
                    btn.Tag = s;
                    btn.Click += AppxUninstBtn_Click;
                    grid.Children.Add(btn);
                }
            }
        }

        private async void AppxUninstBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                try
                {
                    string[] listName = btn.Tag.ToString().Split(new char[] { ':' }, 2);
                    string log = "";
                    PowershellHelper helper = new PowershellHelper();
                    if (listName[1] == "appx")
                    {
                        OutputTB.Inlines.Add(AddString("wait...\nRemove-AppxPackage -Package " + listName[0] + "\n"));
                        log = await helper.RemoveAppxPackage(listName[0]);
                        OutputTB.Inlines.Add(AddString(log));
                        OutputSV.ScrollToEnd();
                    }
                    else if (listName[1] == "appxProvisioned")
                    {
                        OutputTB.Inlines.Add(AddString("wait...\nRemove-AppxProvisionedPackage -Online -PackageName " + listName[0] + "\n"));
                        log = await helper.RemoveAppxProvisionedPackage(listName[0]);
                        OutputTB.Inlines.Add(AddString(log));
                        OutputSV.ScrollToEnd();
                    }

                    if (log.Contains("Successfully Removed"))
                    {
                        btn.IsEnabled = false;
                    }
                }
                catch (Exception exp)
                {
                    OutputTB.Inlines.Add(AddString(exp.Message + "\n", Colors.White, Colors.Red));
                    OutputSV.ScrollToEnd();
                }
            }
        }

        private async void VendorSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(VendorNameTB.Text))
            {
                OutputTB.Inlines.Clear();
                await LoadInfData();
                GoSearchInf(VendorNameTB.Text);
            }
        }

        private async void VendorNameTB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!string.IsNullOrEmpty(VendorNameTB.Text))
                {
                    OutputTB.Inlines.Clear();
                    await LoadInfData();
                    GoSearchInf(VendorNameTB.Text);
                }
            }
        }

        /// <summary>
        /// Search the original name inf
        /// </summary>
        /// <param name="nameList"></param>
        private void GoSearchInf(List<string> nameList)
        {
            BaseInfLB.Items.Clear();
            ExtInfLB.Items.Clear();
            SwcInfLB.Items.Clear();
            List<PnputilData> infList = new List<PnputilData>();
            
            foreach (var name in nameList)
            {
                if (installedInfList.Count == 0)
                {
                    OutputTB.Inlines.Add(AddString("Can not find any inf installed.\n", Colors.White, Colors.Red));
                    return;
                }

                bool found = false;
                Regex rgx = new Regex(@"oem\d*\.inf");
                foreach (var item in installedInfList)
                {
                    if (item.OriginalName.Equals(name, StringComparison.OrdinalIgnoreCase) 
                        || item.PublishedName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!rgx.IsMatch(name))
                        {
                            OutputTB.Inlines.Add(AddString("Find " + name + " installed.\n", Colors.Black, Colors.White));
                        }
                        infList.Add(item);
                        found = true;
                    }
                }
                
                if (!found & !rgx.IsMatch(name))
                {
                    OutputTB.Inlines.Add(AddString("Can not find " + name + " installed.\n"));
                }
            }

            if (infList.Count == 0)
            {
                OutputTB.Inlines.Add(AddString("Can not find any inf from list installed.\n"));
                OutputSV.ScrollToEnd();
                return;
            }

            ShowInfListItem(infList);
        }

        private void GoSearchInf(string text)
        {

            BaseInfLB.Items.Clear();
            ExtInfLB.Items.Clear();
            SwcInfLB.Items.Clear();
            //string nameOrVersion = "";
            //if (VendorRadioBtn.IsChecked == true)
            //{
            //    nameOrVersion = "Vendor ";
            //}
            //else if (VersionRadioBtn.IsChecked == true)
            //{
            //    nameOrVersion = "Version ";
            //}
            OutputTB.Inlines.Add(AddString("Searching " + text + " related inf information..."));
            if (installedInfList.Count == 0)
            {
                OutputTB.Inlines.Add(AddString("\nCan not find " + text + " related inf installed.\n"));
                return;
            }
            List<PnputilData> infList = new List<PnputilData>();
            foreach (var item in installedInfList)
            {
                if (item.ProviderName.ToLower().Contains(text.ToLower()) 
                    || item.DriverVersion.ToLower().Contains(text.ToLower()))
                {
                    infList.Add(item);
                }
            }
            //if (VendorRadioBtn.IsChecked == true)
            //{
            //    foreach (var item in installedInfList)
            //    {
            //        if (item.ProviderName.ToLower().Contains(text.ToLower()))
            //        {
            //            infList.Add(item);
            //        }
            //    }
            //}
            //else if (VersionRadioBtn.IsChecked == true)
            //{
            //    foreach (var item in installedInfList)
            //    {
            //        if (item.DriverVersion.ToLower().Contains(text.ToLower()))
            //        {
            //            infList.Add(item);
            //        }
            //    }
            //}
            if (infList.Count == 0)
            {
                OutputTB.Inlines.Add(AddString("\nCan not find " + text + " related inf installed.\n"));
                return;
            }
            ShowInfListItem(infList);
            OutputTB.Inlines.Add(AddString("Done\n"));
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            InitAll();
        }

        private async void LoadListBtn_Click(object sender, RoutedEventArgs e)
        {
            List<string> infToRemove = new List<string>();
            var filePath = string.Empty;

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;
            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        while (reader.Peek() > -1)
                        {
                            string s = await reader.ReadLineAsync();
                            infToRemove.Add(s.Trim());
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            catch (Exception exp)
            {
                OutputTB.Inlines.Add(AddString(exp.Message, Colors.White, Colors.Red));
                OutputSV.ScrollToEnd();
            }

            if (infToRemove.Count == 0)
            {
                OutputTB.Inlines.Add(AddString("The list is empty!\n", Colors.White, Colors.Red));
                OutputSV.ScrollToEnd();
                return;
            }

            await LoadInfData();
            GoSearchInf(infToRemove);
        }

        private async void UninstallAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to uninstall all inf files?",
                "Question", MessageBoxButton.YesNo, 
                MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                //do no stuff
                OutputTB.Inlines.Add(AddString("Cancelled.\n"));
                OutputSV.ScrollToEnd();
            }
            else
            {
                //do yes stuff
                try
                {
                    OutputTB.Inlines.Add(AddString("Wait for uninstalling all inf listed...\n"));
                    OutputSV.ScrollToEnd();

                    List<string> savedList = SaveList();

                    if (savedList.Count == 0)
                    {
                        OutputTB.Inlines.Add(AddString("No inf file to be uninstalled.\n"));
                        OutputSV.ScrollToEnd();
                        return;
                    }

                    PnputilHelper helper = new PnputilHelper();
                    DevconHelper dh = new DevconHelper();

                    foreach (var item in savedList)
                    {
                        OutputTB.Inlines.Add(AddString("Wait for uninstalling " + item + " ...\n"));

                        //ask if remove Intel High Definition Audio Controller
                        if (item.Equals(intelHdAudioExtInf, StringComparison.OrdinalIgnoreCase))
                        {
                            await ProcessIntelHdAudioController();
                        }
                        string s = string.Empty;

                        PowershellHelper psh = new PowershellHelper();
                        pnpDeviceList = await psh.GetPnpDeviceData();

                        List<string> instanceIds = GetInstanceId(item);
                        PsexecHelper pseh = new PsexecHelper();
                        if (instanceIds.Count != 0)
                        {
                            foreach (var id in instanceIds)
                            {
                                s = await pseh.DeleteRegistryKey(id);
                                OutputTB.Inlines.Add(AddString(s));
                                OutputSV.ScrollToEnd();
                            }
                        }
                        s = await helper.DeleteDriver(item);
                        OutputTB.Inlines.Add(AddString(s));
                        OutputSV.ScrollToEnd();
                    }

                    await LoadInfData();
                    GoSearchInf(savedList);
                }
                catch (Exception exp)
                {

                    OutputTB.Inlines.Add(AddString(exp.Message));
                    OutputSV.ScrollToEnd();
                }
            }
        }

        private async Task ProcessIntelHdAudioController()
        {
            if (MessageBox.Show("Intel HD Audio Extension INF may be used by High Definition Audio Controller"
                + " on platforms support SGPC such as KBL, WHL... etc.\n"
                + " Do you want to uninstall High Definition Audio Controller?",
                "Question", MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                //do no stuff
                return;
            }
            else
            {
                //do yes stuff
                List<string> hwId = new List<string>();
                if (hwIdOemInfList.Count != 0)
                {
                    foreach (var item in hwIdOemInfList)
                    {
                        if (item.Descrpition.ToLower().Contains("Audio Controller".ToLower()) 
                            && item.HardwareId.ToLower().Contains("PCI\\VEN_8086".ToLower()))
                        {
                            //DevconHelper dh = new DevconHelper();
                            //string s = await dh.RemoveDriver(item.HardwareId);
                            PsexecHelper psexec = new PsexecHelper();
                            foreach (var pnp in pnpDeviceList)
                            {
                                if (pnp.Description.Equals(item.Descrpition, StringComparison.OrdinalIgnoreCase))
                                {
                                    string s = await psexec.DeleteRegistryKey(pnp.InstanceId);
                                    OutputTB.Inlines.Add(AddString(s));
                                    OutputSV.ScrollToEnd();
                                }
                            }
                        }
                    }
                }
            }
        }

            private void GetOemNumberFromListBox(List<string> list, ListBox InfLB)
        {
            if (InfLB.Items.Count == 0)
            {
                return;
            }

            foreach (var item in InfLB.Items)
            {
                Grid grid = item as Grid;
                if (grid != null)
                {
                    list.Add(grid.Tag.ToString());
                }
            }
        }

        private async void DebugBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var watch = Stopwatch.StartNew();

                string s = "";

                PsexecHelper psexec = new PsexecHelper();
                s = await psexec.RegCommand();

                watch.Stop();
                OutputTB.Inlines.Add(AddString(string.Format("function takes {0} ms.\n", watch.ElapsedMilliseconds)));
                OutputTB.Inlines.Add(AddString(s));
                OutputSV.ScrollToEnd();
            }
            catch (Exception exp)
            {
                OutputTB.Inlines.Add(AddString(exp.Source.ToString() + exp.Message + "\n", Colors.White, Colors.Red));
                OutputSV.ScrollToEnd();
            }
        }

        private Run AddString(string text, Color foreColor, Color bgColor)
        {
            Run run = new Run();

            run.Text = text;
            run.Foreground = new SolidColorBrush(foreColor);
            run.Background = new SolidColorBrush(bgColor);

            return run;
        }

        private Run AddString(string text)
        {
            Run run = new Run();

            run.Text = text;
            run.Foreground = new SolidColorBrush(Colors.Black);
            run.Background = new SolidColorBrush(Colors.White);

            return run;
        }
    }
}