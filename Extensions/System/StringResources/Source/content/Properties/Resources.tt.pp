﻿<#@ Template Debug="False" Hostspecific="True" Language="C#" #>
<#@ Output Extension="Strings.cs" #>
<#@ Assembly Name="System.Core" #>
<#@ Assembly Name="System.Xml" #>
<#@ Assembly Name="System.Xml.Linq" #>
<#@ Import Namespace="System.Collections.Generic" #>
<#@ Import Namespace="System.IO" #>
<#@ Import Namespace="System" #>
<#@ Import Namespace="System.Linq" #>
<#@ Import Namespace="System.Diagnostics" #>
<#@ Import Namespace="System.Xml.Linq" #>
<#@ Import Namespace="System.Text.RegularExpressions" #>
<#
var targetNamespace = "$rootnamespace$.Properties";
var targetClassName = "Strings";
var resourceFile = Path.ChangeExtension(this.Host.TemplateFile, "resx");

if (!File.Exists(resourceFile))
	return "// Please create a " + Path.GetFileName(resourceFile) + " file.";

var rootArea = ResourceFile.Build(resourceFile, targetClassName);
#>
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Globalization;

namespace <#= targetNamespace #>
{<#
RenderArea(rootArea, "");
#>}

<#+
private void RenderMessageHint(string value)
{
	foreach (var line in value.Split(new [] { '\n' }, StringSplitOptions.None))
    {
	    PushIndent("\t");
#>
///	<#= line #>
<#+
        PopIndent();
    }
}

private void RenderArea(ResourceArea area, string visibility)
{
    PushIndent("\t");

#>

///	<summary>
///	Provides access to string resources.
///	</summary>
[global::System.CodeDom.Compiler.GeneratedCodeAttribute("netfx-System.Strings", "1.0.0.0")]
[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
<#=visibility #>static partial class <#=area.Name #>
{<#+
	foreach (var value in area.Values)
	{
		if (!value.HasFormat)
		{
#>

	/// <summary>
	/// Looks up a localized string similar to: 
<#+ RenderMessageHint(value.Value); #>
	/// </summary>
	public static string <#=value.Name #> { get { return Resources.<#=area.Prefix #><#=value.Name #>; } }
<#+
		}
		else
		{
#>

	/// <summary>
	/// Looks up a localized string similar to: 
<#+ RenderMessageHint(value.Value); #>
	/// </summary>
	public static string <#=value.Name #>(<#= string.Join(", ", value.FormatNames.Select(s => "object " + s)) #>)
	{
		return Resources.<#=area.Prefix #><#=value.Name #>.FormatWith(new 
		{
<#+
for (int i = 0; i < value.FormatNames.Count; i++)
{
#>
			<#=value.FormatNames[i] #> = <#=value.FormatNames[i]#>,
<#+
}
#>
		});
	}
<#+
		}
	}

	foreach (var nestedArea in area.NestedAreas)
	{
		RenderArea(nestedArea, "public ");
	}
#>
}
<#+
	PopIndent();
}

public static class ResourceFile
{
	private static Regex FormatExpression = new Regex("{(?<name>[^{}]+)}", RegexOptions.Compiled);

	public static ResourceArea Build(string fileName, string rootArea)
	{
		return Build(
			XDocument.Load(fileName)
				.Root.Elements("data")
				.Where(e => e.Attribute("type") == null),
			rootArea);
	}

	public static ResourceArea Build(IEnumerable<XElement> data, string rootArea)
	{
		var root = new ResourceArea { Name = rootArea, Prefix = "" };
		foreach (var element in data)
		{
			//  Splits: ([resouce area]_)*[resouce name]
			var nameAttribute = element.Attribute("name").Value;
			var valueElement = element.Element("value").Value;
			var areaParts = nameAttribute.Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
			if (areaParts.Length <= 1)
			{
				root.Values.Add(GetValue(nameAttribute, valueElement));
			}
			else
			{
				var area = GetArea(root, areaParts.Take(areaParts.Length - 1));
				var value = GetValue(areaParts.Skip(areaParts.Length - 1).First(), valueElement);

				area.Values.Add(value);
			}
		}

		SortArea(root);

		return root;
	}

	private static void SortArea(ResourceArea area)
	{
		area.Values.Sort((left, right) => left.Name.CompareTo(right.Name));
		foreach (var nested in area.NestedAreas)
		{
			SortArea(nested);
		}
	}

	private static ResourceArea GetArea(ResourceArea area, IEnumerable<string> areaPath)
	{
		var currentArea = area;
		foreach (var areaName in areaPath)
		{
			var existing = currentArea.NestedAreas.FirstOrDefault(a => a.Name == areaName);
			if (existing == null)
			{
				if (currentArea.Values.Any(v => v.Name == areaName))
					throw new ArgumentException(string.Format(
						"Area name '{0}' is already in use as a value name under area '{1}'.",
						areaName, currentArea.Name));

				existing = new ResourceArea { Name = areaName, Prefix = string.Join("_", areaPath) + "_" };
				currentArea.NestedAreas.Add(existing);
			}

			currentArea = existing;
		}

		return currentArea;
	}

	private static ResourceValue GetValue(string resourceName, string resourceValue)
	{
		var value = new ResourceValue { Name = resourceName, Value = resourceValue };

		value.HasFormat = FormatExpression.IsMatch(resourceValue);

		// Parse parameter names
		if (value.HasFormat)
		{
			var argIndex = 0;
			value.FormatNames.AddRange(FormatExpression
				.Matches(resourceValue)
				.OfType<Match>()
				.Select(match => int.TryParse(match.Groups["name"].Value, out argIndex) ? "arg" + match.Groups["name"].Value : match.Groups["name"].Value)
				.Distinct());
		}

		return value;
	}
}

[DebuggerDisplay("Name = {Name}, NestedAreas = {NestedAreas.Count}, Values = {Values.Count}")]
public class ResourceArea
{
	public ResourceArea()
	{
		this.NestedAreas = new List<ResourceArea>();
		this.Values = new List<ResourceValue>();
	}

	public string Name { get; set; }
	public string Prefix { get; set; }
	public List<ResourceArea> NestedAreas { get; private set; }
	public List<ResourceValue> Values { get; private set; }
}

[DebuggerDisplay("{Name} = {Value}")]
public class ResourceValue
{
	public ResourceValue()
	{
		this.FormatNames = new List<string>();
	}
	public string Name { get; set; }
	public string Value { get; set; }
	public bool HasFormat { get; set; }
	public List<string> FormatNames { get; private set; }
}
#>