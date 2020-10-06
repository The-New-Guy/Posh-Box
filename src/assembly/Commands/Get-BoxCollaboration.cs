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
    /// PowerShell cmdlet that returns the requested Box collaboration.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "BoxCollaboration", DefaultParameterSetName = "ID")]
    [OutputType(typeof(BoxCollaboration))]
    public class GetBoxCollaborationCommand : PSCmdlet
    {

        private BoxClient client;
        private string[] fieldNames;

        /// <summary>
        /// (Optional) The Box UserID to use when retrieving the collaboration. Only items the user has access to will be returned. If no UserID is provided then the admin account will be used.
        /// </summary>
        [Parameter(ParameterSetName = "ID")]
        [Parameter(ParameterSetName = "FolderObj")]
        [Parameter(ParameterSetName = "FileObj")]
        public string UserID { get; set; }

        /// <summary>
        /// The Box ItemID of the folder or item to retrieve collaborations for. If no ItemID is provided then the user's root folder is returned.
        /// </summary>
        [Parameter(
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "ID"
        )]
        public string ItemID { get; set; } = "0";

        /// <summary>
        /// The type of item to retrieve collaborations for. This is not needed but if provided can save an extra API call to Box and reduce rate limit counts. Only used with ItemID parameter.
        /// </summary>
        [Parameter(ParameterSetName = "ID")]
        [Alias("Type")]
        [ValidateSet(new string[] {
            "File",
            "Folder"
        })]
        public string ItemType { get; set; } = "Folder";

        /// <summary>
        /// The box folder to retrieve collaborations for.
        /// </summary>
        [Parameter(
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "FolderObj"
        )]
        public BoxFolder Folder;

        /// <summary>
        /// The box file to retrieve collaborations for.
        /// </summary>
        [Parameter(
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "FileObj"
        )]
        public BoxFile File;

        /// <summary>
        /// A list of properties to retrieve with the item.
        /// </summary>
        [Parameter(ParameterSetName = "ID")]
        [Parameter(ParameterSetName = "FolderObj")]
        [Parameter(ParameterSetName = "FileObj")]
        [ArgumentCompleter(typeof(BoxCollaborationPropertyNameCompleter))]
        public string[] Properties { get; set; } = BoxCollaborationPropertyNameCompleter.DefaultPropertyNames;

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
                Properties = BoxCollaborationPropertyNameCompleter.AllPropertyNames;

            fieldNames = PropertyUtility.GetPropertyNames(typeof(BoxCollaboration), Properties);

            if (UserID == null) {
                WriteVerbose("Using admin client already established: " + PoshBoxAuth.BoxConfiguration.ClientId);
                client = PoshBoxAuth.BoxClient;
            }
            else
            {
                WriteVerbose("Retrieving user client for user: " + UserID);
                client = PoshBoxAuth.NewUserClient(UserID);
            }

        }

        /// <summary>
        /// Retrieves folder/file information for each item in the pipeline.
        /// </summary>
        /// <remarks>
        /// This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called.
        /// </remarks>
        protected override void ProcessRecord()
        {

            BoxCollection<BoxCollaboration> collabs = null;

            if (String.Equals(ItemType, "Folder", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    WriteVerbose("Retrieving folder: " + ItemID);
                    collabs = client.FoldersManager.GetCollaborationsAsync(ItemID, fields: fieldNames).Result;
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
                                collabs = client.FilesManager.GetCollaborationsAsync(ItemID, fields: fieldNames).Result;
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
                    collabs = client.FilesManager.GetCollaborationsAsync(ItemID, fields: fieldNames).Result;
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

            if (collabs?.Entries != null)
                WriteObject(collabs.Entries);

        }

    }

}
