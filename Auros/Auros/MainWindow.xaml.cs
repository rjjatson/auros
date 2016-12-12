using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace Auros
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ClearElements();
            //serialOutput.Text = "opening serial port";
            //Serial serialPort = new Serial();
            //serialPort.readPort();
            //serialOutput.Text = "closing serial port";
        }
        
        private void ClearElements()
        {
            UIElementCollection clearlist = ListGrid.Children;
            foreach(UIElement c in clearlist){ c.Visibility = Visibility.Collapsed; }
            clearlist = null;
            clearlist = ContentGrid.Children;
            foreach (UIElement c in clearlist) { c.Visibility = Visibility.Collapsed; }
        }

        private void Train_Click(object sender, RoutedEventArgs e)
        {
            ClearElements();
            ListTrainGrid.Visibility = Visibility.Visible;
            ContentTrainGrid.Visibility = Visibility.Visible;
        }

        private void Score_Click(object sender, RoutedEventArgs e)
        {
            ClearElements();
            ListScoreGrid.Visibility = Visibility.Visible;
            ContentScoreGrid.Visibility = Visibility.Visible;
        }

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            ClearElements();
            ListReportGrid.Visibility = Visibility.Visible;
            ContentReportGrid.Visibility = Visibility.Visible;
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ClearElements();
            ListSettingGrid.Visibility = Visibility.Visible;
            ContentSettingGrid.Visibility = Visibility.Visible;
        }
    }
}
