
using System;
using System.Linq;
using Box.V2;
using Box.V2.Exceptions;
using Box.V2.Models;

namespace PoshBox.Helper
{

    /// <summary>
    /// Utility class providing a variety of helper methods for working with folders.
    /// </summary>
    public static class Utility
    {

        /// <summary>
        /// Walk a Box folder tree, applying the specified action to every file and folder encountered.
        /// </summary>
        /// <param name="client">An authenticated Box client.</param>
        /// <param name="folder">The Box folder to traverse.</param>
        /// <param name="action">An action to apply to the Box files and folders. This can be any method that accepts a BoxItem as a parameter.</param>
        /// <param name="verboseAPIAction">(Optional) An action that takes a string and displays it as verbose information for each Box API call made. This can be any method that accepts a string as a parameter.</param>
        /// <param name="properties">(Optional) A string array of Box file/folder properties to fetch with every folder item.</param>
        /// <param name="pageSize">(Optional) The number of items per request page. Default is 1000.</param>
        /// <param name="recursionDepth">(Optional) The maximum depth to recur if the -Recurse switch is given. A value less than 0 will recur infinitely. The default is -1 (infinite).</param>
        /// <exception cref="System.AggregateException">Throws when errors occur with the Box API. Typically consisting of BoxException objects.</exception>
        public static void WalkFolderTree(
            BoxClient client,
            BoxFolder folder,
            Action<BoxItem> action,
            Action<String> verboseAPIAction = null,
            string[] properties = null,
            int pageSize = 1000,
            int recursionDepth = -1
        ) {

            properties = properties ?? BoxItemPropertyNameCompleter.DefaultPropertyNames;
            var fieldNames = PropertyUtility.GetPropertyNames(typeof(BoxFolder), properties);

            // Apply the action to this folder.
            action(folder);

            // Get all items in this folder.
            ApplyVerboseAPIAction(verboseAPIAction, "Retrieving child items for folder: " + folder.Id);
            var items = client.FoldersManager.GetFolderItemsAsync(folder.Id, pageSize, fields:fieldNames, autoPaginate:true).Result.Entries;

            // Recur to each subfolder.
            foreach (var subfolder in items.Where(i => i.Type == "folder"))
                if (recursionDepth != 0)
                    WalkFolderTree(client, (BoxFolder)subfolder, action, verboseAPIAction, properties, pageSize, recursionDepth - 1);

            // Apply the action to each item in this folder.
            foreach (var file in items.Where(i => i.Type == "file"))
                action(file);
        }

        // Applies custom error action.
        private static void ApplyErrorAPIAction(Action<Exception> errorAction, string msg, Exception origException)
        {
            var error = new Exception(msg , origException);
            if (errorAction != null)
                errorAction(error);
            else
                throw error;
        }

        // Applies custom verbose action.
        private static void ApplyVerboseAPIAction(Action<string> verboseAction, string msg)
        {
            if (verboseAction != null)
                verboseAction(msg);
        }

    }

}