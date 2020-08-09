using Box.V2;
using Box.V2.Exceptions;
using Box.V2.Models;
using PoshBox.Helper;
using System;
using System.Management.Automation;

namespace PoshBox.Commands
{
    [Cmdlet(VerbsCommon.Get, "BoxUser", DefaultParameterSetName = "UserID")]
    [OutputType(typeof(BoxUser))]
    public class GetBoxUserCommand : PSCmdlet
    {

        private BoxClient client;
        private string[] fieldNames;

        /// <summary>
        /// The Box UserID of the user to retrieve information for.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "UserID")]
        public string UserID { get; set; }

        /// <summary>
        /// The Box UserID of the user to retrieve information for.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "SearchUser")]
        public string SearchUser { get; set; }

        /// <summary>
        /// A list of properties to retrieve with the item.
        /// </summary>
        [Parameter(ParameterSetName = "UserID")]
        [Parameter(ParameterSetName = "SearchUser")]
        [ArgumentCompleter(typeof(BoxUserPropertyNameCompleter))]
        public string[] Properties { get; set; } = BoxUserPropertyNameCompleter.DefaultPropertyNames;

        /// <summary>
        /// The maximum number of items to retrieve per request.
        /// </summary>
        [Parameter(ParameterSetName = "SearchUser")]
        [ValidateRange(0, 1000)]
        public uint PageSize { get; set; } = 500;

        /// <summary>
        /// Limits the results to the kind of user specified.
        ///
        /// - All returns every kind of user for whom the login or name partially matches the SearchUser parameter. It will only return an external user if the login matches the SearchUser parameter completely, and in that case it will only return that user.
        ///
        /// - Managed returns all managed and app users for whom the login or name partially matches the SearchUser parameter.
        ///
        /// - External returns all external users for whom the login matches the SearchUser parameter exactly.
        /// </summary>
        [Parameter(ParameterSetName = "SearchUser")]
        [ValidateSet(new string[] {
            "All",
            "Managed",
            "External"
        })]
        public string UserType { get; set; } = "All";

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
                Properties = BoxUserPropertyNameCompleter.AllPropertyNames;

            fieldNames = PropertyUtility.GetPropertyNames(typeof(BoxUser), Properties);

            SearchUser = (SearchUser == "*") ? null : SearchUser;

            UserType = UserType.ToLower();

            WriteVerbose("Using admin client already established: " + PoshBoxAuth.BoxConfiguration.ClientId);
            client = PoshBoxAuth.BoxClient;

        }

        /// <summary>
        /// Retrieves folder/file information for each item in the pipeline.
        /// </summary>
        /// <remarks>
        /// This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called.
        /// </remarks>
        protected override void ProcessRecord()
        {

            if (ParameterSetName == "UserID") {

                BoxUser user = null;

                // Get the initial set of items in the specified folder.
                try
                {
                    WriteVerbose("Retrieving user: " + UserID);
                    user = client.UsersManager.GetUserInformationAsync(UserID, fieldNames).Result;
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.Flatten().InnerExceptions) {
                        // Just want to provide a more helpful error message for the common 404 Not Found error.
                        if (e is BoxException && ((BoxException)e).StatusCode.ToString() == "NotFound")
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                    new Exception("User not found: " + UserID , e),
                                    "9000",
                                    ErrorCategory.ObjectNotFound,
                                    null
                                ));
                        }
                        else
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                    new Exception("Error retrieving user: " + UserID , e),
                                    "9000",
                                    ErrorCategory.NotSpecified,
                                    null
                                ));
                        }
                    }
                }

                if (user != null)
                    WriteObject(user);

            } else {

                BoxCollection<BoxUser> users = null;

                // Get the initial set of items in the specified folder.
                try
                {
                    WriteVerbose("Retrieving users with search string: " + SearchUser);
                    users = client.UsersManager.GetEnterpriseUsersAsync(SearchUser, limit: PageSize, fields: fieldNames, userType: UserType, autoPaginate: true).Result;
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.Flatten().InnerExceptions) {
                        ThrowTerminatingError(
                            new ErrorRecord(
                                new Exception("Error retrieving users with search string: " + SearchUser , e),
                                "9000",
                                ErrorCategory.NotSpecified,
                                null
                            ));
                    }
                }

                if (users != null)
                    users.Entries.ForEach(e => WriteObject(e));

            }

        }

    }

}
