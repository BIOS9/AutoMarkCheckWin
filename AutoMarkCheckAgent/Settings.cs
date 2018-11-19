using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoMarkCheck.Helpers.Logging;

namespace AutoMarkCheckAgent
{
    public class Settings
    {
        public const string FileName = "settings.json";

        //Defaults
        public bool CheckingEnabled = true;
        public bool CoursesPublic = false ;
        public int GradeCheckInterval = 300; // 5 minutes
        public DateTime LastGradeCheck = DateTime.MinValue;

        public static async Task Save(Settings settings)
        {
            try
            {
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(Settings)}.{nameof(Save)}", "Starting settings save.");

                string json = JsonConvert.SerializeObject(settings); // Convert settings object into json string

                //Write string to settings file
                using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (StreamWriter writer = new StreamWriter(stream))
                    await writer.WriteLineAsync(json);

                Logging.Log(LogLevel.INFO, $"{nameof(AutoMarkCheckAgent)}.{nameof(Settings)}.{nameof(Save)}", "Successfully saved settings.");
            }
            catch(Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(Settings)}.{nameof(Save)}", $"Failed to save settings to \"{FileName}\".", ex);
                throw ex;
            }
        }

        public static async Task<Settings> Load()
        {
            try
            {
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(Settings)}.{nameof(Load)}", "Starting settings load.");

                Settings settings;

                //If file doesn't exist, load default settings and save the default
                if (!File.Exists(FileName))
                {
                    Logging.Log(LogLevel.WARNING, $"{nameof(AutoMarkCheckAgent)}.{nameof(Settings)}.{nameof(Load)}", "Settings file does not exist, loading default settings.");
                    settings = new Settings();
                    await Save(settings);
                    return settings;
                }

                string json = "";

                //Read string from settings file
                using (FileStream stream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (StreamReader reader = new StreamReader(stream))
                    json = await reader.ReadToEndAsync();

                settings = JsonConvert.DeserializeObject<Settings>(json); //Convert json string to settings object

                if (settings.LastGradeCheck > DateTime.Now) //Ensure last grade check cannot be in the future
                    settings.LastGradeCheck = DateTime.MinValue;

                Logging.Log(LogLevel.INFO, $"{nameof(AutoMarkCheckAgent)}.{nameof(Settings)}.{nameof(Load)}", "Successfully loaded settings.");

                return settings;
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(Settings)}.{nameof(Load)}", $"Failed to load settings from \"{FileName}\".", ex);
                return null;
            }
        }
    }
}
