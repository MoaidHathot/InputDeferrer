using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace InputDeferrer
{
    public static class Extensions
    {
        public static IEnumerable<TChild> FindVisualChildren<TChild>(this DependencyObject @this)
            where TChild : class
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(@this); i++)
            {
                DependencyObject depChild = VisualTreeHelper.GetChild(@this, i);
                TChild child = depChild as TChild;
                if (child != null)
                {
                    yield return child;
                }

                foreach (TChild childOfChild in FindVisualChildren<TChild>(depChild))
                {
                    yield return childOfChild;
                }
            }
        }

        public static void ForEach<IItem>(this IEnumerable<IItem> items, Action<IItem> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }

        public static IEnumerable<IItem> Do<IItem>(this IEnumerable<IItem> items, Action<IItem> action)
        {
            foreach (var item in items)
            {
                action(item);
                yield return item;
            }
        }
    }
}
