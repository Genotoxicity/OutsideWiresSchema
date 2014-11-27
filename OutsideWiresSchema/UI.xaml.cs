using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using KSPE3Lib;

namespace OutsideConnectionsSchema
{

    public partial class UI : Window
    {
        private E3ApplicationInfo applicationInfo;
        private ScriptType scriptType;

        public UI()
        {
            applicationInfo = new E3ApplicationInfo();
            InitializeComponent();
            MinHeight = Height;
            MinWidth = Width;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            scriptType = ScriptType.Scheme;
            if (applicationInfo.Status == SelectionStatus.Selected)
                richTextBox.AppendText(applicationInfo.MainWindowTitle);
            else
            {
                richTextBox.AppendText(applicationInfo.StatusReasonDescription);
                DoButton.IsEnabled = false;
            }
        }

        private void SetScriptType(RadioButton radioButton)
        {
            if (radioButton.Name.Equals("Scheme"))
                scriptType = ScriptType.Scheme;
            else
                scriptType = ScriptType.Enumeration;
        }

        private void DoButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            new Script().Main(applicationInfo.ProcessId, scriptType);
            Cursor = Cursors.Arrow;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetScriptType(sender as RadioButton);
        }

    }
}
