using Myria.Wpf.Model;
using Myria.Lib.Core.Entities.Items;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Myria.Wpf.View.Selectors
{
    public class IconTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            if (container is FrameworkElement element)
            {
                var itemId = ResolveItemId(item);
                if (string.IsNullOrWhiteSpace(itemId))
                    return element.TryFindResource("Icon.Default") as DataTemplate;

                string resourceKey = $"Icon.{itemId}";

                object resource = element.TryFindResource(resourceKey);

                if (resource is DataTemplate template)
                {
                    return template;
                }

                return element.TryFindResource("Icon.Default") as DataTemplate;
            }

            return null;
        }

        private static string? ResolveItemId(object? item)
        {
            if (item == null)
                return null;

            return item switch
            {
                ItemVm itemVm => itemVm.Id,
                Item gameItem => gameItem.Id,
                _ => item.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item) as string
            };
        }
    }
}
