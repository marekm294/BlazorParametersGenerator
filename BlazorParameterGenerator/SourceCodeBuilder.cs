﻿using BlazorParameterGenerator.Models;
using System.Collections.Generic;
using System.Linq;

namespace BlazorParameterGenerator;

internal sealed class SourceCodeBuilder
{
    private string? _namespace;
    private string? _modifiers;
    private string? _className;
    private bool _shouldOverrideMethod;
    private List<CustomPropertyInformation> _customPropertyInfos = [];
    CustomPropertyInformation? _captureUnmatchedValuesProperty;

    public SourceCodeBuilder AddUsings()
    {
        return this;
    }

    public SourceCodeBuilder AddNamespace(string namespaceValue)
    {
        _namespace = namespaceValue;
        return this;
    }

    public SourceCodeBuilder AddClass(
        IEnumerable<string> modifiers,
        string className)
    {
        _modifiers = string.Join(" ", modifiers);
        _className = className;
        return this;
    }

    public SourceCodeBuilder AddMethodContent(
        List<CustomPropertyInformation> customPropertyInfos,
        CustomPropertyInformation? captureUnmatchedValuesProperty)
    {
        _customPropertyInfos = customPropertyInfos;
        _shouldOverrideMethod = _customPropertyInfos.Any();
        _captureUnmatchedValuesProperty = captureUnmatchedValuesProperty;
        return this;
    }

    public override string ToString()
    {
        return $@"// This code is generated by SetParameterGenerator
using Microsoft.AspNetCore.Components;

namespace {_namespace}
{{
    {_modifiers} class {_className}
    {{
        {GetMethodSourceCode()}
    }}
}}";
    }

    public string GetMethodSourceCode()
    {
        if (_shouldOverrideMethod is false)
        {
            return "";
        }

        return $@"public override Task SetParametersAsync(ParameterView parameters)
        {{
            foreach (var parameter in parameters)
            {{
                {GetSwitchSourceCode()}
            }}

            return base.SetParametersAsync(ParameterView.Empty);
        }}";
    }

    private string GetSwitchSourceCode()
    {
        return $@"switch (parameter.Name)
                {{
                    {GetCaseSourceCodes()}
                    {GetDefaultCaseSourceCode()}
                }}";
    }

    private string GetCaseSourceCodes()
    {
        var caseStatements = _customPropertyInfos
            .Select(p => $@"case nameof({p.PropertyName}):
                        {p.PropertyName} = ({p.PropertyType})parameter.Value;
                        break;
                    ");

        return string.Join("", caseStatements).TrimEnd();
    }

    private string GetDefaultCaseSourceCode()
    {
        if (_captureUnmatchedValuesProperty is null)
        {
            return $@"default:
                        throw new ArgumentException($""Unknown parameter: {{parameter.Name}}"");";
        }
        return $@"default:
                        {_captureUnmatchedValuesProperty.PropertyName} ??= new Dictionary<string, object>();
                        {_captureUnmatchedValuesProperty.PropertyName}[parameter.Name] = parameter.Value;
                        break;";
    }
}