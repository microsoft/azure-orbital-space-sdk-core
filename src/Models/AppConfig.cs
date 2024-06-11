namespace Microsoft.Azure.SpaceFx;
public partial class Core {
    public class APP_CONFIG {
        public static string _SPACEFX_DIR {
            get {
                return Environment.GetEnvironmentVariable("SPACEFX_DIR") ?? "/var/spacedev";
            }
        }
        public static string _SPACEFX_CONFIG_DIR {
            get {
                return Environment.GetEnvironmentVariable("SPACEFX_SECRET_DIR") ?? "/etc/spacefx_config";
            }
        }

        public int HEARTBEAT_PULSE_TIMING_MS { get; set; }
        public int HEARTBEAT_RECEIVED_TOLERANCE_MS { get; set; }
        public bool RESOURCE_MONITOR_ENABLED { get; set; }
        public int RESOURCE_MONITOR_TIMING_MS { get; set; }
        public bool RESOURCE_SCAVENGER_ENABLED { get; set; }
        public int RESOURCE_SCAVENGER_TIMING_MS { get; set; }
        public string SPACEFX_DIR {
            get {
                return Environment.GetEnvironmentVariable("SPACEFX_DIR") ?? "/var/spacedev";
            }
        }

        public string SPACEFX_CONFIG_DIR {
            get {
                return Environment.GetEnvironmentVariable("SPACEFX_SECRET_DIR") ?? "/etc/spacefx_config";
            }
        }
        public string XFER_DIRECTORY_ROOT { get; set; }
        public string PLUGIN_DIRECTORY { get; set; }
        public APP_CONFIG() {
            try {
                HEARTBEAT_PULSE_TIMING_MS = int.Parse(GetConfigSetting("heartbeatpulsetimingms").Result);
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving heartbeatpulsetimingms: " + ex.Message);
                Console.WriteLine("Setting default value of '2000'");
                HEARTBEAT_PULSE_TIMING_MS = 2000;
            }

            try {
                HEARTBEAT_RECEIVED_TOLERANCE_MS = int.Parse(GetConfigSetting("heartbeatreceivedtolerancems").Result);
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving heartbeatreceivedtolerancems: " + ex.Message);
                Console.WriteLine("Setting default value of '10000'");
                HEARTBEAT_RECEIVED_TOLERANCE_MS = 10000;
            }

            try {
                RESOURCE_MONITOR_ENABLED = bool.Parse(GetConfigSetting("resourcemonitorenabled").Result);
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving resourcemonitorenabled: " + ex.Message);
                Console.WriteLine("Setting default value of 'true'");
                RESOURCE_MONITOR_ENABLED = true;
            }


            try {
                RESOURCE_SCAVENGER_ENABLED = bool.Parse(GetConfigSetting("resourcescavengerenabled").Result);
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving resourcescavengerenabled: " + ex.Message);
                Console.WriteLine("Setting default value of 'true'");
                RESOURCE_SCAVENGER_ENABLED = true;
            }


            try {
                RESOURCE_MONITOR_TIMING_MS = int.Parse(GetConfigSetting("resourcemonitortimingms").Result);
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving resourcemonitortimingms: " + ex.Message);
                Console.WriteLine("Setting default value of '5000'");
                RESOURCE_MONITOR_TIMING_MS = 5000;
            }


            try {
                RESOURCE_SCAVENGER_TIMING_MS = int.Parse(GetConfigSetting("resourcescavengertimingms").Result);
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving resourcescavengertimingms: " + ex.Message);
                Console.WriteLine("Setting default value of '30000'");
                RESOURCE_SCAVENGER_TIMING_MS = 30000;
            }

            try {
                XFER_DIRECTORY_ROOT = GetConfigSetting("spacefx_dir_xfer").Result;
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving spacefx_dir_xfer: " + ex.Message);
                Console.WriteLine("Setting default value of ''");
                XFER_DIRECTORY_ROOT = "";
            }

            try {
                PLUGIN_DIRECTORY = GetConfigSetting("spacefx_dir_plugins").Result;
            } catch (Exception ex) {
                Console.WriteLine("Error retrieving spacefx_dir_plugins: " + ex.Message);
                Console.WriteLine("Setting default value of ''");
                PLUGIN_DIRECTORY = "";
            }
        }
    }
}
