using System.Windows;
using System.Windows.Input;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{

    public partial class UI : Window
    {
        private E3ApplicationInfo applicationInfo;

        public UI()
        {
            applicationInfo = new E3ApplicationInfo();
            InitializeComponent();
            MinHeight = Height;
            MinWidth = Width;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            if (applicationInfo.Status == SelectionStatus.Selected)
                richTextBox.AppendText(applicationInfo.MainWindowTitle);
            else
            {
                richTextBox.AppendText(applicationInfo.StatusReasonDescription);
                DoButton.IsEnabled = false;
            }
        }

        private void DoButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            new Script().Main(applicationInfo.ProcessId);
            Cursor = Cursors.Arrow;
        }
    }
}
