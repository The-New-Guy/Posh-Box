using Box.V2;
using Box.V2.Exceptions;
using Box.V2.Models;
using PoshBox.Helper;
using System;
using System.Linq;
using System.Management.Automation;

namespace PoshBox.Commands
{

    /// <summary>
    /// PowerShell cmdlet that returns the requested Box item.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "BoxItem")]
    [OutputType(typeof(BoxItem))]
    public class GetBoxItemCommand : PSCmdlet
    {

        private BoxClient client;
        private string[] fieldNames;

        /// <summary>
        /// The Box UserID of the user that has access to the Box item to be retrieved.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string UserID { get; set; }

        /// <summary>
        /// The Box ItemID of the folder or item to be retrieved. If no ItemID is provided then the user's root folder is returned.
        /// </summary>
        [Parameter(
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true
        )]
        public string ItemID { get; set; } = "0";

        /// <summary>
        /// The type of item to be retrieved. This is not needed but if provided can save an extra API call to Box and reduce rate limit counts.
        /// </summary>
        [Parameter]
        [Alias("Type")]
        [ValidateSet(new string[] {
            "File",
            "Folder"
        })]
        public string ItemType { get; set; } = "Folder";

        /// <summary>
        /// A list of properties to retrieve with the item.
        /// </summary>
        [Parameter]
        [ArgumentCompleter(typeof(BoxItemPropertyNameCompleter))]
        public string[] Properties { get; set; } = BoxItemPropertyNameCompleter.DefaultPropertyNames;

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

            BoxItem item = null;

            if (String.Equals(ItemType, "Folder", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    WriteVerbose("Retrieving folder: " + ItemID);
                    item = client.FoldersManager.GetInformationAsync(ItemID, fields: fieldNames).Result;
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.Flatten().InnerExceptions) {
                        // If no ItemType was given and we got a 404, try checking to see if its a file ID.
                        if (e is BoxException && ((BoxException)e).StatusCode.ToString() == "NotFound")
                        {
                            if (!this.MyInvocation.BoundParameters.Keys.Contains("ItemType"))
                            {
                                WriteVerbose("Item not found as a folder. Retrying as a file.");
                                WriteVerbose("Retrieving file: " + ItemID);
                                item = client.FilesManager.GetInformationAsync(ItemID, fields: fieldNames).Result;
                            }
                            else {
                                ThrowTerminatingError(
                                new ErrorRecord(
                                    new Exception("Folder not found: " + ItemID, e),
                                    "9000",
                                    ErrorCategory.ObjectNotFound,
                                    null
                                ));
                            }
                        }
                        else
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                    new Exception("Error retrieving item: " + ItemID, e),
                                    "9000",
                                    ErrorCategory.NotSpecified,
                                    null
                                ));
                        }
                    }
                }
            }
            else
            {
                try
                {
                    WriteVerbose("Retrieving file: " + ItemID);
                    item = client.FilesManager.GetInformationAsync(ItemID, fields: fieldNames).Result;
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.Flatten().InnerExceptions) {
                        // Just want to provide a more helpful error message for the common 404 Not Found error.
                        if (e is BoxException && ((BoxException)e).StatusCode.ToString() == "NotFound")
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                    new Exception("File not found: " + ItemID , e),
                                    "9000",
                                    ErrorCategory.ObjectNotFound,
                                    null
                                ));
                        }
                        else
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                    new Exception("Error retrieving file: " + ItemID , e),
                                    "9000",
                                    ErrorCategory.NotSpecified,
                                    null
                                ));
                        }
                    }
                }
            }

            if (item != null)
                WriteObject(item);

        }

    }

}
