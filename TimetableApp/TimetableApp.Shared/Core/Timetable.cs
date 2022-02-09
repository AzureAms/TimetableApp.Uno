using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
#if __WASM__
using WebClient = TimetableApp.Core.WasmWebClient;
#endif

namespace TimetableApp.Core
{
    [Serializable]
    public class Timetable
    {
        #region Private values
        private static readonly int DaysInAWeek = System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames.Length;
        private static string DataFilePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "TimetableAppData", "data.json");
        #endregion

        #region Fields
        //Name: class name, school, id,... for human use.
        public string Name;
        public string UpdateURL;

        //0 is Sunday.
        public List<Lesson>[] Lessons;

        public event EventHandler<EventArgs> OnSucessfulUpdate;
        #endregion

        #region Constructors
        //This class is designed to be used by JSON parsers.
        [JsonConstructor]
        public Timetable() { Lessons = Lessons ?? Enumerable.Range(0, DaysInAWeek).Select(x => new List<Lesson>()).ToArray(); }
        #endregion

        #region Queries
        public Lesson GetCurrentLesson()
        {
            DateTime currentTime = DateTime.Now;
            int day = (int)currentTime.DayOfWeek;
            TimeSpan time = currentTime.TimeOfDay;

            //A typical student cannot have more than 1e5 lessons a day, so yes,
            //brute-forcing is practically acceptable here.

            foreach (var l in Lessons[day])
            {
                if ((l.StartTime <= time) && (time <= l.EndTime))
                {
                    return l;
                }
            }

            //Hooray! No classes for you!
            return null;
        }

        public Lesson GetNextLesson(TimeSpan? MaxDelay)
        {
            DateTime currentTime = DateTime.Now;
            int day = (int)currentTime.DayOfWeek;
            TimeSpan time = currentTime.TimeOfDay;

            for (int i = day; i < day + 7; ++i)
            {
                int dayOfWeek = i % 7;
                foreach (var l in Lessons[dayOfWeek])
                {
                    var startTime = l.StartTime + TimeSpan.FromDays(i - day);
                    if (startTime < time) continue;
                    if (MaxDelay == null)
                    {
                        return l;
                    }
                    else
                    {
                        if (startTime <= time + MaxDelay) return l;
                    }
                }
            }
            

            return null;
        }

        public bool CheckNextLesson(TimeSpan? MaxDelay)
        {
            return GetNextLesson(MaxDelay) != null;
        }
        #endregion

        #region File operations
        public async Task<string> UpdateAsync()
        {
            if (string.IsNullOrEmpty(UpdateURL))
            {
                return "Bad update URL";
            }

            try
            {
                string oldSha512;
                string newSha512;

                Timetable newTimetable = null;

                using (var localFile = File.OpenRead(DataFilePath))
                {
                    var hasher = SHA512.Create();
                    var hash = hasher.ComputeHash(localFile);
                    oldSha512 = string.Concat(hash.Select(x => x.ToString("X2")));
                }

                using (var client = new WebClient())
                {
                    byte[] data = await client.DownloadDataTaskAsync(UpdateURL);

                    using (var stream = new MemoryStream(data))
                    using (var sr = new StreamReader(stream))
                    {
                        var response = (UpdateResponse)JsonConvert.DeserializeObject(sr.ReadToEnd(), typeof(UpdateResponse));
                        newSha512 = response.SHA512;

                        if (string.Compare(oldSha512, newSha512, true) != 0)
                        {
                            data = await client.DownloadDataTaskAsync(response.Location);

                            var hash = SHA512.Create().ComputeHash(data);

                            var hashString = string.Concat(hash.Select(x => x.ToString("X2")));
                            if (string.Compare(newSha512, hashString, true) != 0)
                            {
                                return "Bad hash";
                            }
                            using (var timetableStream = new MemoryStream(data))
                            using (var timetableReader = new StreamReader(timetableStream))
                            {
                                var timetableData = timetableReader.ReadToEnd().Replace("$ASSEMBLY_NAME", Assembly.GetExecutingAssembly().GetName().Name);
                                //throw new Exception(timetableData);
                                newTimetable =
                                    (Timetable)JsonConvert.DeserializeObject(timetableData, typeof(Timetable),
                                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                            }
                        }
                        else return null;
                    }
                }


                if (newTimetable != null)
                {
                    Name = newTimetable.Name;
                    UpdateURL = newTimetable.UpdateURL;
                    Lessons = newTimetable.Lessons;
                }

                try
                {
                    OnSucessfulUpdate?.Invoke(this, null);
                }
                catch { }

                _ = SaveAsync();

                return null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                Console.WriteLine(e.ToString());
                //throw;
                return e.ToString();
            }
        }   

        private async Task ReloadAsync()
        {
            await ApplicationData.Current.LocalFolder.CreateFolderAsync("TimetableAppData", CreationCollisionOption.OpenIfExists);
            try
            {
                using (var stream = File.OpenText(DataFilePath))
                {
                    var newTimetable = (Timetable)JsonConvert.DeserializeObject(stream.ReadToEnd().Replace("$ASSEMBLY_NAME", Assembly.GetExecutingAssembly().GetName().Name), typeof(Timetable),
                        new JsonSerializerSettings()
                        {
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                    if (newTimetable != null)
                    {
                        Name = newTimetable.Name;
                        UpdateURL = newTimetable.UpdateURL;
                        Lessons = newTimetable.Lessons;
                        OnSucessfulUpdate?.Invoke(this, null);
                    }
                    else
                    {
                        await SaveAsync();
                    }
                }
            }
            catch
            {
                await SaveAsync();
            }
            finally
            {
                if (!string.IsNullOrEmpty(UpdateURL))
                {
                    // Auto-update and be quiet.
                    _ = await UpdateAsync();
                }
                Loaded?.Invoke(this, EventArgs.Empty);
            }
        }

        // On some targets where Load must be async...
        public event EventHandler Loaded;

        public static Timetable Load()
        {
            try
            {
                // File system has not been initialized yet.
                if (PlatformHelper.RuntimePlatform == Platform.WASM)
                {
                    var timetable = new Timetable();
                    _ = timetable.ReloadAsync();
                    return timetable;
                }
                using (var stream = File.OpenText(DataFilePath))
                {
                    var timetable = (Timetable)JsonConvert.DeserializeObject(stream.ReadToEnd().Replace("$ASSEMBLY_NAME", Assembly.GetExecutingAssembly().GetName().Name), typeof(Timetable),
                        new JsonSerializerSettings()
                        {
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                    if (timetable == null)
                    {
                        timetable = new Timetable();
                        // Yes, no await doesn't do any harm. The UI can continue work normally.
                        _ = timetable.SaveAsync();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(timetable.UpdateURL))
                        {
                            System.Diagnostics.Debug.WriteLine($"Automatically updating from {timetable.UpdateURL}");

                            timetable.UpdateAsync().ContinueWith((task) =>
                            {
                                System.Diagnostics.Debug.WriteLine($"Done updating from {timetable.UpdateURL}");
                                timetable.Loaded?.Invoke(timetable, EventArgs.Empty);
                            });
                        }
                        else
                        {
                            timetable.Loaded?.Invoke(timetable, EventArgs.Empty);
                        }
                    }
                    return timetable;
                }
            }
            catch
            {
                var timetable = new Timetable();
                _ = timetable.SaveAsync();
                return timetable;
            }
        }

        public async Task SaveAsync()
        {
            await ApplicationData.Current.LocalFolder.CreateFolderAsync("TimetableAppData", CreationCollisionOption.OpenIfExists);
            using (var stream = File.CreateText(DataFilePath))
            {
                stream.Write(JsonConvert.SerializeObject(this,
                    new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }
                ).Replace(Assembly.GetExecutingAssembly().GetName().Name, "$ASSEMBLY_NAME"));
            }
        }
#endregion
    }
}
