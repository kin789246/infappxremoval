using System.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Linq;

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
        private string logFileName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private List<Win32PnpSignedDriverData> hwIdOemInfList;
        //get Intel HD audio extension inf
        private string intelHdAudioExtInf = string.Empty;
        //get HD Audio Controller HardwareId
        private string hdAudioControllerId = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            
            var time = DateTime.Now;
            logFileName += time.ToLocalTime().ToString("_yyyyMMdd-HHmmss") + ".log";
            string logStart = "######### Start log at " + time.ToLocalTime() + " #########\n\n";
            WriteLog(logFileName, logStart);

            InitAll();

            //load version
            VerLabel.Content = "v1.0 by Kin|Jiaching";
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
            //DisButtons();
            WholeGrid.IsEnabled = false;
            OutputTB.Inlines.Add(AddString("Initial data..."));

            PowershellHelper psh = new PowershellHelper();
            hwIdOemInfList = await psh.GetWin32PnpSignedDriverData();

            OutputTB.Inlines.Add(AddString("Done\n"));
            
            await LoadInfData();
            //ShowInfListItem(installedInfList);
            
            string s = "Input keyword to search Appx Packages";
            Label lb1 = new Label();
            lb1.Content = s;
            AppxPackageLB.Items.Add(lb1);

            Label lb2 = new Label();
            lb2.Content = s;
            AppxProvisionedPackageLB.Items.Add(lb2);

            //EnButtons();
            WholeGrid.IsEnabled = true;
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
            //ProcessInfListOrder(installedInfList);

            OutputTB.Inlines.Add(AddString("Done\n"));
            OutputSV.ScrollToEnd();
        }

        private void ProcessInfListOrder(List<PnputilData> list)
        {
            try
            {
                Dictionary<int, List<PnputilData>> arrange = new Dictionary<int, List<PnputilData>>();
                for (int it=0; it<4; it++)
                {
                    arrange[it] = new List<PnputilData>();
                }

                int i = 0;
                while(i < list.Count)
                {
                    if (list[i].Descriptions.Count > 0)
                    {
                        //Intel Graphics
                        if (list[i].Descriptions[0].ToLower().Contains("graphics") 
                            && list[i].HardwareIds[0].ToLower().Contains("pci\\ven_8086"))
                        {
                            arrange[0].Add(list[i]);
                            list.RemoveAt(i);
                            continue;
                        }
                        //ISST
                        if (list[i].Descriptions[0].ToLower().Contains("smart sound technology")
                            && list[i].HardwareIds[0].ToLower().Contains("intelaudio\\ctlr"))
                        {
                            arrange[1].Add(list[i]);
                            list.RemoveAt(i);
                            continue;
                        }
                        //ISST OED
                        if (list[i].Descriptions[0].ToLower().Contains("smart sound technology")
                            && list[i].HardwareIds[0].ToLower().Contains("intelaudio\\dsp_ctlr"))
                        {
                            arrange[2].Add(list[i]);
                            list.RemoveAt(i);
                            continue;
                        }
                        //ISST Audio Controller or BUS
                        if (list[i].Descriptions[0].ToLower().Contains("smart sound technology")
                            && list[i].HardwareIds[0].ToLower().Contains("pci\\ven_8086"))
                        {
                            arrange[3].Add(list[i]);
                            list.RemoveAt(i);
                            continue;
                        }
                    }
                    i++;
                }

                for (int it = 0; it < 4; it++)
                {
                    if (arrange.ContainsKey(it))
                    {
                        if (arrange[it].Count != 0)
                        {
                            foreach (var item in arrange[it])
                            {
                                list.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                OutputTB.Inlines.Add(AddString(exp.Message + "\n"));
                OutputSV.ScrollToEnd();
            }
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
                        if (!string.IsNullOrEmpty(item.FriendlyName))
                        {
                            list[i].FriendlyNames.Add(item.FriendlyName);
                        }
                        if (!string.IsNullOrEmpty(item.HardwareId))
                        {
                            list[i].HardwareIds.Add(item.HardwareId);
                        }
                        if (!string.IsNullOrEmpty(item.Description))
                        {
                            list[i].Descriptions.Add(item.Description);
                        }
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

            ProcessInfListOrder(datas);

            intelHdAudioExtInf = string.Empty;
            hdAudioControllerId = string.Empty;

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

                WriteInstalledInfToLog(pnpdata, datas.IndexOf(pnpdata));

                //find Intel HD audio extension inf
                if (pnpdata.OriginalName.ToLower().Contains("IntcDAudioExt".ToLower()) 
                    || pnpdata.OriginalName.ToLower().Contains("HdBusExt".ToLower()))
                {
                    intelHdAudioExtInf = pnpdata.PublishedName;
                }

                //find ISST audio controller inf
                foreach (var des in pnpdata.Descriptions)
                {
                    if (des.ToLower().Contains("audio controller") && pnpdata.HardwareIds.Count > 0)
                    {
                        hdAudioControllerId = pnpdata.HardwareIds[0];
                        break;
                    }
                }
            }
        }

        private void WriteInstalledInfToLog(PnputilData data, int idx)
        {
            idx++;
            string s = idx.ToString() + "\n" + data.ToListBoxString() + "\n";
            WriteLog(logFileName, s);
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
                string oem = btn.Tag.ToString();

                //DisButtons();
                WholeGrid.IsEnabled = false;
                //save oem list before remove
                List<PnputilData> savedList = SaveListToDataList();
                
                PnputilData toRemove = null;
                foreach (var item in savedList)
                {
                    if (oem.Equals(item.PublishedName, StringComparison.OrdinalIgnoreCase))
                    {
                        toRemove = item;
                        break;
                    }
                }

                if (toRemove != null)
                {
                    OutputTB.Inlines.Add(AddString("wait for uninstalling " + toRemove.OriginalName + "\n"));
                    await ProcessUninstallation(helper, oem);
                }
                
                await LoadInfData();
                GoSearchInf(savedList);

                //EnButtons();
                WholeGrid.IsEnabled = true;
                ShowUninstallationCompleted();
            }
        }

        private async Task ProcessUninstallation(PnputilHelper helper, string oem)
        {
            string s;
            s = await helper.DeleteAndUninstallDriverForce(oem);
            OutputTB.Inlines.Add(AddString(s));
            OutputSV.ScrollToEnd();
        }
        private void ShowHdAudioInfo()
        {
            //string message = "Intel HD Audio Extension INF may be used by High Definition Audio Controller"
            //                  + " on platforms support SGPC such as KBL, WHL... etc.\n"
            //                  + "Please uninstall High Definition Audio Controller in Device Manager first.";

            string message = "Please uninstall High Definition Audio Controller in Device Manager, then uninstall again.\n"
                + "請手動移除High Definition Audio Controller再移除一次";
            string caption = "Information";
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowUninstallationCompleted()
        {
            string message = "Uninstallation Completed.\n";
            string caption = "Information";
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private List<string> SaveList()
        {
            List<string> result = new List<string>();

            result.AddRange(GetGridTag(SwcInfLB.Items));
            result.AddRange(GetGridTag(BaseInfLB.Items));
            result.AddRange(GetGridTag(ExtInfLB.Items));

            return result;
        }

        private List<PnputilData> SaveListToDataList()
        {
            List<string> oemList = new List<string>();
            List<PnputilData> result = new List<PnputilData>();

            oemList.AddRange(GetGridTag(SwcInfLB.Items));
            oemList.AddRange(GetGridTag(BaseInfLB.Items));
            oemList.AddRange(GetGridTag(ExtInfLB.Items));

            foreach (var oem in oemList)
            {
                foreach (var data in installedInfList)
                {
                    if (oem.Equals(data.PublishedName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(data);
                        break;
                    }
                }
            }

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
                //DisButtons();
                WholeGrid.IsEnabled = false;
                await GoSearchAppx(AppxNameTB.Text, "both");
                //EnButtons();
                WholeGrid.IsEnabled = true;
            }
        }

        private async void AppxNameTB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!string.IsNullOrEmpty(AppxNameTB.Text))
                {

                    OutputTB.Inlines.Clear();
                    //DisButtons();
                    WholeGrid.IsEnabled = false;
                    await GoSearchAppx(AppxNameTB.Text, "both");
                    WholeGrid.IsEnabled = true;
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

        private async Task GoSearchAppx(string name, string lb)
        {
            List<string> appxLog = new List<string>();
            List<string> appxProvisionedLog = new List<string>();

            psh = new PowershellHelper();
            OutputTB.Inlines.Add(AddString("Loading " + name + " related Appx Package information..."));

            if (lb.Equals("both"))
            {
                AppxPackageLB.Items.Clear();
                AppxProvisionedPackageLB.Items.Clear();
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
                ShowAppxListItem(AppxPackageLB, appxLog);
                ShowAppxListItem(AppxProvisionedPackageLB, appxProvisionedLog);
            }
            else if (lb.Equals("appx"))
            {
                AppxPackageLB.Items.Clear();
                appxLog.AddRange(await psh.GetAppxPackageFullName(name));
                OutputTB.Inlines.Add(AddString("Done\n"));
                if (appxLog.Count == 0)
                {
                    OutputTB.Inlines.Add(AddString("Can not find " + name + " related AppxPackage.\n"));
                }
                ShowAppxListItem(AppxPackageLB, appxLog);
            }
            else if (lb.Equals("appxProvisioned"))
            {
                AppxProvisionedPackageLB.Items.Clear();
                appxProvisionedLog.AddRange(await psh.GetAppxProvisionedPackageFullName(name));
                OutputTB.Inlines.Add(AddString("Done\n"));
                if (appxProvisionedLog.Count == 0)
                {
                    OutputTB.Inlines.Add(AddString("Can not find " + name + " related AppxProvisionedPackage.\n"));
                }
                ShowAppxListItem(AppxProvisionedPackageLB, appxProvisionedLog);
            }

            OutputSV.ScrollToEnd();
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
                TextBlock tb = new TextBlock();
                tb.Margin = new Thickness(3, 0, 3, 0);
                tb.Text = item;
                grid.Children.Add(tb);
                Grid.SetColumn(tb, 1);

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
                    grid.Tag = s;
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
                await UninstallAppx(btn.Tag.ToString());
                ShowUninstallationCompleted();
            }
        }

        private async void VendorSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(VendorNameTB.Text))
            {
                OutputTB.Inlines.Clear();
                //DisButtons();
                WholeGrid.IsEnabled = false;
                await LoadInfData();
                GoSearchInf(VendorNameTB.Text);
                //EnButtons();
                WholeGrid.IsEnabled = true;
            }
        }

        private async void VendorNameTB_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!string.IsNullOrEmpty(VendorNameTB.Text))
                {
                    OutputTB.Inlines.Clear();
                    //DisButtons();
                    WholeGrid.IsEnabled = false;
                    await LoadInfData();
                    GoSearchInf(VendorNameTB.Text);
                    //EnButtons();
                    WholeGrid.IsEnabled = true;
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
            
            foreach (var name in nameList.Distinct())
            {
                if (installedInfList.Count == 0)
                {
                    OutputTB.Inlines.Add(AddString("Can not find any inf installed.\n", Colors.White, Colors.Red));
                    return;
                }

                //bool found = false;
                //Regex rgx = new Regex(@"oem\d*\.inf");
                foreach (var item in installedInfList)
                {
                    if (item.OriginalName.Equals(name, StringComparison.OrdinalIgnoreCase) 
                        || item.PublishedName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        //if (!rgx.IsMatch(name))
                        //{
                        //    OutputTB.Inlines.Add(AddString("Find " + name + " installed.\n", Colors.Black, Colors.White));
                        //}
                        infList.Add(item);
                        //found = true;
                    }
                }
                
                //if (!found & !rgx.IsMatch(name))
                //{
                //    OutputTB.Inlines.Add(AddString("Can not find " + name + " installed.\n"));
                //}
            }

            if (infList.Count == 0)
            {
                OutputTB.Inlines.Add(AddString("Can not find any inf from list installed.\n"));
                OutputSV.ScrollToEnd();
                return;
            }

            ShowInfListItem(infList);
        }

        /// <summary>
        /// Search the original name inf
        /// </summary>
        /// <param name="nameList"></param>
        private void GoSearchInf(List<PnputilData> nameList)
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

                //bool found = false;
                foreach (var item in installedInfList)
                {
                    if (item.OriginalName.Equals(name.OriginalName, StringComparison.OrdinalIgnoreCase))
                    {
                        //OutputTB.Inlines.Add(AddString("Find " + name.OriginalName + " installed.\n", Colors.Black, Colors.White));
                        //OutputSV.ScrollToEnd();
                        infList.Add(item);
                        //found = true;
                        break;
                    }
                }

                //if (found == false)
                //{
                //    OutputTB.Inlines.Add(AddString("Can not find " + name.OriginalName + " installed.\n"));
                //}
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
                    || item.DriverVersion.ToLower().Contains(text.ToLower())
                    || item.OriginalName.ToLower().Contains(text.ToLower())
                    || item.PublishedName.ToLower().Contains(text.ToLower()))
                {
                    infList.Add(item);
                }
            }

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

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

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

            //DisButtons();
            WholeGrid.IsEnabled = false;
            await LoadInfData();
            //EnButtons();
            WholeGrid.IsEnabled = true;
            OutputTB.Inlines.Add(AddString("Done\n"));
            OutputSV.ScrollToEnd();
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
                OutputTB.Inlines.Add(AddString("Wait for uninstalling all inf listed...\n"));
                OutputSV.ScrollToEnd();

                //DisButtons();
                WholeGrid.IsEnabled = false;
                //save before removing
                List<PnputilData> savedList = SaveListToDataList();
                PnputilHelper helper = new PnputilHelper();

                foreach (var item in savedList)
                {
                    await ProcessUninstallation(helper, item.PublishedName);
                }

                await LoadInfData();
                GoSearchInf(savedList);

                //remove Appx
                List<string> appxNames = new List<string>();
                appxNames.AddRange(GetGridTag(AppxPackageLB.Items));
                appxNames.AddRange(GetGridTag(AppxProvisionedPackageLB.Items));
                foreach (var name in appxNames)
                {
                    await UninstallAppx(name);
                }
                
                //EnButtons();
                WholeGrid.IsEnabled = true;
                ShowUninstallationCompleted();
            }
        }

        private async Task UninstallAppx(string appxName)
        {
            try
            {
                string[] listName = appxName.Split(new char[] { ':' }, 2);
                string log = "";
                string lb = listName[1];
                PowershellHelper helper = new PowershellHelper();
                if (listName[1] == "appx")
                {
                    OutputTB.Inlines.Add(AddString("wait...\nRemove-AppxPackage -Package " + listName[0] + "\n"));
                    WholeGrid.IsEnabled = false;
                    log = await helper.RemoveAppxPackage(listName[0]);
                    OutputTB.Inlines.Add(AddString(log));
                    OutputSV.ScrollToEnd();
                    WholeGrid.IsEnabled = true;
                }
                else if (listName[1] == "appxProvisioned")
                {
                    OutputTB.Inlines.Add(AddString("wait...\nRemove-AppxProvisionedPackage -Online -PackageName " + listName[0] + "\n"));
                    WholeGrid.IsEnabled = false;
                    log = await helper.RemoveAppxProvisionedPackage(listName[0]);
                    OutputTB.Inlines.Add(AddString(log));
                    OutputSV.ScrollToEnd();
                    WholeGrid.IsEnabled = true;
                }
                
                await GoSearchAppx(listName[0], lb);
            }
            catch (Exception exp)
            {
                OutputTB.Inlines.Add(AddString(exp.Message + "\n", Colors.White, Colors.Red));
                OutputSV.ScrollToEnd();
            }
        }

        private async Task ProcessIntelHdAudioController()
        {
            string message = "Intel HD Audio Extension INF may be used by High Definition Audio Controller"
                + " on platforms support SGPC such as KBL, WHL... etc.\n"
                + "Do you want to uninstall High Definition Audio Controller?";

            if (MessageBox.Show( message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
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
                        if (item.Description.ToLower().Contains("Audio Controller".ToLower()) 
                            && item.HardwareId.ToLower().Contains("PCI\\VEN_8086".ToLower()))
                        {
                            DevconHelper dh = new DevconHelper();
                            string s = await dh.RemoveHardwareId(item.HardwareId);
                            OutputTB.Inlines.Add(AddString(s));
                            OutputSV.ScrollToEnd();
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
                puh = new PnputilHelper();

                OutputTB.Inlines.Add(AddString("Loading all of the installed inf information..."));
                installedInfList = await puh.EnumDrivers();

                //GetHwId(installedInfList);

                OutputTB.Inlines.Add(AddString("Done\n"));
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

            // write to log file
            WriteLog(logFileName, text);

            return run;
        }

        private Run AddString(string text)
        {
            Run run = new Run();

            run.Text = text;
            run.Foreground = new SolidColorBrush(Colors.Black);
            run.Background = new SolidColorBrush(Colors.White);

            // write to log file
            WriteLog(logFileName, text);

            return run;
        }

        private void DisButtons()
        {
            AppxSearchBtn.IsEnabled = false;
            VendorSearchBtn.IsEnabled = false;
            RefreshBtn.IsEnabled = false;
            LoadListBtn.IsEnabled = false;
            UninstallAllBtn.IsEnabled = false;
            LoadInfBtn.IsEnabled = false;
        }

        private void EnButtons()
        {
            AppxSearchBtn.IsEnabled = true;
            VendorSearchBtn.IsEnabled = true;
            RefreshBtn.IsEnabled = true;
            LoadListBtn.IsEnabled = true;
            UninstallAllBtn.IsEnabled = true;
            LoadInfBtn.IsEnabled = true;
        }

        private async void LoadInfBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = false;
            //folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;

            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();

            //</ Dialog >
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //----< Selected Folder >----
                //load inf list
                WholeGrid.IsEnabled = false;
                OutputTB.Inlines.Add(AddString("Search inf files in " + folderDialog.SelectedPath + "\n"));
                OutputSV.ScrollToEnd();

                var infNames = Directory.GetFiles(folderDialog.SelectedPath, "*.inf", SearchOption.AllDirectories);
                List<string> infs = new List<string>();
                foreach (var name in infNames)
                {
                    //    OutputTB.Inlines.Add(AddString(System.IO.Path.GetFileName(name) + "\n"));
                    //    OutputSV.ScrollToEnd();
                    infs.Add(System.IO.Path.GetFileName(name));
                }
                GoSearchInf(infs);

                //load appx names from AUMIDs.txt
                var aumidss = Directory.GetFiles(folderDialog.SelectedPath, "AUMIDs.txt", SearchOption.AllDirectories);
                if (aumidss.Length != 0)
                {
                    List<string> appxNameList = new List<string>();
                    foreach (var path in aumidss)
                    {
                        await ParseAppxAumid(path, appxNameList);
                    }
                    await GoSearchAppx(appxNameList.Distinct().ToList());
                }

                OutputTB.Inlines.Add(AddString("Done\n"));
                OutputSV.ScrollToEnd();
                WholeGrid.IsEnabled = true;
            }
        }

        private async Task ParseAppxAumid(string path, List<string> nameList)
        {
            try
            {
                string rgx = @"(\.\w+_)";
                using (StreamReader sr = new StreamReader(path))
                {
                    while (sr.Peek() > -1)
                    {
                        string line = await sr.ReadLineAsync();
                        string[] substrings = Regex.Split(line, rgx);
                        foreach (var s in substrings)
                        {
                            if (Regex.IsMatch(s,rgx))
                            {
                                nameList.Add(s.Substring(1, s.Length - 2));
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                OutputTB.Inlines.Add(AddString(exp.Message, Colors.White, Colors.Red));
                OutputSV.ScrollToEnd();
            }
        }
        private void WriteLog(string logFileName, string text)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFileName, true))
            {
                file.WriteLine(text);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            string logEnd = "\n\n######### Log End #########\n\n";
            WriteLog(logFileName, logEnd);
        }
    }
}