using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infappxremoval
{
    enum DeviceStatus
    {
        OK,
        Unknown
    }

    class PnpDeviceData
    {
        private string description;
        private string instanceId;
        private DeviceStatus status;


        public string Description { get => description; set => description = value; }
        public string InstanceId { get => instanceId; set => instanceId = value; }
        internal DeviceStatus Status { get => status; set => status = value; }

        public PnpDeviceData()
        {
            description = string.Empty;
            instanceId = string.Empty;
            status = DeviceStatus.OK;
        }

        public string PrintProperty()
        {
            string s = "";
            if (status == DeviceStatus.OK)
            {
                s = "Status: OK";
            }
            else
            {
                s = "Status: Unknown";
            }

            return "Description: " + description + "\n" + "InstanceId: " + instanceId + "\n" + s + "\n";
        }
    }
}

//Class                       : Monitor
//FriendlyName                : Generic Non-PnP Monitor
//InstanceId                  : DISPLAY\DEFAULT_MONITOR\1&8713BCA&0&UID0
//Problem                     : CM_PROB_PHANTOM
//ConfigManagerErrorCode      : CM_PROB_PHANTOM
//ProblemDescription          :
//Caption                     : Generic Non-PnP Monitor
//Description                 : Generic Non-PnP Monitor
//InstallDate                 :
//Name                        : Generic Non-PnP Monitor
//Status                      : Unknown
//Availability                :
//ConfigManagerUserConfig     : False
//CreationClassName           : Win32_PnPEntity
//DeviceID                    : DISPLAY\DEFAULT_MONITOR\1&8713BCA&0&UID0
//ErrorCleared                :
//ErrorDescription            :
//LastErrorCode               :
//PNPDeviceID                 : DISPLAY\DEFAULT_MONITOR\1&8713BCA&0&UID0
//PowerManagementCapabilities :
//PowerManagementSupported    :
//StatusInfo                  :
//SystemCreationClassName     : Win32_ComputerSystem
//SystemName                  : DESKTOP-IF8I24S
//ClassGuid                   : {4d36e96e-e325-11ce-bfc1-08002be10318}
//CompatibleID                : {* PNP09FF}
//HardwareID                  : {MONITOR\Default_Monitor}
//Manufacturer                : (Standard monitor types)
//PNPClass                    : Monitor
//Present                     : False
//Service                     : monitor
//PSComputerName              :
//CimClass                    : ROOT/cimv2:Win32_PnPEntity
//CimInstanceProperties       : {Caption, Description, InstallDate, Name...}
//CimSystemProperties         : Microsoft.Management.Infrastructure.CimSystemProperties
