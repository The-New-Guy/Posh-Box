
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using Box.V2.Models;

namespace PoshBox.Helper
{

    public class PropertyUtility
    {

        /// <summary>
        /// Returns an array of strings representing the display name of the properties of the given Box object type.
        /// </summary>
        /// <param name="type">The Box object type to return property display names for.</param>
        /// <returns>Array of strings representing the display name of the properties of the given Box object type.</returns>
        public static string[] GetPropertyDisplayNames(Type type) {
            return type.GetProperties().Select(prop => prop.Name).ToArray();
        }

        /// <summary>
        /// Returns an array of strings representing the Box API field name of the properties of the given Box object type.
        /// </summary>
        /// <param name="type">The Box object type to return Box API field names for.</param>
        /// <param name="properties">(Optional) An array of strings containing the display names of the desired property Box API field names</param>
        /// <returns>Array of strings representing the Box API field name of the properties of the given Box object type.</returns>
        public static string[] GetPropertyNames(Type type, string[] properties = null) {
            if (properties == null)
                return type.GetProperties().
                    Select(prop => prop.CustomAttributes.First(e => e.AttributeType.Name == "JsonPropertyAttribute").
                    NamedArguments.First().TypedValue.Value.ToString()
                ).ToArray();
            else
                return type.GetProperties().
                    Where(e => properties.Contains(e.Name)).
                    Select(prop => prop.CustomAttributes.First(e => e.AttributeType.Name == "JsonPropertyAttribute").
                    NamedArguments.First().TypedValue.Value.ToString()
                ).ToArray();
        }

    }

    // This ArgumentCompleter is for tab completion of the Properties parameter for BoxItem type commands.
    // See the following for property names : https://developer.box.com/reference/get-folders-id-items

    /// <summary>
    /// PowerShell IArgumentCompleter used to provide tab completion support for the commonly used "-Properties" parameter.
    /// </summary>
    public class BoxItemPropertyNameCompleter : IArgumentCompleter
    {

        public static readonly string[] AllPropertyNames = PropertyUtility.GetPropertyDisplayNames(typeof(BoxItem));

        public static readonly string[] DefaultPropertyNames = new string[] {
            "Id",
            "Type",
            "CreatedAt",
            "CreatedBy",
            "Description",
            "Name",
            "OwnedBy",
            "Permissions",
            "Parent",
            "PathCollection"
        };

        IEnumerable<CompletionResult> IArgumentCompleter.CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters
        ) {
            return AllPropertyNames.
                Where(new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase).IsMatch).
                Select(s => new CompletionResult(s));
        }

    }

    // This ArgumentCompleter is for tab completion of the Properties parameter for BoxUser type commands.
    // See the following for property names : https://developer.box.com/reference/resources/user

    /// <summary>
    /// PowerShell IArgumentCompleter used to provide tab completion support for the commonly used "-Properties" parameter.
    /// </summary>
    public class BoxUserPropertyNameCompleter : IArgumentCompleter
    {

        public static readonly string[] AllPropertyNames = PropertyUtility.GetPropertyDisplayNames(typeof(BoxUser));

        public static readonly string[] DefaultPropertyNames = new string[] {
            "Id",
            "Type",
            "CreatedAt",
            "CreatedBy",
            "Description",
            "Name",
            "OwnedBy",
            "Permissions",
            "Parent",
            "PathCollection"
        };

        IEnumerable<CompletionResult> IArgumentCompleter.CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters
        ) {
            return AllPropertyNames.
                Where(new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase).IsMatch).
                Select(s => new CompletionResult(s));
        }

    }

}