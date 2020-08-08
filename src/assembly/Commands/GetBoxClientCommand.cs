using Box.V2;
using System;
using System.Management.Automation;

namespace PoshBox.Commands
{
    /// <summary>
    /// PowerShell cmdlet which returns authenticated Box clients.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "BoxClient")]
    [OutputType(typeof(BoxClient))]
    public class GetBoxClientCommand : PSCmdlet
    {

        /// <summary>
        /// The Box UserID of the user to create a user client for. If no UserID is provided the, current admin client will be returned.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public string UserID { get; set; }

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing (before the process block of any other command).
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

        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called.
        protected override void ProcessRecord()
        {

            if (String.IsNullOrEmpty(UserID))
                WriteObject(PoshBoxAuth.BoxClient);
            else
                WriteObject(PoshBoxAuth.NewUserClient(UserID));

        }

    }

}
