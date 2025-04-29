using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RavenHub
{
    public class SidebarManager
    {
        private readonly Grid _mainGrid;
        private readonly StackPanel _buttonsPanel;
        private readonly string[] _textBlockNames = { "HomeText", "EquipmentText", "EmployeesText", "TasksText", "StatisticsText" };

        private const double ExpandedWidth = 260;
        private const double CollapsedWidth = 80;
        private bool _isExpanded = false;
        private bool _isAnimating = false;
        private readonly TimeSpan _animationDuration = TimeSpan.FromSeconds(0.3);

        public SidebarManager(Grid mainGrid, Border navPanel, StackPanel buttonsPanel)
        {
            _mainGrid = mainGrid ?? throw new ArgumentNullException(nameof(mainGrid));
            _buttonsPanel = buttonsPanel ?? throw new ArgumentNullException(nameof(buttonsPanel));

            _mainGrid.ColumnDefinitions[0].BeginAnimation(ColumnDefinition.WidthProperty, null);
            _mainGrid.ColumnDefinitions[0].Width = new GridLength(CollapsedWidth);
            InitializeTextBlocks();
        }

        public void CompleteInitialization()
        {
            _mainGrid.ColumnDefinitions[0].BeginAnimation(ColumnDefinition.WidthProperty, null);
            _mainGrid.ColumnDefinitions[0].Width = new GridLength(CollapsedWidth);
            InitializeTextBlocks();
        }

        public void InitializeTextBlocks()
        {
            foreach (var name in _textBlockNames)
            {
                var textBlock = GetTextBlock(name);
                if (textBlock != null)
                {
                    textBlock.BeginAnimation(UIElement.OpacityProperty, null);
                    textBlock.Opacity = 0;
                    textBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void ToggleSidebar()
        {
            if (_isExpanded)
            {
                CollapsePanel();
            }
            else
            {
                ExpandPanel();
            }
        }

        private void ExpandPanel()
        {
            if (_isAnimating || _isExpanded) return;
            _isAnimating = true;

            _mainGrid.ColumnDefinitions[0].BeginAnimation(ColumnDefinition.WidthProperty, null);
            _mainGrid.ColumnDefinitions[0].Width = new GridLength(CollapsedWidth);

            var widthAnimation = new GridLengthAnimation
            {
                From = new GridLength(CollapsedWidth),
                To = new GridLength(ExpandedWidth),
                Duration = new Duration(_animationDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            widthAnimation.Completed += (s, e) =>
            {
                _isAnimating = false;
                _isExpanded = true;
                _mainGrid.ColumnDefinitions[0].Width = new GridLength(ExpandedWidth);
            };
            _mainGrid.ColumnDefinitions[0].BeginAnimation(ColumnDefinition.WidthProperty, widthAnimation);

            foreach (var name in _textBlockNames)
            {
                var textBlock = GetTextBlock(name);
                if (textBlock != null)
                {
                    textBlock.BeginAnimation(UIElement.OpacityProperty, null);
                    textBlock.Visibility = Visibility.Visible;
                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = new Duration(_animationDuration),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    textBlock.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
                }
            }
        }

        private void CollapsePanel()
        {
            if (_isAnimating || !_isExpanded) return;
            _isAnimating = true;

            _mainGrid.ColumnDefinitions[0].BeginAnimation(ColumnDefinition.WidthProperty, null);
            _mainGrid.ColumnDefinitions[0].Width = new GridLength(ExpandedWidth);

            foreach (var name in _textBlockNames)
            {
                var textBlock = GetTextBlock(name);
                if (textBlock != null)
                {
                    textBlock.BeginAnimation(UIElement.OpacityProperty, null);
                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = new Duration(_animationDuration),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    opacityAnimation.Completed += (s, e) =>
                    {
                        if (textBlock.Opacity == 0)
                        {
                            textBlock.Visibility = Visibility.Collapsed;
                        }
                    };
                    textBlock.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
                }
            }

            var widthAnimation = new GridLengthAnimation
            {
                From = new GridLength(ExpandedWidth),
                To = new GridLength(CollapsedWidth),
                Duration = new Duration(_animationDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            widthAnimation.Completed += (s, e) =>
            {
                _isAnimating = false;
                _isExpanded = false;
                _mainGrid.ColumnDefinitions[0].Width = new GridLength(CollapsedWidth);
            };
            _mainGrid.ColumnDefinitions[0].BeginAnimation(ColumnDefinition.WidthProperty, widthAnimation);
        }

        private TextBlock GetTextBlock(string name)
        {
            return _buttonsPanel.FindName(name) as TextBlock ??
                   Application.Current.MainWindow.FindName(name) as TextBlock;
        }

        private class GridLengthAnimation : AnimationTimeline
        {
            public static readonly DependencyProperty FromProperty =
                DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));
            public static readonly DependencyProperty ToProperty =
                DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(GridLengthAnimation));

            public GridLength From
            {
                get => (GridLength)GetValue(FromProperty);
                set => SetValue(FromProperty, value);
            }

            public GridLength To
            {
                get => (GridLength)GetValue(ToProperty);
                set => SetValue(ToProperty, value);
            }

            public IEasingFunction EasingFunction
            {
                get => (IEasingFunction)GetValue(EasingFunctionProperty);
                set => SetValue(EasingFunctionProperty, value);
            }

            public override Type TargetPropertyType => typeof(GridLength);

            protected override Freezable CreateInstanceCore()
            {
                return new GridLengthAnimation();
            }

            public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
            {
                double fromValue = From.Value;
                double toValue = To.Value;
                double progress = animationClock?.CurrentProgress ?? 0;

                if (EasingFunction != null)
                {
                    progress = EasingFunction.Ease(progress);
                }

                double currentValue = fromValue + (toValue - fromValue) * progress;
                double clampedValue = Math.Max(CollapsedWidth, Math.Min(ExpandedWidth, currentValue));

                return new GridLength(clampedValue);
            }
        }
    }
}