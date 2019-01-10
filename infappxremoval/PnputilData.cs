
using System.Collections.Generic;

namespace infappxremoval
{
    class PnputilData
    {
        public enum InfClass
        {
            Base,
            Extensions,
            SoftwareComponets
        }
        
        private string publishedName; // oem?.inf
        private string originalName;
        private string providerName;
        private InfClass className;
        private string orgClassName;
        private string driverVersion;
        private string signerName;
        private string friendlyName;
        private string description;
        private string hardwareId;

        public string PublishedName { get => publishedName; set => publishedName = value; }
        public string OriginalName { get => originalName; set => originalName = value; }
        public string ProviderName { get => providerName; set => providerName = value; }
        public InfClass ClassName { get => className; set => className = value; }
        public string OrgClassName { get => orgClassName; set => orgClassName = value; }
        public string DriverVersion { get => driverVersion; set => driverVersion = value; }
        public string SignerName { get => signerName; set => signerName = value; }
        public string FriendlyName { get => friendlyName; set => friendlyName = value; }
        public string Description { get => description; set => description = value; }
        public string HardwareId { get => hardwareId; set => hardwareId = value; }

        public PnputilData()
        {
            publishedName = string.Empty;
            orgClassName = string.Empty;
            providerName = string.Empty;
            className = InfClass.Base;
            orgClassName = string.Empty;
            driverVersion = string.Empty;
            signerName = string.Empty;
            friendlyName = string.Empty;
            description = string.Empty;
            hardwareId = string.Empty;
        }

        public string ToListBoxString()
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

            return s + "\n" +
                publishedName + "   " + originalName + "\n" + driverVersion + "   " + providerName;
        }

        public override string ToString()
        {
            string log = "";

            log += "Publish Name: " + publishedName + "\n";
            log += "Origina Name: " + originalName + "\n";
            log += "Provider Name: " + providerName + "\n";
            log += "Class Name: " + orgClassName + "\n";
            log += "Driver Version: " + driverVersion + "\n";
            log += "Signer Name: " + signerName + "\n";

            return log;
        }
    }
}


//Microsoft PnP Utility

//Published Name:     oem20.inf
//Original Name:      alexahpconfigrtk.inf
//Provider Name:      Realtek
//Class Name:         Extensions
//Class GUID:         {e2f84ce7-8efa-411c-aa69-97454ca4cb57}
//Extension ID:       {7f7901fa-ea73-4a95-bada-55bf89f37009}
//Driver Version:     12/04/2018 1.0.0.8586
//Signer Name:        Microsoft Windows Hardware Compatibility Publisher

//Published Name:     oem47.inf
//Original Name:      amustor.inf
//Provider Name:      Alcorlink Corp.
//Class Name:         Universal Serial Bus controllers
//Class GUID:         { 36fc9e60 - c465 - 11cf - 8056 - 444553540000}
//Driver Version:     04/17/2018 2.0.148.24503
//Signer Name:        Microsoft Windows Hardware Compatibility Publisher