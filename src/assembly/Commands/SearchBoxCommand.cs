using Box.V2;
using Box.V2.Exceptions;
using Box.V2.Models;
using Box.V2.Models.Request;
using PoshBox.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PoshBox.Commands
{

    /// <summary>
    /// PowerShell cmdlet that searches for Box items.
    /// </summary>
    [Cmdlet(VerbsCommon.Search, "Box")]
    [OutputType(typeof(BoxItem))]
    public class SearchBoxCommand : PSCmdlet
    {

        private BoxClient client;
        private string[] fieldNames;

        /// <summary>
        /// The string to search Box for.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string SearchString;

        /// <summary>
        /// The Box UserID to be used during the search. Only items the user has access to will be returned. If no UserID is provided then the admin account will be used.
        /// </summary>
        [Parameter]
        public string UserID { get; set; }

        /// <summary>
        /// The type of item to be retrieved. This is not needed but if provided can save an extra API call to Box and reduce rate limit counts.
        /// </summary>
        [Parameter]
        [Alias("Type")]
        [ValidateSet(new string[] {
            "File",
            "Folder",
            "WebLink"
        })]
        public string ItemType { get; set; }

        /// <summary>
        /// A list of properties to retrieve with the item.
        /// </summary>
        [Parameter]
        [ArgumentCompleter(typeof(BoxItemPropertyNameCompleter))]
        public string[] Properties { get; set; } = BoxItemPropertyNameCompleter.DefaultPropertyNames;

        /// <summary>
        /// Limits search results to a user scope.
        ///
        /// Defaults to UserContent which limits the search to content available to the current user.
        ///
        /// Setting this to EnterpriseContent widens the search to content available to the entire enterprise. To enable this scope for an administrator, please reach out to support.
        /// </summary>
        [Parameter]
        [ValidateSet(new string[] {
            "UserContent",
            "EnterpriseContent"
        })]
        public string Scope { get; set; } = "UserContent";

        /// <summary>
        /// Limits search results to items within the given list of folders IDs.
        /// </summary>
        [Parameter]
        public string[] AncestorFolders;

        /// <summary>
        /// Limits search results to items owned by the given list of owners IDs.
        /// </summary>
        [Parameter]
        public string[] OwnerIDs;

        /// <summary>
        /// Limits search results to a comma-separated list of file extensions.
        /// </summary>
        [Parameter]
        public string[] FileExtensions;

        /// <summary>
        /// Limits search results to items created after the given date.
        /// </summary>
        [Parameter]
        public DateTime? CreatedAfter { get; set; }

        /// <summary>
        /// Limits search results to items created before the given date.
        /// </summary>
        [Parameter]
        public DateTime? CreatedBefore { get; set; }

        /// <summary>
        /// Limits search results to items updated after the given date.
        /// </summary>
        [Parameter]
        public DateTime? UpdatedAfter { get; set; }

        /// <summary>
        /// Limits search results to items updated before the given date.
        /// </summary>
        [Parameter]
        public DateTime? UpdatedBefore { get; set; }

        /// <summary>
        /// Limits search results to items above a given file size.
        /// </summary>
        [Parameter]
        public long? SizeLowerBound { get; set; }

        /// <summary>
        /// Limits search results to items below a given file size.
        /// </summary>
        [Parameter]
        public long? SizeUpperBound { get; set; }

        /// <summary>
        /// Limits search results to items with the given content types.
        /// </summary>
        [Parameter]
        public string[] ContentTypes { get; set; }

        /// <summary>
        /// Controls if search results include the trash.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeTrash { get; set; }

        /// <summary>
        /// The maximum number of items to retrieve per request.
        /// </summary>
        [Parameter]
        [ValidateRange(0, 200)]
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// The offset of the item at which to begin the response.
        /// </summary>
        [Parameter]
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Defines the order in which results are returned. Defaults to Relevance.
        ///
        /// - Relevance (default) returns the results sorted by relevance to the query search term.
        /// - ModifiedAt returns the results ordered in descending order by date at which the item was last modified.
        /// </summary>
        [Parameter]
        [ValidateSet(new string[] {
            "Relevance",
            "ModifiedAt"
        })]
        public string Sort { get; set; } = "Relevance";

        /// <summary>
        /// Defines the direction in which search results are ordered, ascending (ASC) or descending (DESC). Default value is DESC.
        ///
        /// When results are sorted by Relevance the ordering is forced to DESC, ignoring the value of this parameter.
        /// </summary>
        [Parameter]
        public BoxSortDirection SortDirection { get; set; } = BoxSortDirection.DESC;

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

            fieldNames = PropertyUtility.GetPropertyNames(typeof(BoxItem), Properties);

            if (UserID == null) {
                WriteVerbose("Using admin client already established: " + PoshBoxAuth.BoxConfiguration.ClientId);
                client = PoshBoxAuth.BoxClient;
            }
            else
            {
                WriteVerbose("Retrieving user client for user: " + UserID);
                client = PoshBoxAuth.NewUserClient(UserID);
            }

            ItemType = (ItemType == "WebLink") ? "web_link" : ItemType?.ToLower();
            Scope = (Scope == "UserContent") ? "user_content" : "enterprise_content";
            Sort = (Sort == "Relevance") ? "relevance" : "modified_at";

        }

        /// <summary>
        /// Retrieves folder/file information for each item in the pipeline.
        /// </summary>
        /// <remarks>
        /// This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called.
        /// </remarks>
        protected override void ProcessRecord()
        {

            WriteVerbose("Searching for: " + SearchString);
            client.SearchManager.QueryAsync(
                SearchString,
                Scope,
                FileExtensions,
                CreatedAfter,
                CreatedBefore,
                UpdatedAfter,
                UpdatedBefore,
                SizeLowerBound,
                SizeUpperBound,
                OwnerIDs,
                AncestorFolders,
                ContentTypes,
                ItemType,
                "non_trashed_only",
                null,
                PageSize,
                Offset,
                fieldNames,
                Sort,
                SortDirection
            ).Result.Entries.ForEach(e => WriteObject(e));

            if (IncludeTrash) {
                WriteVerbose("Expanding search to trashed items: " + SearchString);
                client.SearchManager.QueryAsync(
                    SearchString,
                    Scope,
                    FileExtensions,
                    CreatedAfter,
                    CreatedBefore,
                    UpdatedAfter,
                    UpdatedBefore,
                    SizeLowerBound,
                    SizeUpperBound,
                    OwnerIDs,
                    AncestorFolders,
                    ContentTypes,
                    ItemType,
                    "trashed_only",
                    null,
                    PageSize,
                    Offset,
                    fieldNames,
                    Sort,
                    SortDirection
                ).Result.Entries.ForEach(e => WriteObject(e));
            }

        }

    }

}
