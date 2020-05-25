using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;

namespace DDCImprover.WPF
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        private static readonly CultureInfo FormatCulture = CultureInfo.CurrentCulture;

        #region FormatString

        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        public static readonly DependencyProperty StringFormatProperty =
            DependencyProperty.Register(nameof(StringFormat), typeof(string), typeof(NumericUpDown), new PropertyMetadata(null, OnFormatStringChanged));

        private static void OnFormatStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            string newFormatString = (string)args.NewValue;
            if (d is NumericUpDown nud)
            {
                if (string.IsNullOrEmpty(newFormatString))
                    nud.txtNum.Text = nud.Value.ToString(FormatCulture);
                else
                    nud.txtNum.Text = nud.Value.ToString(newFormatString, FormatCulture);
            }
        }

        #endregion

        #region DecimalPlaces

        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumericUpDown), new PropertyMetadata(0), new ValidateValueCallback(ValidateDecimalPlaces));

        private static bool ValidateDecimalPlaces(object value)
            => Convert.ToInt32(value) >= 0;

        #endregion

        #region Maximum

        public decimal Maximum
        {
            get => (decimal)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(decimal), typeof(NumericUpDown), new FrameworkPropertyMetadata(100M, new PropertyChangedCallback(OnMaximumChanged), new CoerceValueCallback(CoerceMaximum)));

        private static object CoerceMaximum(DependencyObject d, object baseValue)
        {
            decimal newValue = (decimal)baseValue;
            if (d is NumericUpDown nud && newValue < nud.Minimum)
                return nud.Minimum;

            return baseValue;
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => d.CoerceValue(ValueProperty);

        #endregion

        #region Minimum

        public decimal Minimum
        {
            get => (decimal)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(decimal), typeof(NumericUpDown), new FrameworkPropertyMetadata(0M, new PropertyChangedCallback(OnMinimumChanged), new CoerceValueCallback(CoerceMinimum)));

        private static object CoerceMinimum(DependencyObject d, object baseValue)
        {
            decimal newValue = (decimal)baseValue;
            if (d is NumericUpDown nup && newValue > nup.Maximum)
                    return nup.Maximum;

            return baseValue;
        }

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(MaximumProperty);
            d.CoerceValue(ValueProperty);
        }

        #endregion

        #region Value

        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(decimal), typeof(NumericUpDown), new FrameworkPropertyMetadata(0M, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue, false, UpdateSourceTrigger.Explicit));

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            NumericUpDown? nud = d as NumericUpDown;
            decimal newValue;
            if (nud!.DecimalPlaces == 0)
                newValue = Math.Truncate((decimal)baseValue);
            else
                newValue = Math.Round((decimal)baseValue, nud.DecimalPlaces, MidpointRounding.AwayFromZero);

            if (newValue < nud.Minimum)
            {
                return nud.Minimum;
            }
            if (newValue > nud.Maximum)
            {
                return nud.Maximum;
            }

            return newValue;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is NumericUpDown nud)
            {
                nud.txtNum.Text = ((decimal)args.NewValue).ToString($"F{nud.DecimalPlaces}");

                nud.GetBindingExpression(ValueProperty)?.UpdateSource();
            }
        }

        #endregion

        #region Increment

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(nameof(Increment), typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(1M));

        public decimal Increment
        {
            get => (decimal)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        #endregion

        public NumericUpDown()
        {
            InitializeComponent();
        }

        private void Up_Click(object sender, RoutedEventArgs e)
            => Value += Increment;

        private void Down_Click(object sender, RoutedEventArgs e)
            => Value -= Increment;

        private void NumTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ParseAndUpdateValue();
        }

        private void ParseAndUpdateValue()
        {
            decimal.TryParse(txtNum.Text, NumberStyles.Float, FormatCulture, out decimal newVal);
            if (newVal < Minimum)
                newVal = Minimum;
            if (newVal > Maximum)
                newVal = Maximum;
            Value = newVal;
            txtNum.Text = string.IsNullOrEmpty(StringFormat) ? Value.ToString(FormatCulture) : Value.ToString(StringFormat, FormatCulture);
        }

        private void NumTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    cmdDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Up:
                    cmdUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Return:
                    ParseAndUpdateValue();
                    break;
            }
        }

        private void UserControl_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == this)
            {
                txtNum.Focus();
                e.Handled = true;
            }
        }

        // Filter non-numeric characters
        private void NumTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string pattern = "[0-9";
            if (Minimum < 0)
                pattern += "-";
            if (DecimalPlaces > 0)
                pattern += ".,"; //CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            pattern += "]";

            if (!Regex.IsMatch(e.Text, pattern))
                e.Handled = true;
        }
    }
}
