using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Module.Shared.Converters
{
    /// <summary>
    /// BorderGapMaskConverter class
    /// </summary>
    public class BorderGapMaskConverter : IMultiValueConverter
    {

        /// <summary>
        /// Convert a value.
        /// </summary>
        /// <param name="values">values as produced by source binding</param>
        /// <param name="targetType">target type</param>
        /// <param name="parameter">converter parameter</param>
        /// <param name="culture">culture information</param>
        /// <returns>
        /// Converted value.
        /// Visual Brush that is used as the opacity mask for the Border
        /// in the style for GroupBox.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //
            // Parameter Validation
            //

            Type doubleType = typeof(double);

            if (parameter == null ||
                values == null ||
                values.Length != 3 ||
                values[0] == null ||
                values[1] == null ||
                values[2] == null ||
                !doubleType.IsAssignableFrom(values[0].GetType()) ||
                !doubleType.IsAssignableFrom(values[1].GetType()) ||
                !doubleType.IsAssignableFrom(values[2].GetType()))
            {
                return DependencyProperty.UnsetValue;
            }

            Type paramType = parameter.GetType();
            if (!(doubleType.IsAssignableFrom(paramType) || typeof(string).IsAssignableFrom(paramType)))
            {
                return DependencyProperty.UnsetValue;
            }

            //
            // Conversion
            //

            double headerWidth = (double)values[0];
            double borderWidth = (double)values[1];
            double borderHeight = (double)values[2];

            // Doesn't make sense to have a Grid
            // with 0 as width or height
            if (borderWidth == 0
                || borderHeight == 0)
            {
                return null;
            }

            // Width of the line to the left of the header
            // to be used to set the width of the first column of the Grid
            double lineWidth;
            if (parameter is string)
            {
                lineWidth = Double.Parse(((string)parameter), NumberFormatInfo.InvariantInfo);
            }
            else
            {
                lineWidth = (double)parameter;
            }

            Grid grid = new Grid();
            grid.Width = borderWidth;
            grid.Height = borderHeight;
            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            colDef1.Width = new GridLength(lineWidth);
            colDef2.Width = new GridLength(headerWidth);
            colDef3.Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions.Add(colDef1);
            grid.ColumnDefinitions.Add(colDef2);
            grid.ColumnDefinitions.Add(colDef3);
            RowDefinition rowDef1 = new RowDefinition();
            RowDefinition rowDef2 = new RowDefinition();
            rowDef1.Height = new GridLength(borderHeight / 2);
            rowDef2.Height = new GridLength(1, GridUnitType.Star);
            grid.RowDefinitions.Add(rowDef1);
            grid.RowDefinitions.Add(rowDef2);

            Rectangle rectColumn1 = new Rectangle();
            Rectangle rectColumn2 = new Rectangle();
            Rectangle rectColumn3 = new Rectangle();
            rectColumn1.Fill = Brushes.Black;
            rectColumn2.Fill = Brushes.Black;
            rectColumn3.Fill = Brushes.Black;

            Grid.SetRowSpan(rectColumn1, 2);
            Grid.SetRow(rectColumn1, 0);
            Grid.SetColumn(rectColumn1, 0);

            Grid.SetRow(rectColumn2, 1);
            Grid.SetColumn(rectColumn2, 1);

            Grid.SetRowSpan(rectColumn3, 2);
            Grid.SetRow(rectColumn3, 0);
            Grid.SetColumn(rectColumn3, 2);

            grid.Children.Add(rectColumn1);
            grid.Children.Add(rectColumn2);
            grid.Children.Add(rectColumn3);

            return (new VisualBrush(grid));
        }

        /// <summary>
        /// Not Supported
        /// </summary>
        /// <param name="value">value, as produced by target</param>
        /// <param name="targetTypes">target types</param>
        /// <param name="parameter">converter parameter</param>
        /// <param name="culture">culture information</param>
        /// <returns>Nothing</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing };
        }
    }
    
    public class LeftBorderGapMaskConverter : IMultiValueConverter
    {
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public LeftBorderGapMaskConverter()
        {
            //      base.ctor();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Type type1 = typeof(double);
            if (parameter == null
                || values == null
                || (values.Length != 3 || values[0] == null)
                || (values[1] == null
                    || values[2] == null
                    || (!type1.IsAssignableFrom(values[0].GetType())
                        || !type1.IsAssignableFrom(values[1].GetType())))
                || !type1.IsAssignableFrom(values[2].GetType()))
                return DependencyProperty.UnsetValue;

            Type type2 = parameter.GetType();
            if (!type1.IsAssignableFrom(type2)
                && !typeof(string).IsAssignableFrom(type2))
                return DependencyProperty.UnsetValue;

            double pixels1 = (double)values[0];
            double num1 = (double)values[1];
            double num2 = (double)values[2];
            if (num1 == 0.0 || num2 == 0.0)
                return (object)null;

            double pixels2 = !(parameter is string)
                ? (double)parameter
                : double.Parse((string)parameter, (IFormatProvider)NumberFormatInfo.InvariantInfo);

            Grid grid = new Grid();
            grid.Width = num1;
            grid.Height = num2;
            RowDefinition RowDefinition1 = new RowDefinition();
            RowDefinition RowDefinition2 = new RowDefinition();
            RowDefinition RowDefinition3 = new RowDefinition();
            RowDefinition1.Height = new GridLength(pixels2);
            RowDefinition2.Height = new GridLength(pixels1);
            RowDefinition3.Height = new GridLength(1.0, GridUnitType.Star);
            grid.RowDefinitions.Add(RowDefinition1);
            grid.RowDefinitions.Add(RowDefinition2);
            grid.RowDefinitions.Add(RowDefinition3);
            ColumnDefinition ColumnDefinition1 = new ColumnDefinition();
            ColumnDefinition ColumnDefinition2 = new ColumnDefinition();
            ColumnDefinition1.Width = new GridLength(num2 / 2.0);
            ColumnDefinition2.Width = new GridLength(1.0, GridUnitType.Star);
            grid.ColumnDefinitions.Add(ColumnDefinition1);
            grid.ColumnDefinitions.Add(ColumnDefinition2);
            Rectangle rectangle1 = new Rectangle();
            Rectangle rectangle2 = new Rectangle();
            Rectangle rectangle3 = new Rectangle();
            rectangle1.Fill = (Brush)Brushes.Black;
            rectangle2.Fill = (Brush)Brushes.Black;
            rectangle3.Fill = (Brush)Brushes.Black;

            Grid.SetColumnSpan((UIElement)rectangle1, 2);
            Grid.SetColumn((UIElement)rectangle1, 0);
            Grid.SetRow((UIElement)rectangle1, 0);
            Grid.SetColumn((UIElement)rectangle2, 1);
            Grid.SetRow((UIElement)rectangle2, 1);
            Grid.SetColumnSpan((UIElement)rectangle3, 2);
            Grid.SetColumn((UIElement)rectangle3, 0);
            Grid.SetRow((UIElement)rectangle3, 2);
            grid.Children.Add((UIElement)rectangle1);
            grid.Children.Add((UIElement)rectangle2);
            grid.Children.Add((UIElement)rectangle3);
            return (object)new VisualBrush((Visual)grid);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[1]
          {
            Binding.DoNothing
          };
        }
    }
}
