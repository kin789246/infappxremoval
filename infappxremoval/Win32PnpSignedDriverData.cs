namespace infappxremoval
{
    class Win32PnpSignedDriverData
    {
        private string friendlyName;
        private string description;
        private string driverVersion;
        private string infName;
        private string hardwareId;

        public string FriendlyName { get => friendlyName; set => friendlyName = value; }
        public string Description { get => description; set => description = value; }
        public string DriverVersion { get => driverVersion; set => driverVersion = value; }
        public string InfName { get => infName; set => infName = value; }
        public string HardwareId { get => hardwareId; set => hardwareId = value; }

        public Win32PnpSignedDriverData()
        {
            friendlyName = string.Empty;
            description = string.Empty;
            driverVersion = string.Empty;
            InfName = string.Empty;
            hardwareId = string.Empty;
        }

        public string PrintProperty()
        {
            string s = string.Empty;
            if (string.IsNullOrEmpty(friendlyName))
            {
                s = description;
            }
            else
            {
                s = friendlyName;
            }
            return "Description: " + s + "\n"
                + "DriverVersion: " + driverVersion + "\n"
                + "InfName: " + infName + "\n"
                + "HardwareId: " + hardwareId + "\n";
        }
    }
}
//Caption                 : 
//Description             : Intel(R) Display Audio
//InstallDate             : 
//Name                    : 
//Status                  : 
//CreationClassName       : 
//Started                 : 
//StartMode               : 
//SystemCreationClassName : 
//SystemName              : 
//ClassGuid               : {4d36e96c-e325-11ce-bfc1-08002be10318}
//CompatID                : HDAUDIO\FUNC_01&CTLR_VEN_8086&CTLR_DEV_3198&VEN_8086&DEV_280D&REV_1000
//DeviceClass             : MEDIA
//DeviceID                : HDAUDIO\FUNC_01&VEN_8086&DEV_280D&SUBSYS_80860101&REV_1000\4&2A043597&0&0201
//DeviceName              : Intel(R) Display Audio
//DevLoader               : 
//DriverDate              : 9/3/2018 5:00:00 PM
//DriverName              : IntcDAud.sys
//DriverProviderName      : Intel(R) Corporation
//DriverVersion           : 10.26.0.1
//FriendlyName            : 
//HardWareID              : HDAUDIO\FUNC_01&VEN_8086&DEV_280D&SUBSYS_80860101&REV_1000
//InfName                 : oem36.inf
//IsSigned                : True
//Location                : Internal High Definition Audio Bus
//Manufacturer            : Intel(R) Corporation
//PDO                     : \Device\0000003d
//Signer                  : Microsoft Windows Hardware Compatibility Publisher
//PSComputerName          : 