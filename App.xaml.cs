using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OSLearningSystem;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length > 0 && e.Args[0] == "--export-screenshots")
        {
            string outDir = e.Args.Length > 1 ? e.Args[1] : @"D:\Coursework_OS\images";
            Directory.CreateDirectory(outDir);

            var win = new MainWindow();
            win.Width = 1240;
            win.Height = 820;
            win.Show();
            win.UpdateLayout();

            if (win.Content is Grid rootGrid && rootGrid.Children.Count > 1 && rootGrid.Children[1] is TabControl tabControl)
            {
                void CaptureWindow(string filename)
                {
                    win.UpdateLayout();
                    var rtb = new RenderTargetBitmap((int)win.ActualWidth, (int)win.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(win);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    string path = Path.Combine(outDir, filename);
                    using var stream = File.Create(path);
                    encoder.Save(stream);
                    Console.WriteLine($"SUCCESS: Exported screenshot to {path}");
                }

                // 0. Home Dashboard (Tab 0)
                tabControl.SelectedIndex = 0;
                CaptureWindow("fig2_7_ui_home.png");

                // 1. CPU Scheduling (Tab 1)
                tabControl.SelectedIndex = 1;
                CaptureWindow("fig2_8_ui_cpu.png");

                // 2. Memory: Dynamic Allocation (Tab 2, SubTab 0)
                tabControl.SelectedIndex = 2;
                if (tabControl.SelectedContent is TabControl memTabCtrl)
                {
                    memTabCtrl.SelectedIndex = 0;
                    CaptureWindow("fig2_9_ui_memory.png");

                    // SubTab 1: Paging
                    memTabCtrl.SelectedIndex = 1;
                    CaptureWindow("fig2_9_2_ui_paging.png");
                }
                else
                {
                    CaptureWindow("fig2_9_ui_memory.png");
                }

                // 3. Deadlocks & Banker's Algorithm (Tab 3)
                tabControl.SelectedIndex = 3;
                CaptureWindow("fig2_10_ui_deadlocks.png");

                // 4. Knowledge Assessment Quiz (Tab 4)
                tabControl.SelectedIndex = 4;
                win.CompleteQuizForDemo();
                CaptureWindow("fig2_11_ui_quiz.png");

                // 5. Theory Handbook (Tab 5)
                tabControl.SelectedIndex = 5;
                CaptureWindow("fig2_12_ui_theory.png");

                // Reset back to Home
                tabControl.SelectedIndex = 0;
            }

            Shutdown(0);
        }
    }
}
