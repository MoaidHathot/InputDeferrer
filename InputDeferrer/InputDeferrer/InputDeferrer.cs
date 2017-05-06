using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace InputDeferrer
{
    [TemplatePart(Name = "PART_ApplyButton", Type = typeof(Button))]
    public class InputDeferrer : ContentControl
    {
        private static readonly string DefferedBindingName = typeof(InputDeferrer).FullName;
        private static readonly BindingMode[] DisallowedBindings = { BindingMode.OneWay, BindingMode.OneTime };

        #region Dependency/Attached Properties

        public static readonly RoutedEvent ApplyChangesEvent = EventManager.RegisterRoutedEvent(
            nameof(ApplyChanges), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InputDeferrer));

        public event RoutedEventHandler ApplyChanges
        {
            add { AddHandler(ApplyChangesEvent, value); }
            remove { RemoveHandler(ApplyChangesEvent, value); }
        }

        private static readonly DependencyProperty OriginalBindingProperty = DependencyProperty.RegisterAttached(
            "OriginalBinding", typeof(BindingBase), typeof(UIElement), new PropertyMetadata(default(BindingBase)));

        private static void SetOriginalBinding(DependencyObject element, BindingBase value)
            => element.SetValue(OriginalBindingProperty, value);

        private static BindingBase GetOriginalBinding(DependencyObject element)
            => (BindingBase)element.GetValue(OriginalBindingProperty);

        private static readonly DependencyProperty DeferredValueProperty = DependencyProperty.RegisterAttached(
            "DeferredValue", typeof(object), typeof(UIElement), new PropertyMetadata(OnPropertyChanged));

        private static void SetDeferredValue(DependencyObject element, object value)
            => element.SetValue(DeferredValueProperty, value);

        private static object GetDeferredValue(DependencyObject element)
            => element.GetValue(DeferredValueProperty);

        private static readonly DependencyProperty DeferredValueInitializedProperty = DependencyProperty.RegisterAttached(
            "DeferredValueInitialized", typeof(bool), typeof(InputDeferrer), new PropertyMetadata(default(bool)));

        private static void SetDeferredValueInitialized(DependencyObject element, bool value)
            => element.SetValue(DeferredValueInitializedProperty, value);

        private static bool GetDeferredValueInitialized(DependencyObject element)
            => (bool)element.GetValue(DeferredValueInitializedProperty);

        private static readonly DependencyProperty InputDeferrerProperty = DependencyProperty.RegisterAttached(
            "InputDeferrer", typeof(InputDeferrer), typeof(InputDeferrer), new PropertyMetadata(default(InputDeferrer)));

        private static void SetInputDeferrer(DependencyObject element, InputDeferrer value)
            => element.SetValue(InputDeferrerProperty, value);

        private static InputDeferrer GetInputDeferrer(DependencyObject element)
            => (InputDeferrer)element.GetValue(InputDeferrerProperty);

        private bool ChangesAvailable
        {
            get { return (bool)GetValue(ChangesAvailableProperty); }
            set { SetValue(ChangesAvailableProperty, value); }
        }

        private static readonly DependencyProperty ChangesAvailableProperty =
            DependencyProperty.Register(nameof(ChangesAvailable), typeof(bool), typeof(InputDeferrer), new PropertyMetadata(default(bool)));


        public Style ApplyButtonStyle
        {
            get { return (Style)GetValue(ApplyButtonStyleProperty); }
            set { SetValue(ApplyButtonStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ApplyButton.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ApplyButtonStyleProperty =
            DependencyProperty.Register(nameof(ApplyButtonStyle), typeof(Style), typeof(InputDeferrer), new PropertyMetadata());


        public bool ElementsInFocus
        {
            get { return (bool)GetValue(ElementsInFocusProperty); }
            set { SetValue(ElementsInFocusProperty, value); }
        }

        public static readonly DependencyProperty ElementsInFocusProperty =
            DependencyProperty.Register(nameof(ElementsInFocus), typeof(bool), typeof(InputDeferrer), new PropertyMetadata(OnInputOnFocus));

        private static void OnInputOnFocus(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (InputDeferrer)d;

            if ((bool)e.NewValue)
            {
                control.StartDeferring();
            }
            else
            {
                control.StopDeferring();

            }

        }

        #endregion Attached Properties

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBox)d;

            if (!GetDeferredValueInitialized(textBox))
            {
                SetDeferredValueInitialized(textBox, true);
                return;
            }

            if (e.OldValue != e.NewValue && null != e.NewValue && null != e.OldValue)
            {
                var deferredControl = (InputDeferrer)textBox.GetValue(InputDeferrerProperty);

                deferredControl.ContentChanged();
            }
        }

        private Button _applyButton;
        //private ICommand _originalButtonCommand;
        //private readonly ButtonCommand _deferredButtonCommand;

        static InputDeferrer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InputDeferrer), new FrameworkPropertyMetadata(typeof(InputDeferrer)));
        }

        public InputDeferrer()
        {
            //_deferredButtonCommand = new ButtonCommand(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //var visualContent = Content as Visual;

            //var buttons = visualContent.FindVisualChildren<Button>().ToArray();

            var button = (Button)GetTemplateChild("PART_ApplyButton");
            //SetValue(ApplyButtonStyleProperty, applyButton);

            if (null != _applyButton)
            {
                _applyButton.Click -= ApplyButtonOnClick;
            }

            _applyButton = button;
            
            _applyButton.Focusable = false;
            _applyButton.Click += ApplyButtonOnClick;

            //_applyButton.Command = _deferredButtonCommand;
            //if (1 == buttons.Length)
            //{
            //_applyButton = buttons.First();
            //_originalButtonCommand = _applyButton.Command;
            //_applyButton.Command = _deferredButtonCommand;
            //_applyButton.Focusable = false;
            //}

            var multiBinding = new MultiBinding
            {
                BindingGroupName = DefferedBindingName,
                Converter = new IsFocusedConverter(),
                NotifyOnSourceUpdated = true,
                NotifyOnTargetUpdated = false,
            };

            (Content as Visual)
                ?.FindVisualChildren<TextBox>()
                .Where(HasQualifiedBinding)
                .Select(CreateIsFocusedBinding)
                .ForEach(multiBinding.Bindings.Add);

            BindingOperations.SetBinding(this, ElementsInFocusProperty, multiBinding);
        }

        private void ApplyButtonOnClick(object sender, RoutedEventArgs routedEventArgs) 
            => ApplyNewContent();

        private void StartDeferring()
        {
            (Content as Visual)
                ?.FindVisualChildren<TextBox>()
                .Where(HasQualifiedBinding)
                .ForEach(ChangeToDeferredBinding);
        }

        private void StopDeferring()
        {
            (Content as Visual)
                ?.FindVisualChildren<TextBox>()
                .Where(IsBeingDeferred)
                .ForEach(RestoreOriginalBinding);
        }

        private Binding CreateIsFocusedBinding(TextBox box)
            => new Binding
            {
                Source = box,
                Path = new PropertyPath(nameof(IsFocused)),
                Mode = BindingMode.OneWay,
                NotifyOnSourceUpdated = true,
            };

        private bool HasQualifiedBinding(TextBox element)
        {
            var binding = BindingOperations.GetBinding(element, TextBox.TextProperty);
            var multiBinding = BindingOperations.GetMultiBinding(element, TextBox.TextProperty);

            return (null != binding && !DisallowedBindings.Contains(binding.Mode) ||
                   (null != multiBinding && !DisallowedBindings.Contains(multiBinding.Mode)));
        }

        private void ChangeToDeferredBinding(TextBox element)
        {
            var originalBinding = BindingOperations.GetBinding(element, TextBox.TextProperty) ?? (BindingBase)BindingOperations.GetMultiBinding(element, TextBox.TextProperty);

            SetOriginalBinding(element, originalBinding);
            SetInputDeferrer(element, this);

            var newBinding = CreateNewBinding(element);

            var text = (string)GetDeferredValue(element) ?? element.Text;

            if (text != element.Text && !ChangesAvailable)
            {
                text = element.Text;
            }

            //BindingOperations.ClearBinding(element, DeferredValueProperty);
            BindingOperations.ClearBinding(element, TextBox.TextProperty);

            element.SetValue(TextBox.TextProperty, text);

            //TODO: check if can remove the clear.
            BindingOperations.SetBinding(element, DeferredValueProperty, newBinding);
        }

        private Binding CreateNewBinding(TextBox box)
            => new Binding
            {
                Source = box,
                Path = new PropertyPath(nameof(TextBox.Text)),
                Mode = BindingMode.OneWay,
                NotifyOnSourceUpdated = true,
                NotifyOnTargetUpdated = true,
                BindingGroupName = DefferedBindingName,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };



        private void RestoreOriginalBinding(TextBox element)
        {
            var originalBinding = GetOriginalBinding(element);

            var text = element.Text;

            var deferredValue = GetDeferredValue(element);

            BindingOperations.ClearBinding(element, DeferredValueProperty);
            BindingOperations.SetBinding(element, TextBox.TextProperty, originalBinding);

            SetDeferredValue(element, deferredValue);

            element.Text = text;

            //var expression = BindingOperations.GetBindingExpression(element, TextBox.TextProperty);
            //expression?.UpdateTarget();

            if (!ChangesAvailable)
            {
                SetDeferredValue(element, null);
            }
        }

        private void SubmitToSource(TextBox element)
        {
            var originalBinding = GetOriginalBinding(element);

            var value = element.Text;

            BindingOperations.ClearBinding(element, DeferredValueProperty);

            BindingOperations.SetBinding(element, TextBox.TextProperty, originalBinding);

            var expression = BindingOperations.GetBindingExpression(element, TextBox.TextProperty);

            element.SetValue(TextBox.TextProperty, value);

            expression?.UpdateSource();

            SetDeferredValue(element, null);

            ChangeToDeferredBinding(element);

            ChangesAvailable = false;
        }

        private void ContentChanged()
        {
            //_deferredButtonCommand.CanExecuteValue = true;
            ChangesAvailable = true;
        }

        private bool IsBeingDeferred(TextBox box)
            => null != GetInputDeferrer(box);

        private void ApplyNewContent()
        {
            (Content as Visual)
                ?.FindVisualChildren<TextBox>()
                .Where(IsBeingDeferred)
                .ForEach(SubmitToSource);

            RaiseEvent(new RoutedEventArgs(ApplyChangesEvent));
        }        

        private class IsFocusedConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
                => values.Any(value => (bool)value);

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
