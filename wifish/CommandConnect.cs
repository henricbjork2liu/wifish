using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using NetworkAuthenticationType = Windows.Networking.Connectivity.NetworkAuthenticationType;

namespace wifish
{
    public class CommandConnect : ICommand
    {
        private List<string> _args;
        private bool _continuous = false;
        private int _interval = 60;

        public string Description { get; } = @"
Usage:  wifish10 connect [ssid]
        wifish10 connect [ssid] [-p Password]
        wifish10 connect [ssid] [-u UserName] [-p Password]
        wifish10 connect [ssid] [-u DomainName\UserName] [-p Password]
        wifish10 connect [ssid] [-w Resource] [-u UserName]
        wifish10 connect [ssid] [-c] [-u UserName] [-p Password]
        wifish10 connect [ssid] [-c] [-w Resource] [-u UserName]

Subcommands in this context:
[-p | -password]            - Connects with password. Optional unless username or domain are entered.
[-u | -username]            - Connects with username. Optional unless domain is entered.
[-w | -wincredentials]      - Uses credentials from Windows Credentials. Requires resource name and username [-u]
[-c | -continuous]          - Will continuously connect to the selectes ssid. Value is in seconds. Optional.";

        public void Execute()
        {
            if(!_continuous)
                _connect();
            else
            {
                _connect();
                while(true)
                {
                    System.Threading.Thread.Sleep(_interval * 1000);
                    Task<bool> t = _is_connected();
                    t.Wait();
                    if (!t.Result)
                        _connect();
                }
            }
        }

        public bool AreRequirementsFulfilled()
        {
            _args = Program.Arguments.ToList();
            Program.Ssid = Program.GetValueOfSubcommand(_args, 0);
            if (Program.Ssid.StartsWith("-"))
            {
                Console.WriteLine("Error, missing SSID");
                return false;
            }

            string _username = null;
            string _password = null;
            string result;

            // Continuous and interval
            if (!Program.TryGetSubCommandAndValue(_args, "-continuous", "-c", out result))
                return false;
            if (result != null)
            {
                if (int.TryParse(result, out int interval))
                {
                    _continuous = true;
                    _interval = interval;
                }
                else
                {
                    Console.WriteLine("Error: value after -continuous is not valid. Should be in seconds and less than 604801");
                    return false;
                }
            }

            // Password
            if (!Program.TryGetSubCommandAndValue(_args, "-password", "-p", out result))
                return false;
            else if (result != null)
                _password = result;

            // Username
            if (!Program.TryGetSubCommandAndValue(_args, "-username", "-u", out result))
                return false;
            else if (result != null)
                _username = result;

            if (_username != null)
                Program.Credentials.UserName = _username;
            if (_password != null)
                Program.Credentials.Password = _password;


            // Windows Credentials
            if (!Program.TryGetSubCommandAndValue(_args, "-wincredentials", "-w", out result))
                return false;
            if (result != null)
            {
                if (_username == null)
                {
                    Console.WriteLine("Username is needed when loading credentials from Windows Credentials");
                    return false;
                }
                if (!_loadCredentials(result, _username))
                    return false;
            }

            if (_args.Count > 0)
            {
                Console.WriteLine("  Too many arguments:");
                foreach (string arg in _args)
                    Console.WriteLine($"    Could not parse: {arg}");
                return false;
            }
            return true;
        }

        private bool _loadCredentials(string resource, string username)
        {
            PasswordCredential tempCredentials = null;
            try
            {
                PasswordVault vault = new PasswordVault();
                IReadOnlyList<PasswordCredential> credentialList = vault.FindAllByResource(resource);
                if (credentialList.Count > 0)
                {
                    tempCredentials = vault.Retrieve(resource, username);
                    Program.Credentials.Password = tempCredentials.Password;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Could not retrieve credentials");
            return false;
        }

        private void _connect()
        {
            Program.ScanForNetworks().Wait();
            string ssid = Program.Ssid.ToLower();
            if (Program.AvailableNetworks == null || !Program.AvailableNetworks.ContainsKey(ssid))
            {
                Console.WriteLine($"Could not find {ssid}");
                return;
            }

            WiFiAvailableNetwork available_network = Program.AvailableNetworks[ssid];
            Task<WiFiConnectionResult> task_connect = null;
            WiFiReconnectionKind reconnectionKind = WiFiReconnectionKind.Automatic;

            switch (available_network.SecuritySettings.NetworkAuthenticationType)
            {
                case NetworkAuthenticationType.Open80211:
                case NetworkAuthenticationType.None:
                case NetworkAuthenticationType.Unknown:
                    task_connect = Program.MainAdapter.ConnectAsync(available_network, reconnectionKind).AsTask();
                    break;
                default:
                    task_connect = Program.MainAdapter.ConnectAsync(available_network, reconnectionKind, Program.Credentials).AsTask();
                    break;
            }
            task_connect.Wait();
            WiFiConnectionResult result = task_connect.Result;
            if (result?.ConnectionStatus == WiFiConnectionStatus.Success)
                Console.WriteLine($"Connected to '{ssid}'");
            else
                Console.WriteLine($"Error: {result?.ConnectionStatus}");
        }

        private async Task<bool> _is_connected()
        {
            ConnectionProfile profile = await Program.MainAdapter.NetworkAdapter.GetConnectedProfileAsync(); //Gets currently connected wlan profile
            if(profile != null && profile.ProfileName != Program.Ssid)
            {
                Program.MainAdapter.Disconnect();
                return false;
            }

            if (profile == null)
                return false;

            return true;
        }
    }
}
