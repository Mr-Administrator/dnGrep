﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Search;
using File = Alphaleonis.Win32.Filesystem.File;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for PreviewControl.xaml
    /// </summary>
    public partial class PreviewControl : UserControl
    {
        private readonly SearchPanel searchPanel;

        public PreviewControl()
        {
            InitializeComponent();

            DataContext = ViewModel;

            searchPanel = SearchPanel.Install(textEditor);
            searchPanel.MarkerBrush = Application.Current.Resources["Match.Highlight.Background"] as Brush;

            ViewModel.ShowPreview += ViewModel_ShowPreview;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool?>(GrepSettings.Key.PreviewWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.PreviewWindowFont);

            AppTheme.Instance.CurrentThemeChanged += (s, e) =>
            {
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
                textEditor.TextArea.TextView.LinkTextForegroundBrush = Application.Current.Resources["AvalonEdit.Link"] as Brush;
                searchPanel.MarkerBrush = Application.Current.Resources["Match.Highlight.Background"] as Brush;
            };
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.HighlightsOn))
            {
                for (int i = textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
                {
                    if (textEditor.TextArea.TextView.LineTransformers[i] is PreviewHighlighter)
                        textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
                }

                if (ViewModel.HighlightsOn && !ViewModel.HighlightDisabled && !ViewModel.IsPdf)
                    textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(ViewModel.GrepResult));
            }
        }

        public PreviewViewModel ViewModel { get; private set; } = new PreviewViewModel();

        void ViewModel_ShowPreview(object sender, ShowEventArgs e)
        {
            if (!e.ClearContent)
            {
                if (textEditor.IsLoaded)
                {
                    textEditor.ScrollTo(ViewModel.LineNumber, 0);
                }
            }
            else
            {
                textEditor.Clear();
                textEditor.Encoding = ViewModel.Encoding;
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
                textEditor.TextArea.TextView.LinkTextForegroundBrush = Application.Current.Resources["AvalonEdit.Link"] as Brush;
                for (int i = textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
                {
                    if (textEditor.TextArea.TextView.LineTransformers[i] is PreviewHighlighter)
                        textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
                }

                if (ViewModel.HighlightsOn && !ViewModel.HighlightDisabled && !ViewModel.IsPdf)
                    textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(ViewModel.GrepResult));

                try
                {
                    if (!ViewModel.IsLargeOrBinary)
                    {
                        if (!string.IsNullOrWhiteSpace(ViewModel.FilePath))
                        {
                            bool isRTL = Utils.IsRTL(ViewModel.FilePath, ViewModel.Encoding);
                            textEditor.FlowDirection = isRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

                            using (FileStream stream = File.Open(ViewModel.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                textEditor.Load(stream);
                            }
                            if (textEditor.IsLoaded)
                            {
                                textEditor.ScrollTo(ViewModel.LineNumber, 0);
                            }
                        }
                        else
                        {
                            //Title = "No files to preview.";
                            textEditor.Text = "";
                        }
                    }
                    //Show();
                }
                catch (Exception ex)
                {
                    textEditor.Text = "Error opening the file: " + ex.Message;
                }
            }
        }

        internal void SaveSettings()
        {
            GrepSettings.Instance.Set<bool?>(GrepSettings.Key.PreviewWindowWrap, cbWrapText.IsChecked);
            GrepSettings.Instance.Set<int>(GrepSettings.Key.PreviewWindowFont, (int)zoomSlider.Value);
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs args)
        {
            base.OnPreviewMouseWheel(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl))
            {
                zoomSlider.Value += (args.Delta > 0) ? 1 : -1;
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs args)
        {
            base.OnPreviewMouseDown(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (args.MiddleButton == MouseButtonState.Pressed)
                {
                    zoomSlider.Value = 12;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            textEditor.Load(ViewModel.FilePath);
            ViewModel.IsLargeOrBinary = false;
            textEditor.ScrollTo(ViewModel.LineNumber, 0);
        }
    }
}
