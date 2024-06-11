namespace Microsoft.Azure.SpaceFx;
public partial class Core {
    public partial class Models {
        public class PLUG_IN {
            [Flags]
            public enum CorePermissions {
                NONE,
                ALLOW_WRITEBACK,
                ALLOW_BACKGROUND_TASK,
                ALL = ALLOW_WRITEBACK | ALLOW_BACKGROUND_TASK
            }
            public bool ENABLED { get; set; }
            public int PROCESSING_ORDER { get; set; }
            public string PLUGINFILE { get; set; }
            public string PLUGINNAME { get; set; }
            public string CORE_PERMISSIONS { get; set; }
            public string PLUGIN_PERMISSIONS { get; set; }
            public Dictionary<string, string> CONFIGURATION { get; set; }
            public CorePermissions CALCULATED_CORE_PERMISSIONS {
                get {
                    CorePermissions result;
                    System.Enum.TryParse(CORE_PERMISSIONS, out result);
                    return result;
                }
            }

            public PLUG_IN() {
                ENABLED = true;
                PROCESSING_ORDER = 0;
                PLUGINFILE = "";
                PLUGINNAME = "";
                CORE_PERMISSIONS = "";
                PLUGIN_PERMISSIONS = "";
                CONFIGURATION = new Dictionary<string, string>();
            }
        }
    }
}
