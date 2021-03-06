﻿using FFmpegInterop;
using SubtitleEditor.ViewModel;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace SubtitleEditor.View
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    sealed partial class SubEventPage : Page
    {
        public SubEventPage()
        {
            this.InitializeComponent();
            var ioc = ViewModelLocator.GetForCurrentView();
            this.ViewModel = ioc.SubEventView;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        public SubEventViewModel ViewModel
        {
            get
            {
                return (SubEventViewModel)GetValue(ViewModelProperty);
            }
            set
            {
                SetValue(ViewModelProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SubEventViewModel), typeof(SubEventPage), new PropertyMetadata(null));
        FileOpenPicker f = new FileOpenPicker() {SuggestedStartLocation= PickerLocationId.Desktop,ViewMode= PickerViewMode.Thumbnail };

        Windows.Media.Editing.MediaComposition comp = new Windows.Media.Editing.MediaComposition();

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            f.FileTypeFilter.Add(".gif");
            var h = await f.PickSingleFileAsync();
            if(h == null)
                return;
            comp.Clips.Clear();
            comp.Clips.Add(await Windows.Media.Editing.MediaClip.CreateFromImageFileAsync(h,new TimeSpan(10000000)));
            media.SetMediaStreamSource(comp.GenerateMediaStreamSource());
            media.Play();
        }
    }
}
