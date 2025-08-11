using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Demo.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        DispatcherTimer timer;
        public MainWindow()
        {
            this.InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            danmaku.DanmakuStyle = NSDanmaku.Model.DanmakuBorderStyle.Stroke;
        }

        private async void btn_AddRoll_Click(object sender, RoutedEventArgs e)
        {
            await danmaku.AddScrollDanmu(new NSDanmaku.Model.DanmakuModel()
            {
                color = Colors.White,
                location = NSDanmaku.Model.DanmakuLocation.Scroll,
                size = 25,
                text = text.Text
            }, ck_own.IsChecked.Value);
        }

        private async void btn_AddTop_Click(object sender, RoutedEventArgs e)
        {
            await danmaku.AddTopDanmu(new NSDanmaku.Model.DanmakuModel()
            {
                color = Colors.Blue,
                location = NSDanmaku.Model.DanmakuLocation.Scroll,
                size = 25,
                text = text.Text
            }, ck_own.IsChecked.Value);
        }

        private async void btn_AddBottom_Click(object sender, RoutedEventArgs e)
        {
            await danmaku.AddBottomDanmu(new NSDanmaku.Model.DanmakuModel()
            {
                color = Colors.Red,
                location = NSDanmaku.Model.DanmakuLocation.Scroll,
                size = 25,
                text = text.Text
            }, ck_own.IsChecked.Value);
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            danmaku.ClearAll();
        }
        private async void Timer_Tick(object sender, object e)
        {
            var danmu = danmakus.Where(x => Convert.ToInt32(x.time) == slider.Value);
            foreach (var item in danmu)
            {
                try
                {
                    await danmaku.AddDanmu(item, false);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Can't add danmaku:" + item.source);
                }
            }
            slider.Value++;
        }
        List<NSDanmaku.Model.DanmakuModel> danmakus;
        private async void btn_Play_Click(object sender, RoutedEventArgs e)
        {
            if (danmakus == null)
            {
                try
                {
                    NSDanmaku.WinUI.Helper.DanmakuParse danmakuParse = new NSDanmaku.WinUI.Helper.DanmakuParse();
                    danmakus = await danmakuParse.ParseBiliBili(11311248);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Can't load danmaku");
                    return;
                }

            }
            danmaku.ResumeDanmaku();
            timer.Start();
        }

        private void btn_Pause_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            danmaku.PauseDanmaku();

        }

        private void btn_GetAll_Click(object sender, RoutedEventArgs e)
        {
            var ls = danmaku.GetDanmakus();
            Debug.WriteLine("Count:" + ls.Count);
        }

        private void ck_HideRoll_Checked(object sender, RoutedEventArgs e)
        {
            danmaku.HideDanmaku(NSDanmaku.Model.DanmakuLocation.Scroll);
        }

        private void ck_HideRoll_Unchecked(object sender, RoutedEventArgs e)
        {
            danmaku.ShowDanmaku(NSDanmaku.Model.DanmakuLocation.Scroll);
        }
    }
}
