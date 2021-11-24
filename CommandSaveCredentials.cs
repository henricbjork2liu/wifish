using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Security.Credentials;

namespace wifish
{
    internal class CommandSaveCredentials : ICommand
    {
        private List<string> _args;
        private PasswordCredential _credential;
        private string _resource;

        public string Description { get; } = @"
Usage:  wifish10 save [name] [-p MyPassword]
        wifish10 save [name] [-u MyUsername] [-p MyPassword]
        wifish10 save [name] [-c]

Subcommands in this context:
[-p | -password]            - Saves with password. Required.
[-u | -username]            - Saves with username. Optional.";

        public bool AreRequirementsFulfilled()
        {
            _credential = new PasswordCredential();
            _args = Program.Arguments.ToList();
            _resource = Program.GetValueOfSubcommand(_args, 0);
            if (string.IsNullOrEmpty(_resource))
            {
                Console.WriteLine("'Resource' can't be empy");
                return false;
            }
            _credential.Resource = _resource;
            string result;

            // Password
            if (!Program.TryGetSubCommandAndValue(_args, "-password", "-p", out result))
                return false;
            if (result != null)
                _credential.Password = result;

            // Username
            if (!Program.TryGetSubCommandAndValue(_args, "-username", "-u", out result))
                return false;
            if (result != null)
                _credential.UserName = result;

            if (_args.Count > 0)
            {
                Console.WriteLine("Too many arguments:");
                foreach (string arg in _args)
                    Console.WriteLine($"  Could not parse: {arg}");
                return false;
            }
            return true;
        }

        public void Execute()
        {
            PasswordVault vault = new PasswordVault();
            vault.Add(_credential);
        }
    }
}
