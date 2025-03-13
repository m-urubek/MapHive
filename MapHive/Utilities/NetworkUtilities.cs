using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MapHive.Utilities
{
    public static class NetworkUtilities
    {
        /// <summary>
        /// Gets the MAC address from an IP address.
        /// Note: In a web application, this might not be reliable as it gets the MAC address
        /// from the ARP table which may not have an entry for remote clients.
        /// In a real environment, you might use JavaScript on the client side or other methods.
        /// </summary>
        /// <param name="ipAddress">The IP address to look up</param>
        /// <returns>The MAC address as a string</returns>
        public static string GetMacAddressFromIp(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                return "00:00:00:00:00:00";
            }

            // For local development testing, return a mock MAC based on IP to simulate different devices
            if (ipAddress is "127.0.0.1" or "::1")
            {
                return "10:11:12:13:14:15"; // Mock MAC for local testing
            }

            try
            {
                // Try to get the MAC from the ARP cache (Windows approach)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string macFromArp = GetMacFromArpWindows(ipAddress);
                    if (!string.IsNullOrEmpty(macFromArp))
                    {
                        return macFromArp;
                    }
                }

                // Alternative approach using NetworkInterface 
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType is not NetworkInterfaceType.Ethernet and
                        not NetworkInterfaceType.Wireless80211)
                    {
                        continue;
                    }

                    PhysicalAddress? pa = nic.GetPhysicalAddress();
                    if (pa != null)
                    {
                        byte[] bytes = pa.GetAddressBytes();
                        return string.Join(":", bytes.Select(b => b.ToString("X2")));
                    }
                }

                // Fall back to default MAC if we can't get it
                return "00:00:00:00:00:00";
            }
            catch (Exception)
            {
                // Log error and return a default value
                return "00:00:00:00:00:00";
            }
        }

        private static string GetMacFromArpWindows(string ipAddress)
        {
            try
            {
                // Run the ARP command
                System.Diagnostics.Process pProcess = new();
                pProcess.StartInfo.FileName = "arp";
                pProcess.StartInfo.Arguments = "-a " + ipAddress;
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.CreateNoWindow = true;
                _ = pProcess.Start();
                string strOutput = pProcess.StandardOutput.ReadToEnd();
                pProcess.WaitForExit();

                // Extract the MAC address using regex
                string pattern = @"([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})";
                Match match = Regex.Match(strOutput, pattern);
                if (match.Success)
                {
                    return match.Value;
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }

            return string.Empty;
        }
    }
}