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
        #region Constants
        private static readonly string DefferedBindingName = typeof(InputDeferrer).FullName;
        private static readonly BindingMode[] DisallowedBindings = { BindingMode.OneWay, BindingMode.OneTime };
        #endregion Constants

        #region Dependency/Attached Properties
        private static readonly DependencyProperty OriginalBindingProperty = DependencyProperty.RegisterAttached(
            "OriginalBinding", typeof(BindingBase), typeof(UIElement), new PropertyMetadata(default(BindingBase)));

        private static void SetOriginalBinding(DependencyObject element, BindingBase value)
            => element.SetValue(OriginalBindingProperty, value);

        private static BindingBase GetOriginalBinding(DependencyObject element)
            => (BindingBase)element.GetValue(OriginalBindingProperty);

        private static readonly DependencyProperty DeferredValueProperty = DependencyProperty.RegisterAttached(
            "DeferredValue", typeof(object), typeof(UIElement), new PropertyMetadata(OnInputPropertyChanged));

        private static void SetDeferredValue(DependencyObject element, object value)
            => element.SetValue(DeferredValueProperty, value);

        private static object GetDeferredValue(DependencyObject element)
            => element.GetValue(DeferredValueProperty);

        public bool ElementsInFocus
        {
            get { return (bool)GetValue(ElementsInFocusProperty); }
            set { SetValue(ElementsInFocusProperty, value); }
        }

        public static readonly DependencyProperty ElementsInFocusProperty =
            DependencyProperty.Register(nameof(ElementsInFocus), typeof(bool), typeof(InputDeferrer), new PropertyMetadata(OnInputFocusChanged));

        public static readonly RoutedEvent ApplyChangesEvent = EventManager.RegisterRoutedEvent(
            nameof(ApplyChanges), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InputDeferrer));

        public event RoutedEventHandler ApplyChanges
        {
            add { AddHandler(ApplyChangesEvent, value); }
            remove { RemoveHandler(ApplyChangesEvent, value); }
        }

        public Style ApplyButtonStyle
        {
            get { return (Style)GetValue(ApplyButtonStyleProperty); }
            set { SetValue(ApplyButtonStyleProperty, value); }
        }

        public static readonly DependencyProperty ApplyButtonStyleProperty =
            DependencyProperty.Register(nameof(ApplyButtonStyle), typeof(Style), typeof(InputDeferrer), new PropertyMetadata());

        public object ApplyButtonContent
        {
            get { return (object)GetValue(ApplyButtonContentProperty); }
            set { SetValue(ApplyButtonContentProperty, value); }
        }

        public static readonly DependencyProperty ApplyButtonContentProperty =
            DependencyProperty.Register(nameof(ApplyButtonContent), typeof(object), typeof(InputDeferrer), new PropertyMetadata(null));
        #endregion Attached Properties

        private static void OnInputFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

        private static void OnInputPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var textBox = (TextBox)d;

            if (e.OldValue != e.NewValue && null != e.NewValue && null != e.OldValue)
            {
                //var deferredControl = (InputDeferrer)textBox.GetValue(InputDeferrerProperty);

                //deferredControl.ContentChanged();
            }
        }

        #region Members
        private Button _applyButton;
        private readonly HashSet<DeferredElement> _elementsBeingTracked = new HashSet<DeferredElement>();
        #endregion Members

        #region Constructors
        static InputDeferrer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InputDeferrer), new FrameworkPropertyMetadata(typeof(InputDeferrer)));
        }

        public InputDeferrer()
        {
            ApplyButtonContent = "Apply";
        }
        #endregion Constructors


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var button = (Button)GetTemplateChild("PART_ApplyButton");

            if (null != _applyButton)
            {
                _applyButton.Click -= ApplyButtonOnClick;
            }

            _applyButton = button;

            _applyButton.Focusable = false;
            //_applyButton.Click += ApplyButtonOnClick;

            UntrackFocusedElements();

            TrackFocusedElements();
        }


        private void ApplyButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
            => SubmitDeferredValue();



        private void SubmitDeferredValue(DeferredElement element)
        {
            var value = element.GetValue();

            RestoreOriginalBinding(element);

            var expression = BindingOperations.GetBindingExpression(element.Element, element.DeferredProperty);

            element.SetValue(value);

            expression?.UpdateSource();

            //TODO: Check if this condiction is needed
            if (ElementsInFocus)
            {
                SetDeferringBinding(element);
            }
        }

        private void SetDeferringBinding(DeferredElement element)
        {
            SetOriginalBinding(element.Element, CreateDeferredBinding(element));

            var value = element.GetValue();

            BindingOperations.ClearBinding(element.Element, element.DeferredProperty);

            element.SetValue(value);

            BindingOperations.SetBinding(element.Element, element.DeferredProperty, CreateDeferredBinding(element));
        }

        private void RestoreOriginalBinding(DeferredElement element)
        {
            BindingOperations.ClearBinding(element.Element, DeferredValueProperty);

            BindingOperations.SetBinding(element.Element, element.DeferredProperty, GetOriginalBinding(element.Element));
        }

        private Binding CreateDeferredBinding(DeferredElement element) 
            => CreateBinding(element.Element, element.DeferredProperty.Name, BindingMode.OneWay, true, true, UpdateSourceTrigger.PropertyChanged);

        private BindingBase GetExistingBinding(DeferredElement element) 
            => BindingOperations.GetBinding(element.Element, element.DeferredProperty) ??
                    (BindingBase)BindingOperations.GetMultiBinding(element.Element, element.DeferredProperty);

        private Binding CreateBinding(UIElement source, string path, BindingMode mode = BindingMode.OneWay, bool notifyOnSource = true, bool notifyOnTarget = true, UpdateSourceTrigger trigger = UpdateSourceTrigger.Default)
            => new Binding
            {
                Source = source,
                Path = new PropertyPath(path),
                Mode = mode,
                UpdateSourceTrigger = trigger,
                NotifyOnSourceUpdated = notifyOnSource,
                NotifyOnTargetUpdated = notifyOnTarget,
            };

        private void SubmitDeferredValue()
            => _elementsBeingTracked.ForEach(SubmitDeferredValue);

        private Binding CreateIsFocusedBinding(DeferredElement element)
            => CreateBinding(element.Element, nameof(IsFocused));

        private void StartDeferring() 
            => _elementsBeingTracked.ForEach(SetDeferringBinding);

        private void StopDeferring() 
            => _elementsBeingTracked.ForEach(RestoreOriginalBinding);

        private bool IsBeingDeferred(DeferredElement element)
            => _elementsBeingTracked.Contains(element);

        private void AddToTrackedElements(DeferredElement element)
            => _elementsBeingTracked.Add(element);

        private void UntrackFocusedElements()
        {
            _elementsBeingTracked.ForEach(RestoreOriginalBinding);

            _elementsBeingTracked.Clear();
        }

        private void TrackFocusedElements()
        {
            var multiBinding = new MultiBinding
            {
                BindingGroupName = DefferedBindingName,
                Converter = new IsFocusedConverter(),
                NotifyOnSourceUpdated = true,
                NotifyOnTargetUpdated = false,
            };

            (Content as Visual)
                ?.FindVisualChildren<UIElement>()
                .Where(DeferredElementResolver.IsTypeSupported)
                .Select(DeferredElementResolver.Resolve)
                .Where(IsQualifiedForDeferrer)
                .Do(AddToTrackedElements)
                .Select(CreateIsFocusedBinding)
                .ForEach(multiBinding.Bindings.Add);

            BindingOperations.SetBinding(this, ElementsInFocusProperty, multiBinding);
        }

        private bool IsQualifiedForDeferrer(DeferredElement element)
        {
            var binding = BindingOperations.GetBinding(element.Element, element.DeferredProperty);
            var multiBinding = BindingOperations.GetMultiBinding(element.Element, element.DeferredProperty);

            return (null != binding && !DisallowedBindings.Contains(binding.Mode) ||
                   (null != multiBinding && !DisallowedBindings.Contains(multiBinding.Mode)));
        }


        #region Private Classes
        private class IsFocusedConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
                => values.Any(value => (bool)value);

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class DeferredElement
        {
            public UIElement Element { get; }
            public DependencyProperty DeferredProperty { get; }

            public DeferredElement(UIElement element, DependencyProperty deferredProperty)
            {
                Element = element;
                DeferredProperty = deferredProperty;
            }

            public object GetValue() 
                => Element.GetValue(DeferredProperty);

            public void SetValue(object value)
                => Element.SetValue(DeferredProperty, value);

            public override bool Equals(object obj)
                => Element.Equals(obj);

            public override int GetHashCode()
                => Element.GetHashCode();
        }

        private static class DeferredElementResolver
        {
            public static readonly Dictionary<Type, DependencyProperty> SupportedTypes = new Dictionary<Type, DependencyProperty> { [typeof(TextBox)] = TextBox.TextProperty };

            public static bool IsTypeSupported(UIElement element)
                => SupportedTypes.ContainsKey(element.GetType());

            public static DeferredElement Resolve(UIElement element)
                => new DeferredElement(element, SupportedTypes[element.GetType()]);
        }
        #endregion Private Classes        
    }
}
