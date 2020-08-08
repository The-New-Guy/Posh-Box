using Box.V2;
using Box.V2.Exceptions;
using Box.V2.Models;
using PoshBox.Helper;
using System;
using System.Management.Automation;

namespace PoshBox.Commands
{
    [Cmdlet(VerbsCommon.Get, "BoxChildItem")]
    [OutputType(typeof(BoxItem))]
    public class GetBoxChildItemCommand : PSCmdlet
    {

        private BoxClient client;
        private string[] fieldNames;

        /// <summary>
        /// The Box UserID of the user that has access to the Box items to be retrieved.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string UserID { get; set; }

        /// <summary>
        /// The Box ItemID of the folder or item to be retrieved.
        /// </summary>
        [Parameter(
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true
        )]
        public string ItemID { get; set; } = "0";

        /// <summary>
        /// A list of properties to retrieve with the item.
        /// </summary>
        [Parameter]
        [ArgumentCompleter(typeof(BoxItemPropertyNameCompleter))]
        public string[] Properties { get; set; } = BoxItemPropertyNameCompleter.DefaultPropertyNames;

        /// <summary>
        /// The maximum number of items to retrieve per request.
        /// </summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int PageSize { get; set; } = 500;

        /// <summary>
        /// If this switch is provided, child items will be returned recursively throughout child items.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse;

        /// <summary>
        /// The maximum depth to recur if the -Recurse switch is given. A value less than 0 will recur infinitely. The default is -1 (infinite).
        /// </summary>
        [Parameter]
        public int RecursionDepth = -1;

        /// <summary>
        /// Validates parameter input before processing the pipeline.
        /// </summary>
        /// <remarks>
        /// This method gets called once for each cmdlet in the pipeline when the pipeline starts executing (before the process block of any other command).
        /// </remarks>
        protected override void BeginProcessing()
        {

            if (!PoshBoxAuth.IsInitialized)
                ThrowTerminatingError(
                    new ErrorRecord(
                        new Exception("No Box configuration found. Use Connect-Box to import a configuration."),
                        "9000",
                        ErrorCategory.InvalidOperation,
                        null
                    ));

            if (Array.Exists(Properties, name => name == "*"))
                Properties = BoxItemPropertyNameCompleter.AllPropertyNames;

            fieldNames = PropertyUtility.GetPropertyNames(typeof(BoxFolder), Properties);

            WriteVerbose("Retrieving user client for user: " + UserID);
            client = PoshBoxAuth.NewUserClient(UserID);

        }

        /// <summary>
        /// Retrieves folder/file information for each item in the pipeline.
        /// </summary>
        /// <remarks>
        /// This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called.
        /// </remarks>
        protected override void ProcessRecord()
        {

            BoxCollection<BoxItem> items = null;

            // Get the initial set of items in the specified folder.
            try
            {
                WriteVerbose("Retrieving child items for folder: " + ItemID);
                items = client.FoldersManager.GetFolderItemsAsync(ItemID, PageSize, fields: fieldNames, autoPaginate: true).Result;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions) {
                    // Just want to provide a more helpful error message for the common 404 Not Found error.
                    if (e is BoxException && ((BoxException)e).StatusCode.ToString() == "NotFound")
                    {
                        ThrowTerminatingError(
                            new ErrorRecord(
                                new Exception("Folder not found: " + ItemID , e),
                                "9000",
                                ErrorCategory.ObjectNotFound,
                                null
                            ));
                    }
                    else
                    {
                        ThrowTerminatingError(
                            new ErrorRecord(
                                new Exception("Error retrieving child items for folder: " + ItemID , e),
                                "9000",
                                ErrorCategory.NotSpecified,
                                null
                            ));
                    }
                }
            }

            // Dump them on the pipeline. If this is being done recursively, then WalkFolderTree will do that with our Action we give it.
            if (items != null && !Recurse)
                items.Entries.ForEach(e => WriteObject(e));

            // If -Recurse switch is given, walk the entire folder tree.
            if (Recurse && RecursionDepth != 0)
                foreach (var currItem in items.Entries)
                    if (currItem.Type == "folder")
                    {
                        try
                        {
                            FolderUtility.WalkFolderTree(client, (BoxFolder)currItem, WriteObject, WriteVerbose, Properties, PageSize, RecursionDepth - 1);
                        }
                        catch (Exception e)
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                    new Exception("Error recursively retrieving child items for folder: " + ItemID, e),
                                    "9000",
                                    ErrorCategory.NotSpecified,
                                    null
                                ));
                        }
                    }
                    else
                    {
                        WriteObject(currItem);
                    }


        }

    }

}
