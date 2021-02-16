﻿/*AmpShell : .NET front-end for DOSBox
 * Copyright (C) 2009, 2021 Maximilien Noal
 *This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with this program.
 * If not, see <http://www.gnu.org/licenses/>.*/

namespace AmpShell.DOSBox
{
    using System.Diagnostics;
    using System.IO;

    using AmpShell.DAL;
    using AmpShell.Model;

    /// <summary>
    /// Used to start DOSBox with a game in it.
    /// </summary>
    public class DOSBoxController
    {
        private readonly Game gameInstance;

        public DOSBoxController(Game game)
        {
            this.gameInstance = game;
        }

        /// <summary>
        /// Run DOSBox itself, without any game.
        /// </summary>
        /// <returns>The DOSBox process if it started successfully, null otherwise.</returns>
        public static Process RunOnlyDOSBox()
        {
            string langArgument = string.Empty;
            if (string.IsNullOrWhiteSpace(UserDataAccessor.UserData.DBDefaultLangFilePath) == false)
            {
                langArgument = $" -lang \"{UserDataAccessor.UserData.DBDefaultLangFilePath}\"";
            }

            string args = langArgument;
            if (string.IsNullOrWhiteSpace(UserDataAccessor.UserData.DBDefaultConfFilePath) == false)
            {
                args = $" -conf \"{UserDataAccessor.UserData.DBDefaultConfFilePath}\"{langArgument}";
            }
            var proc = Process.Start(UserDataAccessor.UserData.DBPath, args);
            if (proc != null)
            {
                proc.EnableRaisingEvents = true;
            }
            return proc;
        }

        /// <summary>
        /// Starts DOSBox with <see cref="Game"/> inside it.
        /// </summary>
        /// <returns>The DOSBox process.</returns>
        public Process StartGame() => this.StartDOSBox(this.BuildArgs(false));

        /// <summary>
        /// Starts DOSBox with <see cref="Game"/>.<see cref="Game.SetupEXEPath"/> inside it.
        /// </summary>
        /// <returns>The DOSBox process.</returns>
        public Process StartGameSetup() => this.StartDOSBox(this.BuildArgs(true));

        /// <summary>
        /// Builds the argument line in order to start DOSBox.
        /// </summary>
        /// <param name="forSetupExe">
        /// Whether or not we are starting the game's setup utility or the game itself.
        /// </param>
        /// <returns>The list of command line arguments to pass to DOSBox.</returns>
        private string BuildArgs(bool forSetupExe)
        {
            var configFile = new DOSBoxConfigFile(this.gameInstance.DBConfPath);

            string dosboxArgs = string.Empty;
            string dosBoxExePath = this.gameInstance.GetDOSBoxPath();
            if (string.IsNullOrWhiteSpace(dosBoxExePath) == true || dosBoxExePath == "dosbox.exe isn't is the same directory as AmpShell.exe!" || File.Exists(dosBoxExePath) == false)
            {
                throw new FileNotFoundException("DOSBox not found!");
            }

            dosboxArgs += this.AddCustomConfigFile();

            if (string.IsNullOrWhiteSpace(UserDataAccessor.UserData.DBDefaultLangFilePath) == false && UserDataAccessor.UserData.DBDefaultLangFilePath != "No language file (*.lng) found in AmpShell's directory.")
            {
                dosboxArgs += $" -lang \"{UserDataAccessor.UserData.DBDefaultLangFilePath}\"";
            }

            dosboxArgs += this.AddAdditionalCommands(forSetupExe, configFile);

            //corresponds to the Fullscreen checkbox in GameForm
            if (this.gameInstance.InFullScreen == true)
            {
                dosboxArgs += " -fullscreen";
            }

            //corresponds to the "no console" checkbox in the GameForm
            if (this.gameInstance.NoConsole == true)
            {
                dosboxArgs += " -noconsole";
            }

            //corresponds to the "quit on exit (only for .exe)" checkbox in the GameForm
            if (this.gameInstance.QuitOnExit == true)
            {
                dosboxArgs += " -exit";
            }

            return dosboxArgs;
        }

        /// <summary>
        /// Starts DOSBox, and returns its <see cref="Process" />.
        /// </summary>
        /// <returns>The DOSBox <see cref="Process" />.</returns>
        private Process StartDOSBox(string args)
        {
            var psi = new ProcessStartInfo(this.gameInstance.GetDOSBoxPath());

            if (string.IsNullOrWhiteSpace(this.gameInstance.DBConfPath) == false)
            {
                psi.WorkingDirectory = Path.GetDirectoryName(this.gameInstance.DBConfPath);
            }

            if (string.IsNullOrWhiteSpace(args) == false)
            {
                psi.Arguments = args;
            }
            Process dosboxProcess = Process.Start(psi);
            if (dosboxProcess != null)
            {
                dosboxProcess.EnableRaisingEvents = true;
            }
            return dosboxProcess;
        }

        private string AddAdditionalCommands(bool forSetupExe, DOSBoxConfigFile configFile)
        {
            string dosboxArgs = string.Empty;
            if (configFile.IsAutoExecSectionUsed() == true)
            {
                return dosboxArgs;
            }

            //The arguments for DOSBox begins with the game executable (.exe, .bat, or .com)
            if (string.IsNullOrWhiteSpace(this.gameInstance.DOSEXEPath) == false)
            {
                if (!forSetupExe)
                {
                    dosboxArgs = $"\"{this.gameInstance.DOSEXEPath}\"";
                }
                else
                {
                    dosboxArgs = $"\"{this.gameInstance.SetupEXEPath}\"";
                }
            }

            //the game directory mounted as C (if the DOSEXEPath is specified, the DOSEXEPath parent directory will be mounted as C: by DOSBox
            //hence the "else if" instead of "if".
            else if (string.IsNullOrWhiteSpace(this.gameInstance.Directory) == false)
            {
                dosboxArgs = $" -c \"mount c '{this.gameInstance.Directory}'\"";
            }

            //Path for the game's CD image (.bin, .cue, or .iso) mounted as D:
            if (string.IsNullOrWhiteSpace(this.gameInstance.CDPath) == false)
            {
                //put ' and not " after imgmount (or else the path will be misunderstood by DOSBox).
                if (this.gameInstance.CDIsAnImage == true)
                {
                    dosboxArgs += $" -c \"imgmount";
                    if (this.gameInstance.MountAsFloppy == true)
                    {
                        dosboxArgs += $" a '{this.gameInstance.CDPath}' -t floppy\"";
                    }
                    else
                    {
                        dosboxArgs += $" d '{this.gameInstance.CDPath}' -t iso\"";
                    }
                }
                else
                {
                    bool addedMountOptions;
                    if (this.gameInstance.UseIOCTL == true)
                    {
                        addedMountOptions = true;
                        dosboxArgs += $" -c \"mount d '{this.gameInstance.CDPath}' -t cdrom -usecd 0 -ioctl";
                    }
                    else if (this.gameInstance.MountAsFloppy == true)
                    {
                        addedMountOptions = true;
                        dosboxArgs += $" -c \"mount a '{this.gameInstance.CDPath}' -t floppy";
                    }
                    else
                    {
                        addedMountOptions = true;
                        dosboxArgs += $" -c \"mount d '{this.gameInstance.CDPath}'";
                    }
                    if (string.IsNullOrWhiteSpace(this.gameInstance.CDLabel) == false && addedMountOptions)
                    {
                        dosboxArgs += $" -label {this.gameInstance.CDLabel}";
                    }
                    if (addedMountOptions)
                    {
                        dosboxArgs += '"';
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(this.gameInstance.AdditionalCommands) == false)
            {
                dosboxArgs += $" {this.gameInstance.AdditionalCommands}";
            }

            return dosboxArgs;
        }

        private string AddCustomConfigFile()
        {
            string gameConfigFilePath = string.Empty;

            //if the "do not use any config file at all" has not been checked
            if (this.gameInstance.NoConfig == false)
            {
                //use at first the game's custom config file
                if (string.IsNullOrWhiteSpace(this.gameInstance.DBConfPath) == false)
                {
                    gameConfigFilePath = this.gameInstance.DBConfPath;
                }

                //if not, use the default dosbox.conf file
                else if (string.IsNullOrWhiteSpace(UserDataAccessor.UserData.DBDefaultConfFilePath) == false && UserDataAccessor.UserData.DBDefaultConfFilePath != "No configuration file (*.conf) found in AmpShell's directory.")
                {
                    gameConfigFilePath = UserDataAccessor.UserData.DBDefaultConfFilePath;
                }
            }
            string dosboxArgs = string.Empty;
            if (string.IsNullOrWhiteSpace(gameConfigFilePath) == false)
            {
                dosboxArgs += $" -conf \"{gameConfigFilePath}\"";
            }

            return dosboxArgs;
        }
    }
}