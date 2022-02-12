using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using TimetableApp.Core;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Uno.Extras;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;

namespace TimetableApp.ViewModels
{
    [Windows.UI.Xaml.Data.Bindable]
    public class HomePageViewModel : BindableBase
    {
        private const string NoClasses = "**HOORAY! NO CLASSES!**";
        private ThreadPoolTimer periodicTimer;
        private Task periodicTask;
        private CancellationTokenSource periodicTaskCancellationTokenSource = new CancellationTokenSource();
        private DateTime lastCheck = DateTime.Now;
        private Lesson currentLesson;
        private Lesson nextLesson;

        #region Settings
        private bool DoAutoJoin => Settings.AutoJoin;
        private string displayName;
        public string DisplayName
        {
            get => Settings.UserName;
            set
            {
                System.Diagnostics.Debug.WriteLine(displayName);
                Settings.UserName = value;
                SetProperty(ref displayName, value);
            }
        }
        private TimeSpan? ReportBeforeTime
        {
            get => Settings.AlwaysReport ? null : (TimeSpan?)Settings.ReportBeforeTime;
        }
        private TimeSpan? AllowJoinBeforeTime
        {
            get => Settings.AlwaysAllowJoin ? null : (TimeSpan?)Settings.AllowJoinBeforeTime;
        }
        #endregion
        #region Properites
        private string displayText = "Your next lesson";
        public string DisplayText
        {
            get => displayText;
            set => SetProperty(ref displayText, value);
        }
        private string lessonInfoText = NoClasses;
        public string LessonInfoText
        {
            get => lessonInfoText;
            set => SetProperty(ref lessonInfoText, value);
        }
        private bool joinButtonIsEnabled = false;
        public bool JoinButtonIsEnabled
        {
            get => joinButtonIsEnabled;
            set => SetProperty(ref joinButtonIsEnabled, value);
        }
        private ObservableCollection<Lesson> todayLessons = new ObservableCollection<Lesson>();
        public ObservableCollection<Lesson> TodayLessons
        {
            get => todayLessons;
            set => SetProperty(ref todayLessons, value);
        }
        private ObservableCollection<Lesson> tomorrowLessons = new ObservableCollection<Lesson>();
        public ObservableCollection<Lesson> TomorrowLessons
        {
            get => tomorrowLessons;
            set => SetProperty(ref tomorrowLessons, value);
        }
        private CollectionViewSource weekLessons = new CollectionViewSource();
        public CollectionViewSource WeekLessons
        {
            get => weekLessons;
            set => SetProperty(ref weekLessons, value);
        }
        #endregion

        public HomePageViewModel()
        {
            periodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) => Periodic(), TimeSpan.FromSeconds(1));
            Data.Timetable.OnSucessfulUpdate += (sender, args) => { RunOnMainThreadAsync(ReloadTabs); };
            RunOnMainThreadAsync(ReloadTodayAndTomorrow);
            RunOnMainThreadAsync(ReloadThisWeek);
        }

        private async Task RunOnMainThreadAsync(Action a)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => a());
        }

        private async Task Periodic()
        {
            var currentCheck = DateTime.Now;
            await RunOnMainThreadAsync(() => System.Diagnostics.Debug.WriteLine("Checking..."));

            if (periodicTaskCancellationTokenSource.Token.IsCancellationRequested) return;

            SetCurrentLesson(Data.Timetable.GetCurrentLesson());

            if (currentLesson == null)
            {   
                nextLesson = Data.Timetable.GetNextLesson(ReportBeforeTime);
                await RunOnMainThreadAsync(() => DisplayText = "Your next lesson");
            }
            else
            {               
                await RunOnMainThreadAsync(() => DisplayText = "Your current lesson");
            }

            await RunOnMainThreadAsync(() =>
            {
                LessonInfoText = currentLesson?.ToMarkdown() ?? (nextLesson?.ToMarkdown() ?? NoClasses);
                JoinButtonIsEnabled = (currentLesson != null) || (nextLesson != null && Data.Timetable.CheckNextLesson(AllowJoinBeforeTime));
            });

            if (lastCheck.DayOfYear != currentCheck.DayOfYear)
            {
                await RunOnMainThreadAsync(ReloadTodayAndTomorrow);
            }
            lastCheck = currentCheck;
        }

        private void SetCurrentLesson(Lesson newLesson)
        {
            if (currentLesson != newLesson) 
            {
                currentLesson = newLesson;
                if (DoAutoJoin) AutoJoin();
                if (currentLesson != null)
                {
                    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                    var JsonString = JsonConvert.SerializeObject(currentLesson, settings);
                    var notification = new ToastNotification();
                    notification.Title = "New Lesson";
                    notification.Message = $"Lesson {currentLesson.Subject} has started on {currentLesson.StartTime}";
                    notification.AddButton(new ToastButton()
                        .SetContent("Join")
                        .AddArgument("lesson", JsonString)
                        );
                    notification.AddButton(new ToastButton()
                        .SetContent("Dismiss")
                        .SetDismissActivation()
                        );
                    notification.Show();
                }
            }
        }

        private void AutoJoin()
        {
            if (currentLesson == null) return;
            if (currentLesson.Credentials.SupportsOneClick)
            {
                currentLesson.Credentials.EnterClass(new StudentInfo() { Name = DisplayName });
            }
        }

        public void ReloadTodayAndTomorrow()
        {
            var day = (int)DateTime.Now.DayOfWeek;
            todayLessons.Clear();
            foreach (var l in Data.Timetable.Lessons[day])
            {
                todayLessons.Add(l);
            }
            tomorrowLessons.Clear();
            var nextDay = (day + 1) % Data.Timetable.Lessons.Length;
            foreach (var l in Data.Timetable.Lessons[nextDay])
            {
                tomorrowLessons.Add(l);
            }
        }

        public void ReloadThisWeek()
        {
            ObservableCollection<ObservableCollection<Lesson>> groups = new ObservableCollection<ObservableCollection<Lesson>>();
            for (int i = 0; i < Data.Timetable.Lessons.Length; ++i)
            {
                ObservableCollection<Lesson> info = new ObservableCollection<Lesson>();
                foreach (var lesson in Data.Timetable.Lessons[i])
                {
                    lesson.DayOfWeek = ((DayOfWeek)(i)).ToString();
                    info.Add(lesson);
                }
                if (info.Count == 0) continue;
                groups.Add(info);
            }

            WeekLessons = new CollectionViewSource()
            {
                IsSourceGrouped = true,
                Source = groups
            };
        }

        private void ReloadTabs()
        {
            ReloadTodayAndTomorrow();
            ReloadThisWeek();
        }

        public void TryEnterClass(StudentInfo info)
        {
            if (currentLesson == null)
            {
                nextLesson.EnterClass(info);
            }
            else
            {
                currentLesson.EnterClass(info);
            }
        }
        //protected void OnActivated(IActivatedEventArgs e)
        //{
        //    Console.WriteLine("Received signal.");
        //    Console.WriteLine(e.GetType().FullName);
        //    if (e.GetType() == typeof(ToastNotificationActivatedEventArgs))
        //    {
        //        Console.WriteLine(e is ToastNotificationActivatedEventArgs);
        //        var toastActivationArgs = (ToastNotificationActivatedEventArgs)e;
        //        ToastArguments args = ToastArguments.Parse(toastActivationArgs.Argument);

        //        var shouldPlay = args.Contains("ShouldPlay") ? Convert.ToBoolean(int.Parse(args["ShouldPlay"])) : false;

        //        var contentDialog = new ContentDialog();
        //        contentDialog.Content = shouldPlay ? "Have a nice day!" : "Hello from Toast!";
        //        contentDialog.PrimaryButtonText = "Close";

        //        _ = contentDialog.ShowAsync();
        //    }
        //}
        
    }
}
