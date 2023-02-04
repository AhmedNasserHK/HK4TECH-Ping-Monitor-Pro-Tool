﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using HK4TECHPingMonitorProTool.Views;

namespace HK4TECHPingMonitorProTool.Classes
{
    class CommandLine
    {
        public static List<string> ParseArguments()
        {
            var args = Environment.GetCommandLineArgs();
            var errorMessage = string.Empty;
            var hostnames = new List<string>();

            const int MinimumInterval = 1;
            const int MaxInterval = 86400;
            const int MinimumTimeout = 1;
            const int MaxTimeout = 60;

            const string CommandLineUsage = "HK4TECHPingMonitorProTool [-i interval] [-w timeout] [<target_host>...] [<path_to_list_of_hosts>...]";

            for (var index = 1; index < args.Length; ++index)
            {
                switch (args[index].ToLower())
                {
                    case "/i":
                    case "-i":
                        if (index + 1 < args.Length &&
                            int.TryParse(args[index + 1], out int interval) &&
                            interval >= MinimumInterval && interval <= MaxInterval)
                        {
                            ApplicationOptions.PingInterval = interval * 1000;
                            ++index;
                        }
                        else
                        {
                            errorMessage +=
                                $"For switch -i you must specify the number of seconds between {MinimumInterval} and {MaxInterval}.{Environment.NewLine}";
                            break;
                        }
                        break;
                    case "/w":
                    case "-w":
                        if (args.Length > index + 1 &&
                            int.TryParse(args[index + 1], out int timeout) &&
                            timeout >= MinimumTimeout && timeout <= MaxTimeout)
                        {
                            ApplicationOptions.PingTimeout = timeout * 1000;
                            ++index;
                        }
                        else
                        {
                            errorMessage +=
                                $"For switch -w you must specify the number of seconds between {MinimumTimeout} and {MaxTimeout}.{Environment.NewLine}";
                            break;
                        }
                        break;
                    case "/?":
                    case "-?":
                    case "-h":
                    case "--help":
                        var dialogWindow = new DialogWindow(
                            icon: DialogWindow.DialogIcon.Info,
                            title: "Command Line Usage",
                            body: CommandLineUsage,
                            confirmationText: "OK",
                            isCancelButtonVisible: false);
                        dialogWindow.Topmost = true;
                        dialogWindow.ShowDialog();
                        Application.Current.Shutdown();
                        break;
                    default:
                        // If an invalid argument is supplied, check to see if the argument is a valid path name.
                        //   If so, attempt to parse and read hosts from the file.  If not, use the argument as a hostname.
                        if (File.Exists(args[index]))
                            hostnames.AddRange(ReadHostsFromFile(args[index]));
                        else
                            hostnames.Add(args[index]);
                        break;
                }
            }

            // Display error message if any problems were encountered while parsing the arguments.
            if (errorMessage.Length > 0)
            {
                var dialogWindow = DialogWindow.ErrorWindow(
                    $"{errorMessage}{Environment.NewLine}Command line usage:{Environment.NewLine}{CommandLineUsage}");
                dialogWindow.Topmost = true;
                dialogWindow.ShowDialog();
                Application.Current.Shutdown();
            }

            return hostnames;
        }


        private static List<string> ReadHostsFromFile(string path)
        {
            const long MaxSizeInBytes = 10240;

            try
            {
                // Check file size.
                long length = new FileInfo(path).Length;
                if (length > MaxSizeInBytes) throw new FileFormatException();

                // Read file into a list of strings, so that each line can get checked.
                var linesInFile = new List<string>(File.ReadAllLines(path));

                // Get a list of valid lines.
                // Valid lines must not be empty and must being with a letter, digit, or '[' character (for IPv6).
                var validLines = linesInFile
                    .Where(x => !string.IsNullOrWhiteSpace(x) &&
                                (char.IsLetterOrDigit(x[0]) || x[0] == '['));

                // Convert list to multiline string (with each line trimmed).
                return validLines.Select(x => x.Trim()).ToList();
            }
            catch (FileFormatException)
            {
                var dialog = DialogWindow.ErrorWindow(
                    $"The file is too large and cannot be opened. The maximum file size is {MaxSizeInBytes / 1024} kb." +
                    $"{Environment.NewLine}{Environment.NewLine}{path}");
                dialog.Topmost = true;
                dialog.ShowDialog();
                return new List<string>();
            }
            catch
            {
                var dialog = DialogWindow.ErrorWindow(
                    "Failed parsing file." +
                    $"{Environment.NewLine}{Environment.NewLine}{path}");
                dialog.Topmost = true;
                dialog.ShowDialog();
                return new List<string>();
            }
        }
    }
}
