using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TimetableApp.Core;
using TimetableApp.UI;
using TimetableApp.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TimetableApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePageViewModel ViewModel { get; set; } = new HomePageViewModel();

        public HomePage()
        {
            this.InitializeComponent();

            #region #5760 Hack
#if HAS_UNO
            TodayDataGrid.LayoutUpdated += TodayDataGrid_LayoutUpdated;
            TomorrowDataGrid.LayoutUpdated += TomorrowDataGrid_LayoutUpdated;
            ThisWeekDataGrid.LayoutUpdated += ThisWeekDataGrid_LayoutUpdated;
#endif
            #endregion
        }

        #region 5760 Hack
#if HAS_UNO
        private void TodayDataGrid_LayoutUpdated(object sender, object e)
        {
            ForceRightEnabled(TodayDataGrid);
        }
        
        private void TomorrowDataGrid_LayoutUpdated(object sender, object e)
        {
            ForceRightEnabled(TomorrowDataGrid);
        }

        private void ThisWeekDataGrid_LayoutUpdated(object sender, object e)
        {
            ForceRightEnabled(ThisWeekDataGrid);
        }

        private void ForceRightEnabled(UIElement root)
        {
            foreach (var child in FindVisualChildren<FrameworkElement>(root))
            {
                child.AddHandler(UIElement.RightTappedEvent, (RightTappedEventHandler)((s, a) => { /*nop*/ }), /*handledEventsToo*/ true);
            }
        }
#endif
        #endregion

        public void ThisWeekDataGrid_LoadingRowGroup(object sender, DataGridRowGroupHeaderEventArgs args)
        {
            ICollectionViewGroup group = args.RowGroupHeader.CollectionViewGroup;
            var item = group.GroupItems[0] as Lesson;
            args.RowGroupHeader.PropertyValue = item.DayOfWeek;
            args.RowGroupHeader.PropertyNameVisibility = Visibility.Collapsed;
            args.RowGroupHeader.ItemCountVisibility = Visibility.Collapsed;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void DataGrid_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            Func<Point, bool> CheckPositivePoint = (p) => p.X >= 0 && p.Y >= 0; 

            //Do it the WinForms way
            var candidates = FindVisualChildren<Grid>(sender as DataGrid)
                                .Where((child) => child.DataContext is Lesson)
                                .Where((child) => CheckPositivePoint(args.GetPosition(child)))
                                .OrderBy((child) => args.GetPosition(child).X)
                                .ThenBy((child) => args.GetPosition(child).Y);

            var source = candidates.FirstOrDefault();

            var lesson = (source as FrameworkElement)?.DataContext as Lesson;
            if (lesson == null)
            {
                ContextMenuStrip genericStrip = new ContextMenuStrip();
                genericStrip.Items.Add(new ToolStripMenuItem() { Text = "Not supported" });
                genericStrip.Show(sender as UIElement, args.GetPosition(sender as UIElement));
                return;
            }

            var dataGrid = sender as DataGrid;

            ContextMenuStrip tempStrip = new ContextMenuStrip();
            foreach (var item in lesson.Credentials.ToolStripMenuItems)
            {
                tempStrip.Items.Add(item);
            }
            tempStrip.Items.Add(new ToolStripSeparator());

            var joinToolStripMenuItem = new ToolStripMenuItem("Join");
            joinToolStripMenuItem.Click +=
                (send, arg) => lesson.EnterClass(new StudentInfo() { Name = textBox1.Text });
            tempStrip.Items.Add(joinToolStripMenuItem);

            tempStrip.Show(dataGrid, args.GetPosition(dataGrid));
        }

        private void JoinButton_Click(object sender, RoutedEventArgs args)
        {
            ViewModel.TryEnterClass(new StudentInfo { Name = textBox1.Text });
        }
    }
}
