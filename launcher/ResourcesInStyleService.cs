using Microsoft.UI.Xaml;

namespace ResourcesInStyle;

public class ResourcesInStyleService
{
	public static readonly DependencyProperty StyleResourcesProperty =
		DependencyProperty.RegisterAttached("StyleResources",
			typeof(ResourceDictionary),
			typeof(ResourcesInStyleService),
			new PropertyMetadata(default(ResourceDictionary), OnStyleResourcesChanged));

	private static void OnStyleResourcesChanged(DependencyObject ownerObject, DependencyPropertyChangedEventArgs eventArgs)
	{
		if (ownerObject is FrameworkElement owner)
		{
			ResourceDictionary styleResourcesDict = (ResourceDictionary) eventArgs.NewValue;
			ResourceDictionary resourcesDict = owner.Resources;

			foreach (var resource in styleResourcesDict)
			{
				resourcesDict.Add(resource);
			}
		}
	}

	public static ResourceDictionary GetStyleResources(DependencyObject ownerObject)
	{
		return (ResourceDictionary) ownerObject.GetValue(StyleResourcesProperty);
	}

	public static void SetStyleResources(DependencyObject ownerObject, ResourceDictionary styleResourcesDict)
	{
		ownerObject.SetValue(StyleResourcesProperty, styleResourcesDict);
	}
}
