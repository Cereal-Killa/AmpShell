﻿using System.Globalization;
/*AmpShell : .NET front-end for DOSBox
 * Copyright (C) 2009, 2019 Maximilien Noal
 *This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with this program.
 * If not, see <http://www.gnu.org/licenses/>.*/

using AmpShell.AutoConfig;
using AmpShell.Model;
using AmpShell.Serialization;

using System;
using System.IO;
using System.Linq;

namespace AmpShell.DAL
{
    public static class UserDataAccessor
    {
        static UserDataAccessor()
        {
            UserData = new Preferences();
        }
        /// <summary>
        /// Used when a new Category or Game is created : it's signature must be unique
        /// so AmpShell can recognize it instantly
        /// </summary>
        /// <param name="signatureToTest">A Category's or Game's signature</param>
        /// <returns>Whether the signature equals none of the other ones, or not</returns>
        public static bool IsItAUniqueSignature(string signatureToTest)
        {
            foreach (Category otherCat in UserData.ListChildren)
            {
                if (otherCat.Signature != signatureToTest)
                {
                    if (otherCat.ListChildren.Count != 0)
                    {
                        foreach (Game otherGame in otherCat.ListChildren)
                        {
                            if (otherGame.Signature == signatureToTest)
                            {
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static string GetAUniqueSignature()
        {
            string newSignature;
            do
            {
                Random randNumber = new Random();
                newSignature = randNumber.Next(1048576).ToString(CultureInfo.InvariantCulture);
            }
            while (IsItAUniqueSignature(newSignature) == false);
            return newSignature;
        }

        internal static Category GetCategoryWithSignature(string signature)
        {
            return UserDataAccessor.UserData.ListChildren.Cast<Category>().FirstOrDefault(x => x.Signature == signature);
        }

        internal static Game GetGameWithSignature(string signature)
        {
            return UserDataAccessor.UserData.ListChildren.Cast<Category>().SelectMany(x => x.ListChildren.Cast<Game>()).FirstOrDefault(x => x.Signature == signature);
        }

        /// <summary>
        /// Object to load and save user data through XML (de)serialization
        /// </summary>
        public static Preferences UserData { get; private set; }

        public static void SaveUserSettings()
        {
            //saves the data inside Amp by serializing it in AmpShell.xml
            if (!UserData.PortableMode)
            {
                ObjectSerializer.Serialize<Preferences>(GetDataFilePath(), UserData);
            }
            else
            {
                foreach (Category category in UserData.ListChildren)
                {
                    foreach (Game game in category.ListChildren)
                    {
                        game.DOSEXEPath = game.DOSEXEPath.Replace(PathFinder.GetStartupPath(), "AppPath");
                        game.DBConfPath = game.DBConfPath.Replace(PathFinder.GetStartupPath(), "AppPath");
                        game.AdditionalCommands = game.AdditionalCommands.Replace(PathFinder.GetStartupPath(), "AppPath");
                        game.Directory = game.Directory.Replace(PathFinder.GetStartupPath(), "AppPath");
                        game.CDPath = game.CDPath.Replace(PathFinder.GetStartupPath(), "AppPath");
                        game.SetupEXEPath = game.SetupEXEPath.Replace(PathFinder.GetStartupPath(), "AppPath");
                        game.Icon = game.Icon.Replace(PathFinder.GetStartupPath(), "AppPath");
                    }
                }
                UserData.DBDefaultConfFilePath = UserData.DBDefaultConfFilePath.Replace(PathFinder.GetStartupPath(), "AppPath");
                UserData.DBDefaultLangFilePath = UserData.DBDefaultLangFilePath.Replace(PathFinder.GetStartupPath(), "AppPath");
                UserData.DBPath = UserData.DBPath.Replace(PathFinder.GetStartupPath(), "AppPath");
                UserData.ConfigEditorPath = UserData.ConfigEditorPath.Replace(PathFinder.GetStartupPath(), "AppPath");
                UserData.ConfigEditorAdditionalParameters = UserData.ConfigEditorAdditionalParameters.Replace(PathFinder.GetStartupPath(), "AppPath");
                ObjectSerializer.Serialize<Preferences>(PathFinder.GetStartupPath() + "\\AmpShell.xml", UserData);
            }
        }

        public static void LoadUserSettings()
        {
            try
            {
                UserData = ObjectSerializer.Deserialize<Preferences>(GetDataFilePath());
            }
            catch (FileNotFoundException)
            {
                UserData = new Preferences();
            }
            foreach (Category concernedCategory in UserData.ListChildren)
            {
                foreach (Game concernedGame in concernedCategory.ListChildren)
                {
                    concernedGame.DOSEXEPath = concernedGame.DOSEXEPath.Replace("AppPath", PathFinder.GetStartupPath());
                    concernedGame.DBConfPath = concernedGame.DBConfPath.Replace("AppPath", PathFinder.GetStartupPath());
                    concernedGame.AdditionalCommands = concernedGame.AdditionalCommands.Replace("AppPath", PathFinder.GetStartupPath());
                    concernedGame.Directory = concernedGame.Directory.Replace("AppPath", PathFinder.GetStartupPath());
                    concernedGame.CDPath = concernedGame.CDPath.Replace("AppPath", PathFinder.GetStartupPath());
                    concernedGame.SetupEXEPath = concernedGame.SetupEXEPath.Replace("AppPath", PathFinder.GetStartupPath());
                    concernedGame.Icon = concernedGame.Icon.Replace("AppPath", PathFinder.GetStartupPath());
                }
            }
            UserData.DBDefaultConfFilePath = UserData.DBDefaultConfFilePath.Replace("AppPath", PathFinder.GetStartupPath());
            UserData.DBDefaultLangFilePath = UserData.DBDefaultLangFilePath.Replace("AppPath", PathFinder.GetStartupPath());
            UserData.DBPath = UserData.DBPath.Replace("AppPath", PathFinder.GetStartupPath());
            UserData.ConfigEditorPath = UserData.ConfigEditorPath.Replace("AppPath", PathFinder.GetStartupPath());
            UserData.ConfigEditorAdditionalParameters = UserData.ConfigEditorAdditionalParameters.Replace("AppPath", PathFinder.GetStartupPath());

            if (string.IsNullOrWhiteSpace(UserData.DBPath))
            {
                UserData.DBPath = FileFinder.SearchDOSBox(GetDataFilePath(), UserData.PortableMode);
            }
            else if (File.Exists(UserData.DBPath) == false)
            {
                UserData.DBPath = FileFinder.SearchDOSBox(GetDataFilePath(), UserData.PortableMode);
            }
            if (string.IsNullOrWhiteSpace(UserData.ConfigEditorPath))
            {
                UserData.ConfigEditorPath = FileFinder.SearchCommonTextEditor();
            }
            else if (File.Exists(UserData.ConfigEditorPath) == false)
            {
                UserData.ConfigEditorPath = FileFinder.SearchCommonTextEditor();
            }

            if (string.IsNullOrWhiteSpace(UserData.DBDefaultConfFilePath))
            {
                UserData.DBDefaultConfFilePath = FileFinder.SearchDOSBoxConf(GetDataFilePath(), UserData.DBPath);
            }
            else if (File.Exists(UserData.DBDefaultConfFilePath) == false)
            {
                UserData.DBDefaultConfFilePath = FileFinder.SearchDOSBoxConf(GetDataFilePath(), UserData.DBPath);
            }

            if (string.IsNullOrWhiteSpace(UserData.DBDefaultLangFilePath) == false)
            {
                UserData.DBDefaultLangFilePath = FileFinder.SearchDOSBoxLanguageFile(UserData.DBPath);
            }
            else if (File.Exists(UserData.DBDefaultLangFilePath) == false)
            {
                UserData.DBDefaultLangFilePath = FileFinder.SearchDOSBoxLanguageFile(UserData.DBPath);
            }
        }

        /// <summary>
        /// Returns the path to the user data file (AmpShell.xml)
        /// </summary>
        private static string GetDataFilePath()
        {
            var appDataFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmpShell\\AmpShell.xml");
            if (FileFinder.HasWriteAccessToAssemblyLocationFolder() == false)
            {
                var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmpShell");
                if (Directory.Exists(appDataDir) == false)
                {
                    Directory.CreateDirectory(appDataDir);
                }
                return appDataFile;
            }
            else
            {
                if(File.Exists(appDataFile))
                {
                    return appDataFile;
                }
                return Path.Combine(PathFinder.GetStartupPath(), "AmpShell.xml");
            }
        }
    }
}