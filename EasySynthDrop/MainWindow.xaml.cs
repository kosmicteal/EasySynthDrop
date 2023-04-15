using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;

namespace EasySynthDrop
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double animend;
        private double animstart;

        public class VBLite
        {
            //DB class
            public string Profile { get; set; }
            public string Name { get; set; }
            public string DragFile { get; set; }
        }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            //Find database information
            DatabaseFind();

            // Get scaling for animation support
            double WindowsUIScaling = VisualTreeHelper.GetDpi(this).DpiScaleX;
            int a = ReturnScaling(WindowsUIScaling, 1000);

            // Get screen width for animation support
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
            Screen currentScreen = Screen.FromHandle(windowInteropHelper.Handle);
            int screenwidth = currentScreen.WorkingArea.Width;

            //Window size
            MainUI.Width = Math.Max(ReturnScaling(WindowsUIScaling, screenwidth / 8), 240);
            MainUI.Height = ReturnScaling(WindowsUIScaling, currentScreen.WorkingArea.Height);

            //Position
            MainUI.Top = 0;

            //Animation values
            animend = ReturnScaling(WindowsUIScaling, screenwidth) - MainUI.Width;
            animstart = ReturnScaling(WindowsUIScaling, screenwidth);

            AnimateWindow(animstart, animend, false);

        }

        public int ReturnScaling(double percentage, int number)
        {
            return (int)(number / percentage);
        }


        private void CloseESD(object sender, RoutedEventArgs e)
        {
            File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"template.svp");
            AnimateWindow(animend, animstart, true);
        }

        private void AnimateWindow(double start, double end, bool close)
        {
            DoubleAnimation MainUIAnim =
            new DoubleAnimation(start, end, new Duration(new TimeSpan(0, 0, 0, 0, 400)));

            MainUIAnim.EasingFunction = new CircleEase();

            Storyboard.SetTarget(MainUIAnim, MainUI);
            Storyboard.SetTargetProperty(MainUIAnim, new PropertyPath(Window.LeftProperty));

            Storyboard story = new Storyboard();
            story.Children.Add(MainUIAnim);
            if (close) story.Completed += (o, s) => Close();
            story.Begin();
        }

        private void ConfigESD(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Properties.Settings.Default.databasepath = dialog.FileName;
                Properties.Settings.Default.Save();
            }
            DatabaseFind();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string drag_id = ((VBLite)((FrameworkElement)e.Source).DataContext).Name;
            //file to be created as a template
            string text = "{\"version\": 129, \"time\": {\"meter\": [{\"index\": 0, \"numerator\": 4, \"denominator\": 4}], " +
                " \"tempo\": [{\"position\": 0, \"bpm\": 120.0}]}, \"library\": [], \"tracks\": [{\"name\": \"Unnamed Track\", \"dispColor\": \"ff7db235\"," +
                " \"dispOrder\": -1, \"renderEnabled\": false, \"mixer\": {\"gainDecibel\": 0.0, \"pan\": 0.0, \"mute\": false, \"solo\": false, \"display\": true}," +
                " \"mainGroup\": {\"name\": \"main\", \"uuid\": \"\", \"parameters\": {\"pitchDelta\": {\"mode\": \"cubic\", \"points\": []}, \"vibratoEnv\": " +
                "{\"mode\": \"cubic\", \"points\": []}, \"loudness\": {\"mode\": \"cubic\", \"points\": []}, \"tension\": {\"mode\": \"cubic\", \"points\": []}," +
                " \"breathiness\": {\"mode\": \"cubic\", \"points\": []}, \"voicing\": {\"mode\": \"cubic\", \"points\": []}, \"gender\": {\"mode\": \"cubic\", " +
                "\"points\": []}, \"toneShift\": {\"mode\": \"cubic\", \"points\": []}}, \"notes\": []}, \"mainRef\": {\"groupID\": \"\", \"blickOffset\": 0, " +
                "\"pitchOffset\": 0, \"isInstrumental\": false, \"systemPitchDelta\": {\"mode\": \"cubic\", \"points\": []}, \"database\": {\"name\": \"" + drag_id +
                "\"}, \"dictionary\": \"\", \"voice\": {\"vocalModeInherited\": true, \"vocalModePreset\": \"\", \"vocalModeParams\": {}}, \"pitchTakes\": " +
                "{\"activeTakeId\": 0, \"takes\": [{\"id\": 0, \"expr\": 1.0, \"liked\": false}]}, \"timbreTakes\": {\"activeTakeId\": 0, \"takes\": [{\"id\": 0," +
                " \"expr\": 1.0, \"liked\": false}]}}, \"groups\": []}], \"renderConfig\": {\"destination\": \"./\", \"filename\": \"untitled\", \"numChannels\": 1," +
                " \"aspirationFormat\": \"noAspiration\", \"bitDepth\": 16, \"sampleRate\": 44100, \"exportMixDown\": true}, \"instantModeEnabled\": true}";
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"template.svp", text);

            string[] files = new String[1];
            files[0] = AppDomain.CurrentDomain.BaseDirectory + @"template.svp";

            DataObject data = new DataObject(DataFormats.FileDrop, files);
            data.SetData(DataFormats.StringFormat, files[0]);

            DragDrop.DoDragDrop((DependencyObject)e.Source, data, DragDropEffects.Copy);
        }

        private void DatabaseFind()
        {
            List<VBLite> item_banks = new List<VBLite>();

            string path = Properties.Settings.Default.databasepath;

            if (path.Length != 0)
            {
                var directories = Directory.GetDirectories(path);
                for (int i = 0; i < directories.Length; i++)
                {
                    VBLite aux = new VBLite();
                    aux.Profile = directories[i] + "\\profile.png";
                    aux.Name = directories[i].Remove(0, path.Length + 1).Replace("_", " ");
                    if (File.Exists(directories[i] + "\\voice.nofs"))
                    {
                        item_banks.Add(aux);
                    }
                }
                FirstTime.Opacity = 0;
                dc_banks.ItemsSource = item_banks;

                if (item_banks.Count == 0)
                {
                    FirstTime.Text = "Oops...\n\nNo SynthV databases found, try to search them again on Settings";
                    FirstTime.Opacity = 1;
                }
            }
            else
            {
                FirstTime.Text = "Welcome!\n\nClick on the Settings button to open your SynthV Databases folder";
                FirstTime.Opacity = 1;
            }

        }

    }

}
