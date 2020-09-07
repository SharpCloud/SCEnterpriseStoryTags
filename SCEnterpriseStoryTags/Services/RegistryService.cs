using Microsoft.Win32;
using SCEnterpriseStoryTags.Interfaces;

namespace SCEnterpriseStoryTags.Services
{
    public class RegistryService : IRegistryService
    {
        private const string RegKey = "SOFTWARE\\SharpCloud\\AMRCWidget";

        public string RegRead(string keyName, string defVal)
        {
            // Opening the registry key
            var rk = Registry.CurrentUser;

            // Open a subKey as read-only
            var sk1 = rk.OpenSubKey(RegKey);

            if (sk1 != null)
            {
                var ret = (string)sk1.GetValue(keyName.ToUpper());
                if (ret == null)
                {
                    return defVal;
                }

                return ret;
            }
            return defVal;
        }

        public void RegWrite(string keyName, object value)
        {
            // Setting
            var rk = Registry.CurrentUser;
            var sk1 = rk.CreateSubKey(RegKey);

            // Save the value
            sk1.SetValue(keyName.ToUpper(), value);
        }

        public void RegDelete(string keyName)
        {
            // Opening the registry key
            var rk = Registry.CurrentUser;
            var sk1 = rk.OpenSubKey(RegKey, true);
            var ret = (string)sk1?.GetValue(keyName.ToUpper());
            if (ret != null)
            {
                sk1.DeleteValue(keyName);
            }
        }
    }
}
